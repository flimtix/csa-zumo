# Architektur- und Anforderungsanalyse: Zumo Robot Labyrinth-Solver

## 1. Um was geht es in diesem Projekt?
Das Projekt ist Teil des Moduls "C# in Action" (CSA) und befasst sich mit der hardwarenahen Softwareentwicklung unter .NET (C#) auf einem Embedded-Linux-System. Im Zentrum steht die Programmierung eines physischen Roboters ("Zumo Robot"), der von einem Raspberry Pi Compute Module 4 (CM4) gesteuert wird. Das Projekt verbindet objektorientierte Softwarearchitektur mit klassischer Mikrocontroller-Kommunikation (GPIO, PWM, UART) und erfordert die Auswertung von Echtzeit-Sensordaten (LiDAR, Farbsensor, Odometrie).

## 2. Was ist das Ziel des Projektes?
**Didaktische Ziele:**
- Erstellung, Deployment und Remote-Debugging einer .NET-Konsolenanwendung auf einem Raspberry Pi.
- Praktischer Einsatz und Ansteuerung von Hardware-Komponenten (Sensoren und Aktoren).
- Analyse, Verständnis und Erweiterung einer hardwarenahen C#-Klassenbibliothek (`ZumoLib`).

**Praktisches Systemziel:**
Der Zumo Robot muss selbstständig, kollisionsfrei und ohne menschliche Eingriffe den Weg aus einem Labyrinth finden. Die Navigation basiert auf Umgebungsdaten eines 360°-LiDAR-Sensors und Bodendaten eines Farbsensors.

## 3. Wie ist das Projekt aufgebaut?
Das Projekt folgt einer klaren Host-Target-Architektur mit einer Trennung zwischen Treiber-Schicht und Applikations-Schicht.

### 3.1. Systemarchitektur
- **Host-System:** Entwicklungsrechner (Windows/Mac/Linux) mit Visual Studio oder VS Code. Code wird lokal kompiliert.
- **Target-System:** Raspberry Pi (CM4) mit Linux. Die fertig kompilierte Anwendung wird via SSH/rsync auf ein RAM-Drive (`~/netcore`) des Pi deployed, um SD-Karten-Verschleiss zu minimieren.
- **Debugging:** Erfolgt remote über SSH (mittels `vsdbg` Server auf dem Pi).

### 3.2. Softwarearchitektur (C# Solution)
Die C#-Solution ist strikt in zwei Projekte unterteilt:
1. **`ZumoLib` (Klassenbibliothek):** Kapselt die gesamte Hardwarekommunikation. Hier werden Sensoren und Aktoren als C#-Klassen abstrahiert. Beinhaltet Zugriffe auf GPIO, PWM (für LiDAR-Motor) und vor allem serielle Kommunikation (UART) zum Zumo-Basisboard über ein proprietäres Dispatcher-Protokoll.
2. **`ZumoApp` (Konsolenanwendung):** Die eigentliche Steuerungslogik. Referenziert `ZumoLib` und implementiert den Lösungsalgorithmus für das Labyrinth.

### 3.3. Hardware & Schnittstellen (Zumo Peripherie)
- **CM4-Onboard:** Taster (GPIO 27, Active-Low) und Benutzer-LED (GPIO 18).
- **LiDAR (LD19):** Motorsteuerung via PWM an Pin 12 (bzw. 7 in älteren Versionen). Daten via UART. Liefert 360° Punktwolke (Distanzen in mm).
- **Zumo UART-Dispatcher (Baudrate 230400, Port: `/dev/ttyAMA4`):**
  - *Antrieb (0x24):* Steuerung von Strecken, Kurven, Drehungen und Geschwindigkeit.
  - *Encoder (0x22):* Abfrage von Radgeschwindigkeiten und zurückgelegten Distanzen.
  - *Farbsensor (0x31):* Gibt den Farbton (Hue) in Grad zurück.
  - *Sound (0x50):* Pieptöne (Frequenz/Dauer) und vordefinierte Melodien abspielen.
  - *RGB-LEDs (0x11 unten, 0x12 oben):* Setzen von Farben mittels Bitmaske.

## 4. Was für Anforderungen hat es?
- **Startposition:** Der Roboter wird manuell exakt in die Mitte des Labyrinths gesetzt. Die initiale Fahrtrichtung zeigt nach "unten".
- **Wegfindung:** Der Roboter muss den Ausgang völlig autonom finden. Es existieren mehrere gültige Auswege.
- **Torbeschränkung:** Es dürfen *nur* Öffnungen durchfahren werden, die am Boden durch eine bestimmte Farbe markiert sind:
  - **Rote Fläche** = Übergang im inneren Ring
  - **Grüne Fläche** = Übergang im äusseren Ring
- **Kollisionsfreiheit:** Während der gesamten Fahrt darf keine Wand berührt werden.
- **Optisches Feedback:** Die vorderen RGB-LEDs *müssen* nach eigenem Ermessen für Statusanzeigen (z.B. Fahren, Drehen, Farbe erkannt, Ziel erreicht) genutzt werden.
- **Akustisches Feedback:** 
  - Beim Überfahren einer farbigen Markierung (rot oder grün) muss ein Ton abgespielt werden.
  - Beim Verlassen des Labyrinths (Ziel erreicht) muss eine Melodie abgespielt werden.

## 5. Was ist die Aufgabenstellung?
Entwicklung der kompletten C#-Steuerungssoftware in `ZumoLib` und `ZumoApp`, um die oben genannten Anforderungen zu erfüllen.

### 5.1. Unteraufgaben für den Coding-Agenten
1. **Ausprogrammieren der ZumoLib:**
   - **UART-Kommunikation:** Das proprietäre Protokoll (z.B. `5>242SSSSVVVVAAAA` für Fahren) in typsichere C#-Methoden kapseln. Senden von Befehlen und synchrones Warten auf/Parsen von Payload-Antworten.
   - **Farbsensor implementieren:** Auslesen des Hue-Wertes (`5?310`). Gemäss `Farbmuster.pdf` liegt Rot bei ~0°/360° und Grün bei ~120°. Hier muss eine Toleranz-Logik implementiert werden (z.B. Rot = 340°-360° & 0°-20°, Grün = 100°-140°).
   - **Antrieb & Odometrie implementieren:** Methoden für Geradeausfahrt, Drehen an Ort und Kurvenfahrt bereitstellen, inkl. Polling für "Ist Fahrt abgeschlossen?" (`5?242`).
   - **Sound & LED:** Methoden zum Abspielen von Frequenzen/Melodien und zum Setzen der RGB-Werte implementieren.
2. **Labyrinth-Algorithmus (ZumoApp):**
   - **LiDAR-Auswertung:** Algorithmus schreiben, der die 360° Punktwolke in Sektoren unterteilt (Vorne, Links, Rechts), um Wände, Öffnungen und Distanzen zur Wand zu erkennen.
   - **Filterung von Stördaten:** Erkennen und Herausrechnen von kleinen Lücken in der Wand (siehe Randbedingungen).
   - **State-Machine (Zustandsautomat):** Programmierung der Kernlogik. Mögliche Zustände: `ScanneUmgebung`, `FahreZuOeffnung`, `PruefeBodenfarbe`, `KorrigiereKurs`, `ZielErreicht`.

## 6. Was sind die Randbedingungen?
Diese physikalischen Restriktionen sind essenziell für den Navigationsalgorithmus:
- **Wand-Dimensionen:** Ein einzelnes Wandelement ist **ca. 200 mm** lang.
- **Falsche Lücken:** Zwischen zusammenhängenden Wänden kann ein mechanischer Spalt von **bis zu 3 cm** bestehen. *Achtung an den Agenten:* Der Algorithmus zur Lückenfindung (Gap-Detection) darf eine 3 cm Lücke nicht als durchfahrbare Tür interpretieren! Eine gültige Tür ist signifikant breiter.
- **Sicherheitsabstand:** Der Roboter darf die Wände nicht berühren. Es muss ein PID-Regler oder ein reaktiver Ausweich-Algorithmus basierend auf den seitlichen LiDAR-Werten (z.B. Winkel 270° und 90°) implementiert werden, um den Roboter mittig durch Gänge zu navigieren.
- **Sensorik-Tücken:** Bodensensoren haben oft Rauschen. Der Farbsensor gibt bei zu dunklem/ungesättigtem Untergrund einen "Ungültig-Wert" zurück. Dies muss im Code sicher abgefangen werden.
- **Deadlines:** Die fertige Software muss robust genug sein, um an der Demo (13. April 2026) live zu funktionieren.
