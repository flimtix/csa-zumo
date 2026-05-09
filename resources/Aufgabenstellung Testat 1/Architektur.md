# Architektur

## Architektur und Projekttrennung
Das Projekt `Zumo.Library` bleibt eine reine Hardware-Abstraktion und stellt Dienste wie `IDriveService` oder `ILidar` generisch zur Verfügung. Die gesamte Wegfindung und der Kartenaufbau werden in eine separate Anwendung (z.B. `Zumo.Labyrinth`) implementiert. Domain-spezifische Analyse-Strategien (z.B. Labyrinth-Navigation) liegen in der Applikationsschicht und nicht in der Basisbibliothek.

## Kartenmodell und Odometrie

Aufgrund der definierten Wandelemente von etwa 200 mm Länge lässt sich die Umgebung ideal in diskrete logische Zellen unterteilen. Die Nachverfolgung der eigenen Position und Ausrichtung in ein relatives Koordinatensystem übersetzt werden. Mit diesen Werten kann jede vom Sensor erkannte Wand oder Öffnung exakt mathematisch im internen Kartenmodell verortet werden.

## Sensorfusion für den Kartenaufbau

Die LiDAR-Snapshots liefern Entfernungsdaten, die durch den Analyzer in Wände und physische Lücken übersetzt und als Hindernisse in die Karte eingetragen werden. Da bauliche Spalten von bis zu 30 mm toleriert werden müssen, reicht der Laser allein nicht aus. Der `IColorSensorService` validiert die Lücken beim Überfahren anhand der roten oder grünen Bodenmarkierung und markiert sie im Modell als erlaubte Durchgänge.

## Iterativer Navigationszyklus

Der Roboter erweitert seine interne Karte schrittweise durch einen wiederkehrenden Ablauf. Dieser Zyklus garantiert, dass Fahren und Kartieren synchron bleiben.

- Scannen der Umgebung im Stillstand mittels LiDAR und Analyse der Gaps.
- Aktualisierung des internen Modells mit neuen Hindernissen und potenziellen Wegen.
- Berechnung der optimalen Route zu unerkundeten Bereichen und Ausrichtung des Roboters.
- Kontrollierte Fahrt zur nächsten Zelle bei gleichzeitiger Überprüfung der Farbmarkierungen.