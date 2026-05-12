Der Plan ist in logische Phasen unterteilt und berücksichtigt die Ungenauigkeiten der Hardware durch ein fehlertolerantes, reaktives Verhalten (Continuous Checking) in Kombination mit dem Always-Right-Algorithmus.

### 1. Initialisierung und Start-Bedingung
*   **Wartestatus (Idle):** Das System startet in einem Ruhemodus. Die hintere LED leuchtet in einer neutralen Farbe (z. B. Weiss), um die Bereitschaft zu signalisieren.
*   **Aktivierung:** Das System blockiert jegliche Navigation, bis ein Klick auf den Start-Button am CM4 registriert wird.
*   **Start-Setup:** Nach dem Klick wechselt die hintere LED die Farbe (z. B. auf Blau für "Initialisierung"). Das dynamische Grid wird im Speicher mit dem Startpunkt (0,0) und der initialen Blickrichtung (Süden/Unten) angelegt.

### 2. Status-Visualisierung via LED
Um das Debugging während der Fahrt zu erleichtern, muss ein durchgängiges Farbkonzept für die hintere LED definiert werden. Copilot soll eine Logik implementieren, die den Zustand jederzeit visuell abbildet:
*   **Blau:** Scannen und Auswerten der Umgebung (Kreuzung / Stillstand).
*   **Gelb:** Vorwärtsfahrt in kleinen Schritten.
*   **Cyan:** Ausrichten / Korrigieren der Rotation anhand der Wände.
*   **Violett:** Drehung um 90 oder 180 Grad.
*   **Rot:** Sackgasse / Verbotene Markierung erkannt (Rückzug).
*   **Grün:** Ziel erreicht / Labyrinth gelöst.

### 3. Fehlertolerante Fortbewegung (Small Steps & Auto-Centering)
Da feste Distanzen und Rotationen ungenau sind, darf der Roboter niemals "blind" eine lange Strecke fahren.
*   **Schrittweise Vorwärtsfahrt:** Die Vorwärtsbewegung wird in sehr kleine Intervalle unterteilt. Nach jedem kleinen Schritt stoppt oder pausiert die Logik kurz die Wegstreckenmessung, um Sensordaten frisch auszuwerten.
*   **Dynamische Wandzentrierung:** Während der kleinen Schritte liest das System den LiDAR aus. Da der LiDAR rauscht, werden die Abstände zur linken und rechten Wand geglättet (z. B. durch Durchschnittswerte mehrerer Scans). Ist der Roboter zu nah an einer Wand, wird die Fahrbewegung minimal in die entgegengesetzte Richtung korrigiert (virtueller Trichter), damit er immer mittig im Gang bleibt.
*   **Winkelausgleich:** Steht der Roboter nach einer 90-Grad-Drehung schief im Gang, wird dies über die Differenz der vorderen und hinteren LiDAR-Messpunkte an den Seitenwänden erkannt und durch eine kleine Rotationskorrektur auf der Stelle behoben, bevor die Vorwärtsfahrt beginnt.

### 4. Dynamischer Kartenaufbau und Kreuzungserkennung
Das Raster wird nicht durch fixe Koordinaten, sondern durch das Erkennen von topologischen Knotenpunkten (Zellen) aufgebaut.
*   **Erkennen einer Kreuzung:** Der Roboter weiss, dass er eine neue Zelle/Kreuzung erreicht hat, wenn entweder die Wand frontal einen kritischen Mindestabstand unterschreitet, oder wenn der LiDAR plötzlich links oder rechts einen drastischen Anstieg der Distanz misst (eine Lücke taucht auf).
*   **Grid-Update:** An einer solchen Kreuzung hält der Roboter an. Die aktuelle Position wird im dynamischen Raster als besuchte Zelle markiert.
*   **Scan-Phase:** Aus dem Stand wird per LiDAR geprüft, in welche Richtungen (Links, Rechts, Geradeaus) physische Lücken existieren. Diese Lücken werden als "potenzielle Wege" im Grid für diese Zelle vermerkt.

### 5. Always-Right Algorithmus & Farb-Validierung
An jeder Kreuzung entscheidet das System nach der Rechte-Hand-Regel, welcher Weg als Nächstes ausprobiert wird. Da physische Lücken im LiDAR nicht zwingend erlaubte Wege sind (sie könnten graue Markierungen haben), erfolgt die Wegfindung in zwei Stufen:
*   **Priorisierung:** Die Logik prüft die potenziellen Wege in der festen Reihenfolge: 1. Rechts, 2. Geradeaus, 3. Links, 4. Umdrehen (Rückweg).
*   **Ausrichten und Herantasten:** Der Roboter dreht sich in die priorisierte Richtung (z. B. Rechts). Er fährt nun in sehr kleinen Schritten vorwärts, primär um den Farbsensor über die Bodenmarkierung der Lücke zu schieben.
*   **Farb-Prüfung:**
    *   *Fall A (Farbe ist Rot):* Der Weg ist gültig. Der Ton wird abgespielt. Der Roboter registriert diesen Weg im Grid als "offen", fährt vollständig in die neue Zelle ein und der Zyklus beginnt von vorn.
    *   *Fall B (Farbe ist Grau / Keine Farbe):* Der Weg ist eine Sackgasse. Der Roboter stoppt sofort, fährt im Rückwärtsgang exakt die wenigen Zentimeter zurück in die Mitte der vorherigen Kreuzung. Im Grid wird diese Richtung nachträglich als "Wand" markiert. Der Algorithmus wählt nun die nächste Priorität (z. B. Geradeaus).
    *   *Fall C (Farbe ist Grün):* Der Zielausgang ist gefunden.

### 6. Zielerreichung
Sobald der Farbsensor die grüne Markierung erkennt:
*   Die Antriebsmotoren werden sofort gestoppt.
*   Die Melodie für das Verlassen des Labyrinths wird abgespielt.
*   Die hintere LED leuchtet dauerhaft Grün.
*   Die Navigationsschleife wird beendet und das System wartet auf einen Neustart (oder schaltet sich ab).