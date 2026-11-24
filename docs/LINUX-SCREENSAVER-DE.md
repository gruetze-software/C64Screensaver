# Gruetze RetroSaver unter Linux als Bildschirmschoner nutzen

Die Begleit-App stellt das Bild ein. Für den Start nach Inaktivität ist der jeweilige Desktop zuständig. Der Snap installiert keinen eigenen Hintergrunddienst und ersetzt keine Bildschirmsperre.

## 1. App und Vollbild testen

Nach der Snap-Installation öffnest du die Begleit-App im Startmenü oder mit:

```bash
gruetze-retrosaver
```

C64 oder Amiga wählen, Monitor-Effekte einstellen und **Save settings** anklicken. Über **Start fullscreen preview** siehst du das Ergebnis. Alternativ:

```bash
/snap/bin/gruetze-retrosaver.screensaver
```

Maus bewegen, klicken oder eine Taste drücken beendet die Vollbildanzeige.

## 2. Lubuntu: zuerst die Sitzung prüfen

Im Terminal:

```bash
echo "$XDG_SESSION_TYPE"
```

Nur bei **x11** mit den folgenden Schritten fortfahren. Bei **wayland** diesen X11-Helfer nicht verwenden: Die automatische Aktivierung muss dann über eine passende Funktion des Compositors erfolgen. XWayland genügt nicht zur Überwachung der gesamten Sitzung.

## 3. Unter Lubuntu/X11 nach Inaktivität starten

Der externe Helfer **xautolock** kann nach einer Ruhezeit ein Programm starten. Er stammt aus den Ubuntu-Paketquellen:

```bash
sudo apt install xautolock
```

Zuerst die Vollbildanzeige ausdrücklich mit dem X11-Backend prüfen:

```bash
/snap/bin/gruetze-retrosaver.screensaver --x11
```

Danach für einen kurzen Test nach **einer Minute** ohne Maus-/Tastatureingabe:

```bash
xautolock -time 1 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'
```

Das Terminal für diesen Test offen lassen. Nach einer Minute sollte die Animation erscheinen. Zum Beenden des Helfers ein weiteres Terminal öffnen und eingeben:

```bash
xautolock -exit
```

Für den normalen Betrieb kannst du fünf Minuten verwenden:

```bash
xautolock -time 5 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'
```

Läuft bereits ein xautolock-Helfer mit anderer Wartezeit, ihn zuerst mit `xautolock -exit` beenden.

## 4. Nach jeder Anmeldung automatisch aktivieren

Erst nach erfolgreichem Test in den **LXQt-Sitzungseinstellungen** den Bereich **Autostart** öffnen und einen eigenen Eintrag hinzufügen:

- Name: **Gruetze RetroSaver bei Inaktivität**
- Befehl: `xautolock -time 5 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'`

Die genaue Benennung des Einstellungsdialogs hängt von der Lubuntu-Version ab. Der Eintrag ist nur für X11-Sitzungen gedacht. Bei einem späteren Wechsel zu Wayland wieder entfernen. Zum Deaktivieren den eigenen Autostart-Eintrag entfernen und `xautolock -exit` ausführen.

**Die vorhandene Passwortsperre eingeschaltet lassen.** Der RetroSaver sperrt den Rechner nicht. Seine Startzeit sollte vor dem automatischen Abdunkeln bzw. der vorhandenen Bildschirmsperre liegen, sonst ist die Animation verdeckt. Nicht die Sperre abschalten, um die Animation sichtbar zu machen. xautolock berücksichtigt zudem nicht automatisch jede Video-/Präsentationssperre.

Lubuntu kann bereits XScreenSaver verwenden. Gruetze RetroSaver ist derzeit **kein einbettbares XScreenSaver-Modul** und erscheint nicht automatisch in dessen Auswahlliste. Der oben beschriebene Weg startet die eigenständige Vollbildanimation vor der normalen Sperre.

## Andere Linux-Desktops

| Sitzung | Einrichtung |
| --- | --- |
| Lubuntu/LXQt, Xfce, MATE oder Cinnamon unter X11 | Den oben beschriebenen xautolock-Weg testen; Autostart im jeweiligen Desktop konfigurieren. |
| KDE Plasma | Wenn vorhanden, die eigene Inaktivitäts-/Skriptfunktion der Energieverwaltung verwenden. Als Befehl `/snap/bin/gruetze-retrosaver.screensaver` eintragen. Je nach Plasma-Version unterscheiden sich die Optionen. |
| GNOME unter Wayland, etwa Ubuntu mit GNOME | Die Vollbildanimation lässt sich manuell starten. Eine native Einbindung in den GNOME-Sperrbildschirm bietet diese App nicht. |
| Andere Wayland-Compositoren | Eine compositor-spezifische Inaktivitätsfunktion ist nötig. Es gibt keinen hier getesteten allgemeinen Linux-Weg. |

Bei einem **ZIP-Paket** statt Snap den absoluten Pfad zur entpackten Anwendung mit `--fullscreen --x11` anstelle des Snap-Befehls einsetzen. Alle Paketdateien zusammen in einem dauerhaften Ordner lassen.

Diese Einrichtung ist anhand der Dokumentation beschrieben. Die Pakete und Bildschirmdarstellung wurden gebaut/geprüft; der automatische Start muss noch in der konkreten Lubuntu-Sitzung getestet werden.

Quellen: [Ubuntu-Handbuch zu xautolock](https://manpages.ubuntu.com/manpages/noble/man1/xautolock.1.html), [XScreenSaver-Handbuch](https://manpages.ubuntu.com/manpages/noble/man1/xscreensaver.1.html), [weitere Desktop- und macOS-Hinweise](SCREEN-SAVER-SETUP.md).
