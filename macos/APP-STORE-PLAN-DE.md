# Mac App Store: echter System-Bildschirmschoner

Stand: 28. Oktober 2026. Gewünschtes Produkt: echter macOS-System-Bildschirmschoner.
Gewünschter Preis: 1,99 USD einmalig. Eine reine manuell gestartete Vollbild-App
ist vom Nutzer ausdrücklich nicht gewünscht. Noch keine Store-Einreichung.

## Ergebnis der konkreten Anbieterprüfung

Ein pauschaler Ausschluss des Mac App Store ist nicht ausreichend begründet.
Es gibt ein aktuelles Gegenbeispiel, dessen Anleitung echte Systemintegration
beschreibt:

- FlipCloc wird im Mac App Store für 4,99 USD mit zusätzlichen In-App-Käufen
  angeboten. Laut Hersteller wählt der Käufer beim ersten Start Screensaver.
  Die App stößt anschließend die Installation an; macOS zeigt den normalen
  Bestätigungsdialog. Danach wird FlipCloc in den Systemeinstellungen als
  Bildschirmschoner ausgewählt. Auch Aktualisierungen des Moduls werden laut
  Anleitung über die App angestoßen.
- Albumscape verwendet dagegen zwei Vertriebswege. Laut Hersteller gehört der
  native Bildschirmschoner nur zur direkt vertriebenen Developer-ID-Version,
  weil die Store-Version kein .saver installieren darf. Pro kostet dort
  einmalig 8 USD.

Quellen:
[FlipCloc im App Store](https://apps.apple.com/us/app/flipcloc-clock-screensaver/id6786569507?mt=12)
[FlipCloc – offizielle Mac-Installationsanleitung](https://flipcloc.com/how-to-use/mac-screensaver)
[Albumscape – Support und Vertriebsunterschiede](https://albumscape.haogre.com/support)

Die öffentlich beschriebene FlipCloc-Installation wurde hier nicht selbst
ausgeführt; die Implementierung und eventuelle individuelle Review-Absprachen
sind nicht bekannt. Eine vorhandene Store-Zulassung eines anderen Produkts ist
keine Zusage von Apple für C64Screensaver.

## Konsequenz für C64Screensaver

Das .saver/DMG bleibt das bestehende, echte Produkt. Ein bloßes Hochladen des
DMG als App ist weiterhin kein App-Store-Weg.

Zu prüfen ist stattdessen eine sandboxed Begleit-App für Konfiguration und
Vorschau, die das vorhandene Modul mitliefert und dessen Installation
ausdrücklich durch den Nutzer über den macOS-Dialog anstoßen lässt. Das ist
eine zu untersuchende Architektur anhand des belegten Anbieters, noch keine
als zulässig oder implementiert bestätigte Lösung.

Der entscheidende Unterschied zur verworfenen Produktidee: Die Begleit-App
ersetzt den System-Bildschirmschoner nicht durch einen eigenen Vollbildmodus.
Das tatsächliche .saver bleibt in den Systemeinstellungen auswählbar.

## Offene Store-Regeln und nächster Prüfpunkt

Apple verlangt unter 2.4.5 unter anderem Sandbox, ein eigenständiges App-Paket
und keine Installation von Code oder Ressourcen in gemeinsam genutzten Orten.
Öffentliche APIs sind nach 2.5.1 Pflicht. Diese Regeln dürfen weder durch
private Extension-Punkte noch durch automatische Dateikopien umgangen werden.

[Apple App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)

Vor einem umfangreichen Umbau sollte der konkrete Begleit-App-Ansatz mit App
Review geklärt werden. Die Unterschiede zwischen den offiziellen Angaben von
FlipCloc und Albumscape müssen dabei berücksichtigt werden. Die Behauptung,
ein vom Nutzer bestätigter Systemdialog sei automatisch eine erlaubte Ausnahme,
ist derzeit nicht belegt.

Eine mögliche Anfrage, die der Kontoinhaber selbst einreichen kann:

Wir entwickeln einen echten macOS-Bildschirmschoner mit dem öffentlichen
ScreenSaver-Framework. Wir möchten eine sandboxed Mac-App für Konfiguration und
Vorschau anbieten, die das feste, mitgelieferte .saver-Modul über öffentliche APIs
dem normalen macOS-Installationsdialog übergibt. Die Installation erfordert
ausdrückliche Nutzerbestätigung; die App kopiert nicht selbst in Library-Ordner.
Ist dieser Ablauf unter 2.4.5(ii)/(iv) zulässig? Falls nicht, gibt es einen
dokumentierten zulässigen Weg für die Verteilung echter System-Bildschirmschoner
über den Mac App Store? Ein vergleichbarer Installationsablauf wird öffentlich
in der Mac-Anleitung von FlipCloc beschrieben.

Keine Anfrage wurde aus diesem Projekt heraus an Apple gesendet.

## Konto, Preis und Medien

Vor einem Verkauf über den App Store sind Paid Apps Agreement sowie die von
Apple geforderten Steuer-/Bankangaben durch den Kontoinhaber zu vervollständigen.
Später bei der konkret angelegten App USA als Preisbasis und 1,99 USD wählen,
sofern angeboten, und die übrigen Länderpreise prüfen.

[Apple – Set a price](https://developer.apple.com/help/app-store-connect/manage-app-pricing/set-a-price)

Eine Store-App benötigt eigene passende App-Signierung, Sandbox, Bundle-ID,
Provisionierung und einen validierten Upload. Unser Developer-ID-DMG-Skript
ist weiterhin für Website-Downloads zuständig und ersetzt das nicht.

Das vorhandene Shop-Video ist 39 Sekunden lang, hat eine geraffte Codeeingabe
und wurde direkt aus dem Renderer exportiert. Es ist kein fertiges
App-Store-Preview. Für eine Einreichung müssen aktuelle Medienanforderungen
geprüft und die endgültig eingereichte App korrekt dargestellt werden.

Die technische Freigabe, Produktbeschreibung und Einreichung werden erst auf
Basis des geklärten Installationswegs vorbereitet. Es wird weder eine
Store-Zulassung garantiert noch stillschweigend auf eine Vollbild-App umgestellt.
