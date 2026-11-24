# Snap Store und Microsoft Store vorbereiten

Windows und Linux bleiben kostenlos. Der Mac App Store und dessen Signierung bleiben getrennt.
Arbeitsbranch: `codex/windows-snap-store`, auf Basis des aktuellen Mac-Build-Zweigs.
Noch keine neue Version in einem Store veröffentlicht.

## Microsoft Store

1. Im Partner Center eine Windows-App mit dem Namen **Gruetze RetroSaver** reservieren. Für dieses Paket den MSIX/AppX-Weg verwenden, nicht den EXE/MSI-Installer-Weg.
2. Unter Produktverwaltung → Produktidentität die drei öffentlichen Werte in eine Kopie von `store/windows/store-identity.example.json` eintragen: Name, Publisher und PublisherDisplayName. Exakt übernehmen, einschließlich Großschreibung. Es handelt sich nicht um Passwörter.
3. Auf Windows mit .NET 10 und Windows 10/11 SDK bauen:

```powershell
pwsh -File scripts/build-windows-store.ps1 -IdentityFile C:/Pfad/store-identity.json
```

Das Skript erzeugt ein x64/ARM64-MSIX-Bundle, Logos, SHA256SUMS und Build-Protokolle in einem neuen Unterordner von `artifacts/windows-store`. Es validiert beide Manifeste mit MakeAppx. Debugsymbole und fremde Plattformdateien werden abgewiesen. Das Bundle für den Store bleibt unsigniert; Microsoft signiert bei Annahme. Für diesen Weg brauchst du kein zusätzlich gekauftes Codesignatur-Zertifikat.

Ohne Produktidentität ist nur ein **nicht einreichbarer** Testbuild möglich:

```powershell
pwsh -File scripts/build-windows-store.ps1 -TestIdentity
```

Für einen lokalen Installationstest muss ein passendes Testzertifikat verwendet und nur auf dem Testrechner vertraut werden. Das Build-Skript installiert weder Zertifikate noch die App. Die endgültige installierte Store-App muss vor Einreichung mit dem Windows App Certification Kit geprüft werden. Manifestprüfung ist kein Ersatz für diesen Test.

4. Für die Einreichung das `STORE-CANDIDATE.msixbundle`, die Texte aus LISTING-EN.md / LISTING-DE.md und die Windows-Screenshots verwenden. Testpakete mit `TEST-ONLY` niemals hochladen.
5. Preis kostenlos, Datenschutzerklärung verlinken, Altersfreigabe-Fragebogen entsprechend den tatsächlichen Inhalten beantworten. `runFullTrust` mit den Prüferhinweisen aus LISTING-EN.md erläutern.
6. Installierten Build testen: C64/Amiga, Monitorregler, Vollbild und Rückkehr; anschließend über den expliziten Installationsknopf den echten System-Bildschirmschoner testen. Der Export muss außerhalb des versionierten WindowsApps-Ordners liegen. Darum kopiert die Begleit-App den vollständigen Windows-Payload pro Installation in einen eigenen Unterordner von `%LOCALAPPDATA%/GruetzeRetroSaver/ScreenSavers` und öffnet den Windows-Installer. Die Auswahl und Wartezeit bestätigt der Nutzer selbst. Nach einem Store-Update erneut Install / update klicken. Alte Exporte bleiben erhalten; zum Entfernen erst einen anderen Saver wählen und dann den jeweiligen Exportordner löschen. Keine Dateien nach System32 kopieren.

## Snap Store

Vorgesehener technischer Name: **gruetze-retrosaver**. Er muss im vorhandenen Konto reserviert sein. Falls ein anderer Name reserviert wurde, `name`, App-Name, Desktop-Datei/Exec und Dokumentation gemeinsam anpassen. Eine Reservierung wird nicht automatisch ausgeführt.

Auf Ubuntu 24.04 oder mit dem GitHub-Workflow bauen. Lokal werden .NET 10 und Snapcraft benötigt. Snapcraft verwendet für das Paket eine Ubuntu-Buildumgebung (z.B. LXD); nicht ungefragt die lokale Systeminstallation ändern.

```bash
bash scripts/prepare-snap.sh amd64
snapcraft --platform amd64
```

Für ARM64 auf einem ARM64-Builder mit `arm64` wiederholen. Die Staging-Datei BUILD-ARCH verhindert, dass ein x64-Payload versehentlich als ARM64 verpackt wird. Für einen weiteren Lauf den vorhandenen Ordner `snap/local/publish` zunächst beiseite verschieben.

Der Workflow `Windows and Snap Store candidates` baut auf echten Ubuntu-x64- und Ubuntu-ARM64-Runnern, erzeugt Snap-Dateien und prüft deren Inhaltslisten. Er meldet nichts automatisch im Store an und lädt nichts dorthin hoch.

Auf einem Ubuntu-Desktop lokal testen:

```bash
sudo snap install ./gruetze-retrosaver_VERSION_amd64.snap --dangerous
gruetze-retrosaver
gruetze-retrosaver.screensaver
snap connections gruetze-retrosaver
```

Wayland und X11 separat ausprobieren; Vollbild verlassen, Einstellungen speichern, App neu starten. Keine devmode/classic-Ausnahme als Ersatz für den strict-Test verwenden. GNOME/Wayland bietet keine Einbindung fremder System-Bildschirmschoner. Unter anderen Desktops gilt `docs/SCREEN-SAVER-SETUP.md`. Der Snap startet nicht selbständig bei Inaktivität und verändert keine Sperreinstellungen.

Nach erfolgreichem lokalen Test kann der Kontoinhaber einen Testkanal verwenden:

```bash
snapcraft login
snapcraft register gruetze-retrosaver
snapcraft upload gruetze-retrosaver_VERSION_amd64.snap --release=edge
```

`register` nur ausführen, wenn der Name noch nicht im eigenen Konto reserviert ist. Anmeldung und eine eventuell angezeigte Vereinbarung selbst abschließen. Danach ARM64 ebenfalls in edge hochladen. Mit `snapcraft status gruetze-retrosaver` die Architektur-Zuordnung prüfen. Erst nach Desktop-Tests die konkreten Revisionen nach stable freigeben; `grade: stable` im YAML veröffentlicht nichts von selbst.

## Bilder und Texte

Die sieben Aufnahmen stammen direkt aus den jeweiligen Windows-/Linux-Avalonia-Ansichten, 2560×1600 PNG mit 2× Auflösung. Keine Mac-Einstellungsbilder als Windows-/Linux-Aufnahmen ausgeben.

```powershell
dotnet run --project tests/StoreScreenshots -c Release -p:WindowsStore=true -- artifacts/windows-store/screenshots
```

Linux mit grafischer Sitzung oder Xvfb:

```bash
xvfb-run -a dotnet run --project tests/StoreScreenshots -c Release -- artifacts/linux-store/screenshots
```

Die Aufnahmen verwenden eine isolierte JSON-Datei und ändern keine persönlichen Einstellungen. Vor Einreichung gegen den installierten finalen Paketstand prüfen. Die animierte README-Grafik ist für GitHub; für Store-Logos die statischen PNGs verwenden.

## Noch vor der Einreichung zu klären

- Echte Microsoft-Produktidentität und reservierter Snap-Name eintragen.
- Installations-, Update- und System-Screensaver-Test des signierten/Store-installierten Windows-Pakets; strict-Snap auf einem echten Desktop testen.
- Kickstart-Grafiken bleiben wie gewünscht nur in Windows/Linux enthalten. Die MIT-Lizenz des Programmcodes erteilt keine Lizenz für diese historischen Drittanbieterbilder (siehe KICKSTART-CREDITS.md). Vor öffentlicher Store-Freigabe müssen die nötigen Nutzungsrechte vorliegen; andernfalls Store-Builds ebenfalls ohne diese Grafiken erstellen. Die Vorbereitung behauptet keine Store-Freigabe dieser Inhalte.

## Offizielle Quellen

- https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/app-package-requirements?pivots=store-installer-msix
- https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
- https://learn.microsoft.com/en-us/windows/win32/devnotes/scrnsave-exe
- https://ubuntu.com/docs/snapcraft/9/reference/extensions/gnome-extension/
- https://ubuntu.com/docs/snapcraft/latest/reference/snapcraft-yaml/
