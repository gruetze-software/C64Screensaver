# Mac-Produktbilder für Gruetze RetroSaver

Geplante Serie, Reihenfolge nach Dateinamen:

1. 01-c64-basic.png — tatsächliche BASIC-Eingabe mit CRT-Glühen
2. 02-amiga-runner.png — Jump & run mit Lauftext
3. 03-amiga-plasma.png — Plasma mit Lauftext
4. 04-amiga-boing.png — klassischer Boing-Ball mit Raster und Schatten
5. 05-c64-companion.png — tatsächliche C64-Steuerung in der Begleit-App
6. 06-amiga-companion.png — tatsächliche Amiga-Steuerung in der Begleit-App
7. 07-monitor-effects.png — gemeinsame CRT-Regler

Die Ansichten werden auf macOS mit dem nativen Avalonia-Backend und den echten
App-Klassen gerendert. Offscreen-Aufnahmen der App-Inhalte, keine Aufnahme des
gesamten Desktops oder eines physischen Mac. Keine nachgebildeten Betriebssystem-
Fenster, Werbeüberlagerungen oder veränderten Bedienelemente. Es werden keine
persönlichen Einstellungen geändert.

PNG, 1280×800, RGB ohne Transparenz. Mac-freigegebene Bildgröße laut Apple:
https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications

Auf einem Mac reproduzieren:

    dotnet run --project tests/StoreScreenshots -c Release

Ausgabe: artifacts/store-screenshots. Der Aufnahmeprozess bricht auf anderen
Betriebssystemen ab. Der Mac-Buildlauf erzeugt außerdem ein separates Artefakt
Gruetze-RetroSaver-Mac-Store-Screenshots. Die Original-Kickstart-Bilder werden in
dieser Serie bewusst nicht gezeigt, während deren Nutzungsnachweise offen sind.

Die neue Mac-Serie für 1.2.6 enthält keine Kickstart-Auswahl mehr. Vor endgültiger Store-
Einreichung müssen sie mit der eingereichten Sandbox-Version abgeglichen werden.
