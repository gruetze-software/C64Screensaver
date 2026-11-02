# Mac App Store: Sandbox-Test und signiertes Paket

Stand: 2. November 2026, Entwicklung 1.2.6. Es wurde noch kein Store-Build
hochgeladen. Die echte .saver-Komponente bleibt Teil des Produkts; ihre
Installation über den Apple-Systemdialog muss App Review beurteilen.

## Voraussetzungen auf dem Mac

.NET 10, Xcode Command Line Tools, gültige Apple Distribution- und Mac Installer
Distribution-Zertifikate mit privatem Schlüssel im Schlüsselbund Anmeldung.
Ein Mac App Store Connect-Profil für de.gruetze-software.RetroSaver herunterladen.
Die Installer-Identität heißt im Schlüsselbund normalerweise
`3rd Party Mac Developer Installer: Christoph Schaefer (8GZ7AD23D7)`.
`security find-identity -v` zeigt die tatsächlichen Namen an.
Keine privaten Schlüssel, Passwörter oder API-Schlüssel in Git oder im Chat speichern.

## Zuerst die Sandbox testen

Im Projektordner auf dem Mac:

```bash
bash macos/build-store.sh test arm64 "Developer ID Application: Christoph Schaefer (8GZ7AD23D7)"
```

Vorher die bisherige Begleitapp mit Cmd+Q vollständig beenden.
Der Befehl baut beide Komponenten neu. Er verändert keine installierte App und
lädt nichts hoch. Den am Ende ausgegebenen .app-Pfad im Finder öffnen.
Die Test-App enthält dieselben Sandbox-Entitlements wie die spätere Store-App.

1. Auf **Connect settings folder** klicken. Im Systemdialog mit Cmd+Shift+G
   `/Users/Shared` öffnen und diesen Ordner auswählen. Existiert bereits der
   eigene GruetzeRetroSaver-<uid>-Ordner, kann auch nur dieser ausgewählt werden.
2. Nur der eigene private Unterordner wird beschrieben; es werden keine fremden
   Einstellungen gelesen. Die dauerhafte Ordnerfreigabe wird als macOS-
   Sicherheits-Bookmark ausschließlich im App-Container gespeichert.
3. Einstellungen ändern und speichern. App vollständig beenden und erneut öffnen.
   Die gespeicherten Werte müssen wieder erscheinen, ohne erneute Freigabe.
4. **Install / update system screensaver** verwenden, im Apple-Dialog bestätigen
   und den echten Bildschirmschoner mit C64 und Amiga testen. Änderungen in der
   App müssen in den Systemeinstellungen und im laufenden Schoner ankommen.
5. Optionen aus den Systemeinstellungen öffnen, Vollbild, CRT und Programmwechsel
   prüfen. Nach dem Beenden müssen Optionen und Vorschau weiter funktionieren.

Die Ordnerwahl ist explizit, ohne pauschale Dateisystem-Ausnahme und ohne
App-Group-Annahmen über Apples fremden Screensaver-Host. Eine abgelehnte oder
verlorene Freigabe überschreibt die gemeinsamen Einstellungen nicht.

Automatisch: `bash macos/test-store-sandbox.sh arm64` baut einen separat
sandboxed Probeprozess mit der tatsächlichen Begleitapp. Er prüft den GUI-Start,
den Freigabe-Button, die Speichersperre und die vom Betriebssystem verweigerte
Dateierstellung außerhalb des Containers. Der positive Systemdialog-/Neustart-
und echte Installationsablauf benötigt weiterhin den obigen Test auf dem Neo.

## Store-Paket signieren

Erst nach erfolgreichem Sandbox-Test, mit dem tatsächlichen Profilpfad:

```bash
bash macos/build-store.sh store arm64 "Apple Distribution: Christoph Schaefer (8GZ7AD23D7)" "$HOME/Downloads/Gruetze_RetroSaver_App_Store.provisionprofile" "3rd Party Mac Developer Installer: Christoph Schaefer (8GZ7AD23D7)"
```

Das Skript prüft App-ID und Team-Zuordnung, bettet das Profil ein, signiert von
innen nach außen und erstellt ein signiertes .pkg für /Applications. Die native
Komponente liegt in dieser Variante unter Contents/PlugIns. Developer-ID-DMGs
verwenden weiterhin ihren bisherigen Weg. Die Store-App wird in einem frischen Ordner aufgebaut. Native Bibliotheken
liegen unter Frameworks, zusätzliche Daten unter Resources; relative Symlinks
erhalten die .NET-Suchpfade. Das nicht benötigte createdump-Werkzeug wird entfernt.
Debugsymbole und Kickstart-Bilder werden nicht in Mac-Pakete aufgenommen.

## Hochladen

Das .pkg in Apples **Transporter** hinzufügen, prüfen lassen und anschließend
hochladen. Das bisherige Developer-ID-DMG ist kein Store-Upload. Profilgültigkeit,
Zertifikatszuordnung, Upload-Validierung und App Review sind zusätzliche Prüfungen;
eine erfolgreiche lokale Signatur ersetzt sie nicht. Version in App Store Connect
an 1.2.6 anpassen, wenn dieser Entwicklungsstand eingereicht wird.

Die REST-API unterstützt Build-Uploads ebenfalls. Dafür wären zusätzliche
App-Store-Connect-API-Zugangsdaten und ein JWT erforderlich. Für den ersten Upload
ist Transporter der kürzere Weg. Dieses Skript veröffentlicht nichts automatisch.

## Quellen

- https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds
- https://developer.apple.com/help/account/provisioning-profiles/create-an-app-store-provisioning-profile
- https://developer.apple.com/library/archive/technotes/tn2206/_index.html
- https://developer.apple.com/documentation/security/app-sandbox
- https://docs.avaloniaui.net/docs/platform-specific-guides/macos
- Avalonia 12.1.4: src/Avalonia.Native/StorageItem.cs (Scopes nur um eigene I/O-Aufrufe)
