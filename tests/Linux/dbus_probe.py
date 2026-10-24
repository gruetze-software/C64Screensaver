"""Run inside dbus-run-session; never contacts the user's desktop bus."""
import os
import json
import tempfile
import selectors
import subprocess
import sys
import threading
from gi.repository import Gio, GLib

app = os.path.abspath(sys.argv[1])
connection = Gio.bus_get_sync(Gio.BusType.SESSION, None)
state = {"locked": False, "denied": False}
services = [
    ("org.gnome.Mutter.IdleMonitor", "/org/gnome/Mutter/IdleMonitor/Core",
     "GetIdletime", "t", lambda: 1000),
    ("org.gnome.ScreenSaver", "/org/gnome/ScreenSaver",
     "GetActive", "b", lambda: state["locked"]),
    ("org.gnome.SessionManager", "/org/gnome/SessionManager",
     "IsInhibited", "b", lambda: False),
]
registrations = []
for bus, path, method, result_type, value in services:
    reply = connection.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus",
                                "org.freedesktop.DBus", "RequestName",
                                GLib.Variant("(su)", (bus, 0)),
                                GLib.VariantType("(u)"), Gio.DBusCallFlags.NONE, 2000, None)
    assert reply.unpack()[0] == 1, reply
    input_arg = '<arg type="u" direction="in"/>' if method == "IsInhibited" else ""
    xml = f'<node><interface name="{bus}"><method name="{method}">{input_arg}<arg type="{result_type}" direction="out"/></method></interface></node>'
    interface = Gio.DBusNodeInfo.new_for_xml(xml).interfaces[0]

    def handle(conn, sender, object_path, iface, method_name, parameters, invocation,
               fn=value, kind=result_type):
        if method_name == "IsInhibited" and state["denied"]:
            invocation.return_dbus_error("org.freedesktop.DBus.Error.AccessDenied", "test denial")
        else:
            invocation.return_value(GLib.Variant(f"({kind})", (fn(),)))

    registrations.append(connection.register_object(path, interface, handle, None, None))

loop = GLib.MainLoop()
worker = threading.Thread(target=loop.run, daemon=True)
worker.start()
environment = dict(os.environ, XDG_CURRENT_DESKTOP="ubuntu:GNOME", XDG_SESSION_TYPE="wayland",
                   WAYLAND_DISPLAY="c64-test-no-display")
try:
    def diagnose(code, message):
        result = subprocess.run([app, "--diagnose-idle"], env=environment, text=True,
                                capture_output=True, timeout=20)
        assert result.returncode == code, (result.returncode, result.stdout, result.stderr)
        assert message in result.stdout, result.stdout
        print(result.stdout.strip())

    diagnose(0, "Ready on UBUNTU:GNOME")
    state["locked"] = True
    diagnose(0, "Desktop is locked")
    state["locked"] = False
    state["denied"] = True
    diagnose(3, "inhibition status unavailable")
    state["denied"] = False
    with tempfile.TemporaryDirectory(prefix="c64-idle-test-") as configuration:
        config_dir = os.path.join(configuration, "C64Screensaver")
        os.makedirs(config_dir)
        config_file = os.path.join(config_dir, "settings.json")

        def write_config(enabled):
            temporary = config_file + ".tmp"
            with open(temporary, "w") as stream:
                json.dump({"AutoStartEnabled": enabled, "IdleDelaySeconds": 60}, stream)
            os.replace(temporary, config_file)

        daemon_env = dict(environment, XDG_CONFIG_HOME=configuration)
        def start_daemon():
            write_config(True)
            process = subprocess.Popen([app, "--idle-daemon"], env=daemon_env, text=True,
                                       stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            try:
                with selectors.DefaultSelector() as selector:
                    selector.register(process.stdout, selectors.EVENT_READ)
                    assert selector.select(15), "daemon did not become ready"
                assert "Ready on" in process.stdout.readline()
                return process
            except BaseException:
                process.kill()
                process.wait(timeout=5)
                raise

        process = start_daemon()
        try:
            duplicate = subprocess.run([app, "--idle-daemon"], env=daemon_env,
                                       capture_output=True, timeout=10)
            assert duplicate.returncode == 0, duplicate.stderr
            write_config(False)
            process.communicate(timeout=10)
            assert process.returncode == 0, process.returncode
        finally:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=5)
        process = start_daemon()
        try:
            process.terminate()
            process.communicate(timeout=10)
            assert process.returncode == 0, process.returncode
        finally:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=5)
    print("PASS: singleton, disabling settings and SIGTERM stop the real Linux helper.")
    print("PASS: Linux app host -> gdbus -> private session bus; ready, locked and denied scenarios.")
finally:
    loop.quit()
    worker.join(timeout=5)
    for registration in registrations:
        connection.unregister_object(registration)
