# Testataufgabe - Labyrinth mit Zumo Robot

## Metadaten
- Robotikplattform: Zumo Robot
- Laufzeitumgebung: .NET / C#
- Hardwarekontext: Raspberry Pi / CM4 mit Zumo-Hardwareabstraktion

## Ziel
Der Zumo Robot soll mit Hilfe des LiDAR-Sensors selbstständig den Weg aus einem Labyrinth finden.

## Startbedingungen
- Startposition: Mitte des Labyrinths
- Startausrichtung: Fahrtrichtung nach unten
- Initiale Platzierung: Wird vor dem Start gesetzt

## Labyrinth
### Explizit bekannte Eigenschaften
- Wandelement Länge: ca. 200mm
- Wandabstand Toleranz: bis 30mm
- Anzahl gueltiger Wege: mehrere

### Strukturregeln
- Das Labyrinth besteht aus Wänden und Öffnungen.
- Nicht jede Öffnung ist befahrbar.
- Es dürfen nur markierte Öffnungen durchfahren werden.

### Nicht explizit spezifiziert
- Exakte Gesamtgeometrie: unbekannt
- Exakte Anzahl Kreuzungen: unbekannt
- Exakte Abmessungen des Gesamtlabyrinths: unbekannt
- Koordinatenmodell: nicht in Quelle enthalten
- Vollstaendige Karte als Text: nicht in Quelle enthalten

## Markierungen und Durchfahrtsregeln
### Zulässige Öffnungen
- Öffnungen sind nur dann zulässig, wenn sie am Boden markiert sind.
- Erlaubte Markierungen:
  - rot: innerer Ring
  - grün: äußerer Ring

### Verbot
- Unmarkierte Öffnungen dürfen nicht durchfahren werden.
- Wände dürfen nicht berührt werden.

## Verhaltensanforderungen
- Der Roboter muss den Ausgang selbstständig finden.
- Der Roboter muss ohne Wandkontakt navigieren.
- Der Roboter darf aus mehreren gültigen Wegen einen beliebigen gültigen Weg wählen.

## Sensorik und Aktorik
### Erforderlich bzw. direkt relevant
- LiDAR zur Erkennung von Wänden, freien Bereichen und möglichen Durchgängen
- Farbsensor zur Erkennung der Bodenmarkierungen
- Antrieb zum geraden Fahren, Drehen und kontrollierten Manövrieren

### Ausgabe / Feedback
- Beim Überfahren einer farbigen Markierung muss ein Ton abgespielt werden.
- Beim Verlassen des Labyrinths muss eine Melodie abgespielt werden.
- Die vorderen RGB-LEDs sollen während der Fahrt verwendet werden.
- Weitere LEDs können optional verwendet werden.

## Erfüllungskriterien
### Muss-Kriterien
- selbstständige Labyrinthlösung
- Start in der Mitte mit Fahrtrichtung nach unten
- nur markierte Öffnungen befahren
- keine Wand berühren
- Ton bei farbiger Markierung
- Melodie beim Verlassen des Labyrinths
- sinnvolle Verwendung der vorderen RGB-LEDs

### Randbedingungen
- Wandelemente sind etwa 200 mm lang.
- Zwischen Wänden können Lücken bis 30 mm vorkommen.
- Es existieren mehrere gültige Wege.

## Bewertungstechnisch relevante Interpretation
- Die Navigation muss robust gegenüber kleinen baulichen Lücken sein.
- Die Öffnungserkennung allein anhand von Abstandsdaten genügt nicht; die Freigabe einer Passage hängt zusätzlich von der Bodenmarkierung ab.
- Die Lösung braucht daher mindestens eine kombinierte Auswertung von LiDAR und Farbsensor.

## Technischer Kontext aus den Begleitdateien
### Bereits vorhandene Systemfunktionen
- UART-Kommunikation mit Request/Response-Muster
- Fahrbefehle für Strecke, Drehung und Radgeschwindigkeit
- Encoder/Odometrie
- Farbsensor mit Hue-Auswertung
- RGB-LED-Steuerung
- Soundausgabe
- LiDAR mit 360°-Scan und Analysehilfen

### Architekturhinweis
- zentraler Einstiegspunkt: ZumoRuntime.cs
- empfohlene Trennung:
  - Navigation
  - Sensorfusion
  - Bewegungsplanung
  - Aktorsteuerung
  - Status-/Feedbacklogik

## Offene Punkte
- Die exakte grafische Form des Labyrinths ist in den vorliegenden Textquellen nicht maschinenlesbar enthalten.
- Falls die PDF eine Skizze oder Grafik enthält, müsste diese separat manuell oder per Bildanalyse in ein Koordinaten- oder Segmentmodell überführt werden.

## Akzeptanztest
Eine Lösung gilt als erfolgreich, wenn der Roboter:
1. korrekt in der Mitte startet,
2. selbstständig navigiert,
3. nur zulässige markierte Öffnungen nutzt,
4. keine Wand berührt,
5. bei Markierungen einen Ton abspielt,
6. beim Verlassen des Labyrinths eine Melodie abspielt.