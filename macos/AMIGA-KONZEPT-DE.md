# Konzept: C64 und Amiga als echte System-Bildschirmschoner

Stand: 28. Oktober 2026. Produktidee des Nutzers: Eine gemeinsame Begleit-App
bietet C64 und Amiga an. Beide sollen echte System-Bildschirmschoner bleiben.
Die Amiga-Variante und die Begleit-App sind noch nicht implementiert.

## Rollen

- Begleit-App: Auswahl des Systems, Konfiguration, Vorschau, vom Nutzer
  angestoßene Installation und Aktualisierung der Module.
- C64: vorhandene BASIC-Eingabe mit Fehlerkorrektur, RUN und drei Programmen.
- Amiga: ausgewähltes Bootmotiv, anschließend eine Demo-Schleife statt BASIC.
- Store-Vertrieb: Begleit-App-Installationsweg weiterhin konkret mit Apple klären;
  die Anbieterrecherche allein ist noch keine Zulassungszusage.
- Bestehende C64-Einstellungen erhalten; kein stilles Umbenennen oder Ersetzen.

## Amiga-Ablauf

1. Gewähltes Startmotiv einige Sekunden anzeigen.
2. Eine virtuelle Diskette wird eingelegt; optional kurze Ladepause.
3. Ein eigener Demo-Part beginnt.
4. Nach konfigurierbarer Zeit zum nächsten gewählten Part wechseln.
5. Optional mit erneuter Bootsequenz beginnen, alternativ ohne Bootpause wechseln.

Der statische Bootbildschirm ist die Einleitung, kein Dauermodus: Der eigentliche
Bildschirmschoner muss das Bild verändern. Kein Sound per Voreinstellung.

Bootmotive als erste Auswahl: Stil der frühen Hand-/Diskettenaufforderung und Stil
der späteren violetten Diskettenaufforderung. Die genaue Versionszuordnung und
Verwendung originaler Kickstart-Grafiken müssen vor Übernahme geprüft werden.
Keine Kickstart-ROMs voraussetzen oder mitliefern. Eigene Demo-Effekte verwenden
selbst erstellte Geometrie, Texturen und Grafiken. Original-Demo-Grafiken und Musik
nur bei ausdrücklich geklärten Nutzungsrechten einbinden.

## Eigene Demo-Parts: sinnvolle erste Auswahl

| Part | Bildidee | Einstellbar |
| --- | --- | --- |
| Boing | Eigener rot-weiß karierter, springender und rotierender Ball mit bewegtem Schatten | Tempo, Größe, Hintergrund |
| Copper & Scroller | Farbige Rasterbalken, Sterne und weich schwingender Lauftext | Palette, Text, Geschwindigkeit |
| Starflight | Räumliches Sternenfeld mit sich drehendem Drahtgitterobjekt | Sternenzahl, Flugtempo, Objekt |
| Rotozoom | Rotierendes, heranzoomendes Muster mit weichen Übergängen | Textur, Palette, Rotation |
| Plasma & Tunnel | Fließendes Farbplasma oder perspektivischer Tunnel | Palette, Geschwindigkeit, Intensität |

Für den ersten Prototyp Boing, Copper & Scroller und Starflight priorisieren.
Rotozoom und Plasma/Tunnel danach ergänzen. Demo-Mix spielt alle ausgewählten
Parts ab; Einzelmodus hält den gewählten Effekt in Bewegung. Auswahl und
Zeitsteuerung sollen unabhängig von der Bootmotiv-Auswahl sein.

Originalität: Effekttypen und Stimmung der Demoszene aufgreifen, neue Mathematik/
Implementierung und eigenes Artwork verwenden. Keine vorhandenen Demo-Videos
als Effekt abspielen und keine kompletten Szenedemos ungeprüft mitverkaufen.

Referenzen für Sichtung, keine zugesicherte Übernahme:
- [State of the Art / Spaceballs (1992)](https://www.pouet.net/prod.php?which=99)
- [Arte / Sanity (1993)](https://www.pouet.net/prod.php?which=1477)

## Zusätzliche persönliche Ideen

- Firmenlogo als bewegtes Vektorobjekt, eigener Text als Sinus-Scroller.
- Demo-Disk-Sammlung: Ein virtuelles Diskettenlabel bezeichnet den nächsten Part.
- Sehr kurze, optionale Guru-Meditation als humorvoller Szenenwechsel.
- Presets: A500-Demo, A1200-Farben und ruhiger Nachtmodus.
- Demo-Mix mit weichen Überblendungen; harte Weißblitze optional statt obligatorisch.

## Technik und Performance

Rendering, CRT-Einstellungen, Lebenszyklus und Speicherverwaltung gemeinsam
nutzen; C64-BASIC und Amiga-Demoeffekte bleiben getrennte Szenen. Amiga-Parts
benötigen keinen Emulator und keine fremden ausführbaren Demo-Dateien.

Zuerst einen eigenständigen, messbaren Effekt-Prototyp erstellen. Performance auf
dem Neo prüfen, bevor Bootsequenzen und alle UI-Optionen integriert werden.
Die gemessenen CPU-Kosten des bestehenden CRT-Endpasses verschwinden nicht
dadurch, dass davor statt BASIC ein Amiga-Effekt gezeichnet wird. Bestehende
Renderbudgets berücksichtigen und GPU-Rendering als gezielte Verbesserung
bewerten. Keine neue permanente Hintergrundüberwachung einbauen.

Noch offen: exakte Bootmotive/Versionen und deren Grafikrechte, endgültiger
Produktname, endgültiger Umfang der ersten Verkaufsversion und Store-Freigabe.
Der bisher gewünschte Preis 1,99 USD wird durch dieses Konzept nicht geändert.


## Implementiert am 29. Oktober 2026

In der Begleit-App auswählbar: Start überspringen, nachgezeichnete
Disketten-Hand (1.3-Stil), violettes Laufwerk (2.0-Stil) und Regenbogen/Laufwerk
(3.1-Stil). Es handelt sich um eigene Motive, keine ROM-genauen Abbilder.
Ein fünfsekündiger Start mit Ladebewegung geht in die Demo über.

Copper/Sinus-Lauftext, rotierender Boing-Ball mit Schatten und Gitter,
Sternenflug mit Drahtwürfel und Plasma sind implementiert. Alle unterstützen
denselben editierbaren Lauftext. Dieser läuft über Demo-Wechsel hinweg weiter.
Einzelprogramme wiederholen sich; die Gesamtauswahl wechselt durch alle vier.
Laufzeit und Startmotiv sind einstellbar. Optional schließen sich die
CRT-Abschalt-/Einschalteffekte mit dem nächsten Startbild an.

Gemeinsamer Renderer für Avalonia und native Mac-Bibliothek; ein separates
Amiga.saver-Modul und der Einstellungs-Austausch mit der Mac-Begleit-App sind
weiterhin offen. Performance-Messungen und Qualitätsstufen siehe PERFORMANCE.md.


## Korrektur der Startsequenz vom 29. Oktober

Die freien Nachzeichnungen sind ersetzt: 1.3 verwendet den statischen
Original-Bildschirm mit kopfüber beschrifteter Workbench-Diskette. 2.0 und 3.1
verwenden die historischen GIF-Animationen mit großem Regenbogen-Häkchen,
Laufwerk, Versions- und Copyright-Texten. Keine RETRO-Etiketten, zusätzlichen
STYLE-Titel oder erfundenen Fortschrittsbalken. 1.3 bleibt fünf Sekunden stehen;
2.0/3.1 spielen einen vollständigen Animationszyklus vor dem Demo-Start.
Quellen und Zuordnung: C64Screensaver/Assets/Kickstart/CREDITS.md.

## Selbstspielendes Jump-and-Run

Fünfte Demo: eigene Pixelfigur, scrollende Plattformen mit Lücken und
unterschiedlichen Höhen, Kristalle und Parallax-Berge. Die Figur steuert sich
selbst; Sprünge und Kollisionen laufen mit einem festen 60-Hz-Zeitschritt,
unabhängig von der Darstellung. Der Lauftext bleibt sichtbar.

Die Demo ist einzeln wählbar und Teil der Gesamtabfolge. Figuren und Hintergrund
sind vorberechnet; keine externen Spielgrafiken oder Game-Engine erforderlich.
Lokale reine Szenenmessung ca. 0,46 ms/Bild (ohne CRT-Pass).

## Grafiküberarbeitung des Jump-and-Run

Die einfachen geometrischen Platzhalter wurden durch eigens erzeugte
Pixelgrafik-Assets ersetzt: Waldpanorama mit Tiefe, schattierte Fels-/Moosflächen,
eine größere Figur mit sechs Lauf- und zwei Sprungposen sowie facettierte Kristalle.
Grafiken und vollständige Imagegen-Prompts liegen unter
C64Screensaver/Assets/Runner/. Die Spielphysik bleibt unverändert.
