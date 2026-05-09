# Aufbau des Labyrinths

Legende:
- 🤖 = Roboter (Startausrichtung nach unten)
- ⬛ = Feste Wand (inneres Zentrum)
- ➖ / ┃ = Feste Wände (Ringe)
- 🟥 = Roter Durchgang (Innerer Ring = ERLAUBT)
- 🟩 = Grüner Ausgang (Äußerer Ring = ERLAUBT)
- ⬜ = Graue Markierung (Sackgasse / VERBOTEN)

```text
🟩-------------------------------------⬜
┃                                      ┃
┃      ┏-----------⬜----------┓       ┃
┃      ┃                       ┃       ┃
┃      ┃       ┏-------┓       ┃       ┃
┃      ┃       ┃       ┃       ┃       ┃
⬜     ⬜      ┃  🤖   ┃      🟥      ⬜
┃      ┃       ┃   ⬇   ┃       ┃       ┃
┃      ┃       ┗       ┛       ┃       ┃
┃      ┃                       ┃       ┃
┃      ┗-----------⬜----------┛       ┃
┃                                      ┃
⬜-------------------------------------⬜
```

### Textuelle Beschreibung des Aufbaus

Das Raster ist in **drei Zonen** unterteilt, die der Roboter von innen nach außen durchqueren muss:

**1. Das Start-Zentrum (Ebene 0)**
Der Roboter startet immer genau in der Mitte. Dieser Bereich ist U-förmig von Wänden (links, rechts, oben) umschlossen. Die Fahrtrichtung zeigt nach unten in die einzig offene Richtung. Der Roboter muss initial geradeaus fahren, um in den ersten Ring zu gelangen.

**2. Der innere Korridor (Ring 1)**
Sobald der Roboter das Start-Zentrum verlässt, befindet er sich in einem quadratischen inneren Rundlauf. 
*   **Die Abzweigungen:** Aus diesem Ring gibt es vier mögliche Ausgänge in den nächsten Ring, die sich mittig an den vier Seiten (oben, unten, links, rechts) befinden.
*   **Die Regel:** Genau eine dieser vier Öffnungen ist am Boden mit einem **roten Quadrat** markiert (in Beispiel 1: rechts, in Beispiel 2: oben). Die anderen drei sind grau (nicht erlaubt). Der Roboter muss den Ring abfahren, die Bodenfarben scannen und beim roten Quadrat nach außen abbiegen.

**3. Der äußere Korridor (Ring 2)**
Hat der Roboter die rote Markierung erfolgreich überfahren (und einen Ton abgespielt), landet er im äußeren Rundlauf.
*   **Die Ausgänge:** Die finalen Ausgänge aus dem gesamten Labyrinth befinden sich an den **vier Ecken** dieses äußeren Rings (oben-links, oben-rechts, unten-links, unten-rechts).
*   **Die Regel:** Genau eine dieser vier Ecken ist mit einem **grünen Quadrat** markiert (in Beispiel 1: oben-links, in Beispiel 2: oben-rechts). Der Roboter navigiert durch den äußeren Korridor bis zur grünen Markierung, verlässt dort das Labyrinth und spielt die Abschlussmelodie ab.

### Fazit für die Implementierung

Da der **geometrische Aufbau immer identisch** ist (Fixes Rastergitter), muss das System das Labyrinth nicht blind erkunden (wie z. B. bei einem klassischen "Wall-Follower" in unbekanntem Terrain). 

Das interne Kartenmodell kann stattdessen vorprogrammiert werden. Die Aufgabe der Software reduziert sich somit elegant auf folgende Punkte:
1. Den Roboter sicher und zentriert durch die bekannten, vorgegebenen Gänge steuern (mithilfe des LiDARs für den Wandabstand).
2. An den vorgegebenen Knotenpunkten (Mitten im inneren Ring, Ecken im äußeren Ring) anhalten oder verlangsamen.
3. Mit dem `IColorSensorService` die Farbe scannen und entscheiden, ob abgebogen wird (Rot/Grün) oder der Weg weiter dem aktuellen Ring folgen muss (Grau). 
