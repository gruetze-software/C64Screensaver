# Integrierte Mac-Distribution ab 1.2.1

Einmal auf dem Mac den aktuellen Stand holen und beide Bestandteile bauen:

    git pull
    bash macos/build-native.sh arm64
    bash macos/build-companion.sh arm64
    bash macos/test-shared-settings.sh

Danach die gesamte App samt eingebettetem Bildschirmschoner signieren und die
kombinierte DMG notarisierten lassen (Befehl in einer Zeile):

    bash macos/sign-notarize.sh "$PWD/artifacts/macos-companion/osx-arm64/Gruetze RetroSaver.app" "Developer ID Application: Christoph Schaefer (8GZ7AD23D7)" C64Screensaver-notary

Das vorhandene Schlüsselbundprofil wird weiterverwendet; kein Passwort in Skripte schreiben.
Das Ergebnis liegt in artifacts/macos-signed/distribution.*/Gruetze-RetroSaver.dmg.
Nur diese vollständige DMG weitergeben. Die Begleitapp enthält den Installationsknopf
für den echten System-Bildschirmschoner und beide verwenden dieselben Einstellungen.
Der Ablauf darunter beschreibt noch den bisherigen Einzel-.saver-Weg; dieser bleibt
für Entwickler unterstützt, ist aber nicht mehr die normale Distribution.

.NET-10-JIT-Berechtigung: https://learn.microsoft.com/en-us/dotnet/core/deploying/macos

> Stand 1.2.0: Der gemeinsame native Bildschirmschoner bietet jetzt C64 und Amiga
> im Optionsdialog an. Amiga enthält ausschließlich Jump & run und Plasma.
> Die Begleit-App wird mit macos/build-companion.sh zusätzlich als .app gebaut.
> Beide verwenden derzeit getrennte Einstellungen; Store-Einreichung und die
> gemeinsame Einstellungsablage sind noch offen. Frühere Entwurfsabschnitte unten
> beschreiben teilweise noch den Stand vor dieser Umsetzung.

# Apple-Developer-Weg für C64Screensaver

Ziel: Den nativen Mac-Bildschirmschoner als signierten und von Apple notarisierten
Download auf der eigenen Website anbieten. Dafür ist keine Veröffentlichung im
Mac App Store nötig. Diese Anleitung betrifft das native .saver-Modul im Branch
codex/macos-native-screensaver.

## 1. Mitgliedschaft beantragen

- Mit deinem Apple Account auf https://developer.apple.com/programs/enroll/ anmelden.
- Zwei-Faktor-Authentifizierung einrichten und Identitätsprüfung abschließen.
- Einzelperson/Einzelunternehmen oder Organisation entsprechend deiner tatsächlichen
  Rechtsform wählen. Für eine Organisation verlangt Apple eine anerkannte juristische
  Person und in der Regel eine D-U-N-S-Nummer. Ein Markenname allein reicht nicht.
- Apple nennt 99 USD pro Mitgliedsjahr; den örtlichen Preis zeigt die Anmeldung.
  Das Konto gilt für deine weiteren Apple-Projekte ebenfalls.
- Freischaltung und Annahme der aktuellen Verträge abwarten.

Einzelunternehmer melden sich bei Apple normalerweise als Einzelperson an. Der rechtliche
Name kann insbesondere als Verkäufername im App Store sichtbar sein. Vor der Anmeldung
prüfen, wenn dir die öffentliche Namensdarstellung wichtig ist.

Quelle: [Apple – Enrollment](https://developer.apple.com/help/account/membership/program-enrollment)

## 2. Auf deinem Mac einrichten

Xcode installieren, einmal öffnen und die angeforderten Komponenten installieren.
Unter Xcode → Settings → Accounts deinen Apple Account hinzufügen und dein Team wählen.
Das .NET-10-SDK wird für den Quellcode-Build ebenfalls benötigt.

In Xcode über Manage Certificates ein Zertifikat vom Typ Developer ID Application
anlegen. Falls dieser Typ dort nicht angeboten wird, als Account Holder im
Developer-Portal unter Certificates, Identifiers & Profiles erstellen; dazu im
Schlüsselbund eine Certificate Signing Request erzeugen und das ausgestellte
Zertifikat wieder importieren. Der private Schlüssel muss auf deinem Mac vorhanden sein.

Developer ID Application ist für unser Plug-in und seine Bibliotheken richtig.
Apple Development, Mac App Distribution und selbst signierte Zertifikate sind dafür
kein Ersatz. Developer ID Installer brauchst du erst für ein signiertes PKG-Installationspaket;
die hier verwendete DMG benötigt es nicht.

Im Terminal die verfügbare Identität anzeigen:

    security find-identity -v -p codesigning

Die vollständige Zeile Developer ID Application: DEIN NAME (TEAMID) merken.
Den privaten Schlüssel geschützt sichern. Zertifikate mit privatem Schlüssel,
.p12-Dateien, Kennwörter und API-Schlüssel gehören niemals ins Git-Repository.

Quelle: [Apple – Developer ID](https://developer.apple.com/developer-id/)

## 3. Native Testversion bauen

Im Repository auf dem Mac:

    git switch codex/macos-native-screensaver
    git pull
    bash macos/build-native.sh

Auf Apple Silicon liegt das Ergebnis hier:

    artifacts/macos-native/osx-arm64/C64Screensaver.saver

Intel verwendet osx-x64. Die Pakete sind bislang architekturspezifisch. Der Build führt
Render- und Optionsdialogtests aus und signiert zunächst nur ad hoc für lokale Tests.

Vor der Veröffentlichung eine endgültige Versions- und Buildnummer in
macos/NativeHost/Info.plist festlegen. Die vierte Testversion ist weiterhin ein
experimenteller Build auf Basis 1.1.0, keine neue stabile Veröffentlichung.

## 4. Notarisierungs-Zugang im Schlüsselbund speichern

Für den einfachen lokalen Weg ein app-spezifisches Passwort für deinen Apple Account
unter https://account.apple.com/ erstellen. Nicht dein normales Account-Passwort im
Skript hinterlegen.

Dann im Mac-Terminal interaktiv ausführen:

    xcrun notarytool store-credentials C64Screensaver-notary

Apple-ID, Team-ID und das app-spezifische Passwort bei den Abfragen eingeben.
Das Profil liegt anschließend im Schlüsselbund. Es wird nicht ins Repository geschrieben.
Alternativ unterstützt Apple App-Store-Connect-API-Schlüssel; für den ersten lokalen
Durchlauf ist das nicht nötig.

## 5. Signieren, als DMG verpacken und notarisiert prüfen

Im Repository ausführen; Namen und Team-ID durch die Ausgabe aus Schritt 2 ersetzen:

    bash macos/sign-notarize.sh \
      "$PWD/artifacts/macos-native/osx-arm64/C64Screensaver.saver" \
      "Developer ID Application: DEIN NAME (TEAMID)" \
      C64Screensaver-notary

Das vorbereitete Skript:
- kopiert den Build in einen neuen Ausgabeordner, damit das Original erhalten bleibt;
- signiert Skia, NativeAOT und das .saver-Bundle mit derselben Developer-ID-Identität,
  sicherem Zeitstempel und Runtime-Option;
- erzeugt und signiert ein DMG;
- lädt es zu Apples Notarisierungsdienst und wartet auf Accepted;
- heftet das Ticket an das DMG, validiert es und prüft Gatekeeper;
- schreibt eine SHA-256-Prüfsumme neben das fertige Paket.

Die Ausgabe liegt unter artifacts/macos-signed/distribution.*/C64Screensaver.dmg.
Für Intel mit dem osx-x64-Bundle wiederholen. Nicht die alten ad-hoc-ZIPs veröffentlichen.

Das Skript ist vorbereitet, aber noch nicht mit deinem Developer-ID-Zertifikat und
Apple-Konto ausgeführt. Ein erfolgreicher experimenteller Build ist keine bestätigte
Notarisierung. Insbesondere muss das Laden der NativeAOT-/Skia-Bibliotheken im echten
Apple-Bildschirmschoner-Host auch mit den endgültigen Signaturen geprüft werden.
Das Plug-in erbt die Einschränkungen seines Hosts; wir fügen keine pauschalen
JIT- oder Library-Validation-Ausnahmen hinzu.

Falls Apple ablehnt, die Submission-ID aus notary-result.json nehmen:

    xcrun notarytool log SUBMISSION-ID \
      --keychain-profile C64Screensaver-notary notarization-log.json

Die konkrete Beanstandung beheben und neu signieren/einreichen. Ein Reject nicht durch
Abschalten von Gatekeeper umgehen.

Quelle: [Apple – Customizing the notarization workflow](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)
und [Notarization requirements / plug-ins](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)

## 6. Den tatsächlichen Download testen

Das fertige DMG auf einem separaten Mac oder Benutzerkonto über den Browser herunterladen.
DMG öffnen und das darin enthaltene .saver per Doppelklick installieren.
Die heruntergeladene Datei dabei nicht von ihren Quarantäne-Metadaten befreien:
Wir wollen die echte Sicherheitsprüfung testen.

Prüfen:
- keine Blockierung wegen nicht verifiziertem Entwickler; ein gewöhnlicher
  Download-/Installationshinweis darf trotzdem erscheinen;
- C64 in Vorschau und Vollbild, Optionsdialog und gespeicherte Regler;
- bildfüllender Modus ohne Balken, alle Programme und CRT-Dot-Übergang;
- Start/Stopp, mehrere Monitore und normale Passwortsperre;
- Homepage-Link und korrekte Versionsnummer.

Nach dem Ersetzen einer früheren Testversion den Mac neu starten, damit der Host
keine alte native Bibliothek weiterverwendet. Erst nach diesem Test das notarisiert
geprüfte DMG und seine Prüfsumme auf der Website veröffentlichen.

## Später: GitHub-Automatisierung

Zunächst einmal lokal erfolgreich signieren und notarisiert testen. Danach können
Developer-ID-Zertifikat und Passwort sowie Notarisierungszugang als geschützte
GitHub-Secrets in einen gesonderten Veröffentlichungsworkflow übernommen werden.
Private Schlüssel nur temporär im Runner-Schlüsselbund importieren und nach dem Job
entfernen. Keine Zugangsdaten in Logs oder öffentliche Build-Artefakte übernehmen.
Der aktuelle experimentelle Workflow verwendet weiterhin ausschließlich Ad-hoc-Signierung.
