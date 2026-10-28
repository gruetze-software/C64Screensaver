# Architekturentscheidung: Begleit-App, C64 und Amiga

Stand: 28. Oktober 2026. Zielarchitektur, noch keine implementierte Begleit-App.
Der Lauftext ist eine Kernfunktion des Amiga-Modus.

## Produkt und Prozesse

Eine Begleit-App bietet Systemauswahl, alle Optionen, große Echtzeitvorschau,
Profile sowie einen vom Nutzer bestätigten Installations-/Update-Ablauf.
C64 und Amiga bleiben echte, von macOS gestartete System-Bildschirmschoner.
Für die erste Ausbaustufe sind zwei getrennte .saver-Module vorgesehen, damit
beide in den Systemeinstellungen einzeln auswählbar sind.

Die Module enthalten ihren Renderer und benötigen keine laufende Begleit-App.
Kein Fenster der App wird als System-Bildschirmschoner ausgegeben.
Keine neue Inaktivitätsüberwachung, kein Autostart-Helfer, kein separates
Passwort- oder Sperrverfahren.

## Gemeinsamer Renderkern

Die Plattformhülle verwaltet Lebenszyklus, native Bildausgabe und Ressourcen.
Der gemeinsame .NET-Kern enthält:
- C64-Szene mit bestehendem BASIC-Interpreter und Codeeingabe;
- Amiga-Szene mit Bootmotiv, Disketten-/Ladeübergang und Demo-Parts;
- Lauftextdarstellung als eigenständige Ebene;
- gemeinsame CRT-Ausgabe und geprüfte Renderbudgets.

App-Vorschau und .saver müssen denselben Szenenkern benutzen. Vorschauänderungen
sind lokale Parameteränderungen innerhalb der App, keine zweite Effektimplementierung.
Stoppen, Fensterwechsel, mehrere Displays und laufende Hintergrundaufträge bleiben
Bestandteil der Lebenszyklustests.

## Einstellungen an einem Ort

Die Begleit-App verwaltet:
- System/Profil;
- C64: Programm, Laufzeit, Codegeschwindigkeit und Randfarben;
- Amiga: Bootmotiv, Effekt-Auswahl, Reihenfolge und Laufzeit;
- Lauftext: langer eigener Text, Tempo, Schriftgröße, Palette, Sinusbewegung und
  Sichtbarkeit pro Effekt;
- Darstellung: 4:3/Strecken, Rand, CRT-Regler und Renderqualität.

Lauftext bekommt ein eigenes Datenfeld. Nicht die bestehende auf 28 Zeichen
begrenzte Firmenkartenzeile wiederverwenden. Mehrere Texte bzw. wechselnde
Botschaften vorsehen; zunächst eine verlässlich lesbare Sinus-Scroller-Variante
mit selbst erstellten Grafiken und Schriftgestaltung implementieren.

## macOS-Optionen

ScreenSaverView.configureSheet erwartet ein NSWindow, das macOS als Sheet zeigt.
Den Systemknopf deshalb nicht so behandeln, als wäre er frei durch eine beliebige
App-Aktion ersetzbar.

Das kleine Sheet zeigt System, aktives Profil und einen ausdrücklichen Knopf
In Begleit-App bearbeiten. Dieser soll die installierte App über ihre Bundle-ID
bzw. die von Launch Services ermittelte URL starten und den passenden Bereich
öffnen. Fehler bzw. nicht installierte App sichtbar behandeln. Keine App beim
Start oder während der Vollbild-/Sperrbildschirmdarstellung automatisch öffnen.

Das vorhandene vollständige Optionsfenster während der Umstellung behalten.
Erst nach erfolgreichem Test von App-Start und Datenaustausch auf den kleinen
Dialog reduzieren. Eine einfache Rückfallmöglichkeit und Datenfreigabe bleiben
im .saver erreichbar.

[Apple – configureSheet](https://developer.apple.com/documentation/screensaver/screensaverview/configuresheet)
[Apple – NSWorkspace](https://developer.apple.com/documentation/appkit/nsworkspace)

## Datenaustausch: zuerst prototypisch prüfen

Die Begleit-App und ein klassisches .saver laufen nicht automatisch im selben
Sandbox-Kontext: Das .saver wird in Apples Host geladen. Derselbe
NSUserDefaults-Suitename allein beweist deshalb keinen gemeinsam zugänglichen
Einstellungsspeicher. Auch ein eigenes Entitlement am Plug-in berechtigt den
Apple-Host nicht automatisch zu einer App Group.

Eine App Group ist nur dann einsetzbar, wenn beide ausführenden Prozesse
tatsächlich dafür berechtigt sind. Das ist beim klassischen Systemhost nicht
als gegeben anzunehmen.

Der konkrete erste Austausch-Prototyp soll einen vom Nutzer freigegebenen
Konfigurationsordner und eine versionierte Konfigurationsdatei verwenden.
Beide Seiten benötigen jeweils zulässigen Zugriff; Zugriffs-Bookmarks sind in
ihrem eigenen Prozesskontext zu speichern. Kein direkter Zugriff auf fremde
Container-/Preferences-Dateien, keine Bearbeitung der Apple-Host-Entitlements.

App schreibt vollständig und atomar; Screensaver liest validierte Schnappschüsse.
Fehlende Freigabe, beschädigte Dateien, Versionsunterschiede und entfernte
Begleit-App dürfen nicht zu schwarzer Anzeige führen: letzte gültige lokale
Konfiguration bzw. sichere Standardeinstellungen verwenden.

Modulspezifische lokale Daten mit ScreenSaverDefaults verwalten. Die bestehende
native Suite-Einstellung und ihre Benutzerdaten bei der Migration berücksichtigen.

Ein tatsächlicher Schreib-/Lesetest mit sandboxed Begleit-App, installiertem .saver
und macOS 27 auf dem Neo ist Voraussetzung für die endgültige Wahl. Den
erfolgreichen Datei-/Bookmark-Austausch nicht allein aus einem unsandboxed
Testprogramm ableiten.

[Apple – ScreenSaverDefaults](https://developer.apple.com/documentation/screensaver/screensaverdefaults)
[Apple – UserDefaults / Sandbox considerations](https://developer.apple.com/documentation/foundation/userdefaults)
[Apple – Configuring app groups](https://developer.apple.com/documentation/xcode/configuring-app-groups)

## Sinnvolle Reihenfolge

1. Begleit-App-Grundgerüst, echte App-Vorschau und Installationserkennung für C64.
2. App-Start über das bestehende Optionssheet und sicherer Einstellungsaustausch
   auf dem Neo, einschließlich bestehender C64-Settings.
3. Zentralen vollständigen Editor integrieren; C64-Rückfalloptionen erhalten.
4. Amiga-Prototyp mit Copper-Balken und Sinus-Lauftext als erstem Demo-Part.
5. Weitere Amiga-Effekte, Bootmotive und Demo-Mix ergänzen.
6. Den konkreten Store-Vertriebsweg separat mit App Review klären.

Die Architekturentscheidung ändert weder die Store-Freigabe noch den Preis
automatisch. Eine reine Vollbild-App bleibt ausdrücklich ausgeschlossen.
