# Gruetze RetroSaver — Vorbereitung Mac App Store

Stand: 2. November 2026. Basis: veröffentlichtes Release 1.2.5.
Ziel bleibt die Begleit-App mit echtem macOS-System-Bildschirmschoner, C64 und
Amiga. Keine stillschweigende Umstellung auf eine reine Vollbild-App.
Geplanter Preis laut Produktentscheidung: einmalig 1,99 USD; verfügbare Preisstufe
und Länderpreise sind in App Store Connect zu bestätigen.

## Was bereits vorhanden ist

- Native .saver-Komponente in der Begleit-App; Installation über den Apple-Dialog.
- Gemeinsame Konfiguration, C64-BASIC, Amiga-Animationen und CRT-Einstellungen.
- Developer-ID-Signier-/Notarisierungsweg für direkten Vertrieb als DMG.
- Mac-CI auf Apple Silicon und Intel: Optionen, Lebenszyklus, Metal, Vollbild,
  gemeinsame Einstellungen und DMG-Prüfung erfolgreich für 1.2.5.
- Kompakte Dialoge, Logos, Produktname, Homepage sowie Release-Screenshots.

Das ist ein getesteter Developer-ID-Vertriebsstand. Es wurde noch kein Mac-App-
Store-Build hochgeladen oder von Apple freigegeben.

## Noch fehlende technische Nachweise

1. **Sandbox:** macos/Companion.entitlements enthält derzeit allow-jit, aber
   nicht com.apple.security.app-sandbox. Der gemeinsame Einstellungsweg ist
   /Users/Shared/GruetzeRetroSaver-<uid>/settings.json. Ein erfolgreicher Test
   ohne Sandbox beweist keinen erlaubten Zugriff mit Sandbox.
2. **System-Bildschirmschoner:** CompanionWindow übergibt das eingebettete
   C64Screensaver.saver mit /usr/bin/open an den macOS-Installer. Ob dieser
   Ablauf für dieses Store-Produkt akzeptiert wird, ist noch offen. Keine
   Ausnahme allein aus der Nutzerbestätigung ableiten.
3. **Store-Paket:** eigenes Store-Profil, passende App-/Installer-Signierung,
   Sandbox-Build und Xcode-kompatibler validierter Upload fehlen. Das bestehende
   sign-notarize.sh ist ausdrücklich Developer ID, kein Store-Upload.

Apple 2.4.5 verlangt Sandbox und ein eigenständiges App-Paket und beschränkt die
Installation zusätzlichen Codes bzw. in gemeinsam genutzte Orte. Der konkrete
.saver-Weg muss deshalb mit App Review und anschließend im echten Sandbox-Build
geprüft werden. Keine pauschale Zusage oder pauschaler Ausschluss anhand anderer
Store-Produkte. Historische Anbieterrecherche: APP-STORE-PLAN-DE.md.

## Inhalte und Einreichungsangaben

- Die historischen Kickstart-GIFs sind übernommene Originaldarstellungen. Ihre
  CREDITS.md nennt Quellen und Rechteinhaber und nimmt sie ausdrücklich von der
  MIT-Lizenz aus. Ein Nutzungsnachweis für die geplante kommerzielle Verteilung
  ist im Repository nicht dokumentiert. Vor Verkauf klären oder den betreffenden
  Inhalt erst nach Produktentscheidung ersetzen/entfernen; nichts heimlich ändern.
- Beschreibung, Suchbegriffe und Screenshots müssen die tatsächlich eingereichte
  Store-Version zeigen, einschließlich des bestätigten Installationsablaufs.
- Datenschutzerklärung/Supportseite und Datenschutzangaben in App Store Connect
  vorbereiten; Datenangaben erst nach Prüfung des finalen Store-Builds bestätigen.
- Paid Apps Agreement, Steuer-/Bankangaben, Altersfreigabe, Inhaltsrechte und
  endgültigen Preis im Konto des Inhabers vervollständigen.

## Nächste konkrete Schritte

1. Reguläre Einreichung mit vollständigen schriftlichen App-Review-Informationen
   vorbereiten. Eine Vorabanfrage oder ein Gespräch ist optional, keine Voraussetzung.
   Die vorhandene Anfrage kann bei Bedarf als Vorlage dienen; nichts wurde versendet.
2. Parallel die Nutzungsnachweise der historischen Bilder klären.
3. Auf Basis des bestätigten Wegs einen getrennten Sandbox-/Store-Build erstellen
   und auf dem Neo mit echtem installierten .saver den Einstellungszugriff prüfen.
4. Store-Metadaten/Medien finalisieren, Paket validieren und zunächst TestFlight
   bzw. den für den bestätigten Pakettyp vorgesehenen Testweg verwenden.
5. Erst danach zur Prüfung einreichen. Das bestehende Release bleibt unverändert.

## Geprüfte Primärquellen

- App Review Guidelines 2.4.5 und 5.2/5.2.1:
  https://developer.apple.com/app-store/review/guidelines/
- Developer ID gegenüber Mac App Store:
  https://developer.apple.com/macos/distribution
- Notarisierung betrifft den Developer-ID-Vertrieb außerhalb des Stores:
  https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution

Diese Zusammenfassung ist eine technische Einreichungsprüfung, keine Zusage einer
Store-Zulassung oder eines Nutzungsrechts an übernommenem Bildmaterial.
