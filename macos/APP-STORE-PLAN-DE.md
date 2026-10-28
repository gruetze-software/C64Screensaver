# Mac App Store: Veröffentlichungsplan

Stand: 28. Oktober 2026. Gewünschter Preis: 1,99 USD als einmaliger Kauf,
kein Abonnement. Noch keine Store-App angelegt, kein Upload und keine Einreichung.

## Zuerst die Produktform entscheiden

Das vorhandene native Mac-Paket ist ein ScreenSaverView-Plug-in mit der
Paketart BNDL und der Endung .saver. Das notarisierten DMG ist der
Distributionsweg für die eigene Homepage, kein App-Store-Upload.

Für den Mac App Store verlangt Apple eine eigenständige, sandboxed App und
untersagt das Installieren zusätzlichen Codes in gemeinsam genutzten Orten.
Eine App, die unser .saver nur nach ~/Library/Screen Savers kopiert oder dort
durch macOS installieren lässt, ist deshalb kein belastbarer Store-Weg.
Die geltende Vorgabe ist insbesondere 2.4.5 (i), (ii) und (iv).

Quelle: [Apple App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)

Die denkbare zusätzliche Store-Variante ist eine eigenständige App:
- C64-Bildschirm in einem Fenster oder manuell gestarteten Vollbildmodus;
- vorhandene BASIC-Programme und CRT-Einstellungen;
- vollständige Funktion innerhalb des eigenen App-Pakets und Containers;
- kein Installer für einen System-Bildschirmschoner;
- keine automatische Eintragung als Bildschirmschoner in den Systemeinstellungen.

Diese Produktänderung ist noch nicht beauftragt. Sie darf im Store nicht als
installierter System-Bildschirmschoner versprochen werden. Eine Veröffentlichung
bleibt von Apples Prüfung abhängig. Die funktionierende .saver-Version soll bei
diesem Weg für den gesonderten Website-Vertrieb erhalten bleiben.

Ein Umbau auf undokumentierte ScreenSaver-App-Extension-Schnittstellen wäre
ebenfalls kein Store-Weg; Apple erlaubt grundsätzlich nur öffentliche APIs
(2.5.1). Ein Sandbox-Workaround würde diese Einschränkung nicht lösen.

## Was bereits vorbereitet werden kann

Im Browser App Store Connect öffnen. Als Account Holder unter Business bzw.
Agreements, Tax and Banking prüfen:
- Paid Apps Agreement selbst lesen und annehmen;
- die verlangten Steuer- und Bankdaten persönlich vervollständigen;
- prüfen, ob die Verträge für kostenpflichtige Apps aktiv sind.

Nicht veröffentlichen oder einen Vertrag bestätigen, dessen Angaben noch nicht
geprüft sind. Zugangsdaten, Bankdaten und Steuerdaten gehören nicht ins Repository.

Für den Preis ist später bei der konkreten App einzustellen:
Monetization → Pricing and Availability → Add Pricing.
USA als Basisregion und 1,99 USD wählen, sofern der Preis im Menü angeboten wird.
Die von Apple vorgeschlagenen Preise anderer Länder vor der Bestätigung prüfen;
1,99 USD ist nicht automatisch 1,99 EUR. Die Gebühren und Steuern beeinflussen
den tatsächlichen Erlös.

Quelle: [Apple – Set a price](https://developer.apple.com/help/app-store-connect/manage-app-pricing/set-a-price)

## Technische Arbeit nach Freigabe der Store-Variante

1. Eigenständigen Mac-App-Startpunkt und App-Fenster erstellen; Renderer, BASIC und
   Einstellungen möglichst weiterverwenden.
2. App Sandbox und ausschließlich benötigte Berechtigungen einrichten. Keine
   Inaktivitätsüberwachung, Autostart- oder Installerfunktion hinzufügen.
3. Eigenen App-Bundle-Identifier festlegen und im Developer-Konto registrieren,
   ohne den bestehenden .saver-Identifier zu überschreiben.
4. Einen Xcode-basierten Archiv-/Veröffentlichungsweg und passende App-Store-
   Signierung/Provisionierung vorbereiten. Unser Developer-ID-DMG-Skript ersetzt
   diesen Weg nicht.
5. Auf dem Mac die gesamte Store-App testen, insbesondere Start/Beenden,
   Vollbild/Escape, Optionen, mehrere Displays und CPU-/Speicherverbrauch.
6. Die endgültige App archivieren, validieren und über Xcode bzw. einen dafür
   geeigneten Apple-Uploadweg in App Store Connect hochladen.

Die jetzigen erfolgreichen .saver-Tests bestätigen keinen App-Sandbox-Betrieb
einer künftigen Store-App. Ein notarisiertes Website-Paket ist ebenfalls keine
App-Review-Zulassung.

## Store-Seite und Einreichung

Erst nach der Produktentscheidung belastbare App-Beschreibung und Review Notes
schreiben. Benötigt werden insbesondere:
- Name, Untertitel, Beschreibung und Kategorie passend zur tatsächlichen App;
- Support- und Datenschutzseite mit funktionierenden URLs;
- zutreffende Datenschutz- und Altersangaben;
- Screenshots der endgültigen Store-App;
- Preis und Länderfreigaben;
- Erklärung der fest eingebauten BASIC-Programme für App Review;
- Kontaktdaten und gegebenenfalls weitere von App Store Connect geforderte Angaben.

Das vorhandene Shop-Video ist 39 Sekunden lang, hat eine geraffte Codeeingabe und
wurde direkt aus dem Renderer exportiert. Es ist deshalb noch kein fertiges
App-Store-Preview. Ein Store-Preview muss die aktuellen Medienanforderungen erfüllen
und die tatsächlich eingereichte App zeigen. Für die Einreichung wird ein passender
Clip aus der endgültigen Store-App benötigt.

Nach Prüfung aller Angaben zunächst manuelle Veröffentlichung nach Freigabe
wählen, damit der Verkaufsstart nicht unbemerkt erfolgt. Einreichen und den
Veröffentlichungszeitpunkt gemeinsam mit dem Kontoinhaber festlegen.
