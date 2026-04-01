# Zumo UART Befehle

## Allgemeines Format

- `5>...` = Set-Befehl. Antwort typischerweise als `5<...`.  
- `5?...` = Get-Befehl. Antwort typischerweise als `5*...`.  
- `5D1Ping` = Verbindungscheck. Antwort `5<D1Ping`.  
- Signed-16-Bit-Werte werden im Zweierkomplement übertragen, z. B. `005A = 90` und `FFA6 = -90`.  

## Verbindungscheck

| Zweck            | Senden    | Rückgabe   | Beispiel               |
| ---------------- | --------- | ---------- | ---------------------- |
| Ping / UART-Test | `5D1Ping` | `5<D1Ping` | beobachtet: `5<D1Ping` |

## Antrieb (0x24) und Quadraturgeber (0x22)

| Zweck                               | Senden                  | Rückgabe                                        | Bedeutung                                                                            |
| ----------------------------------- | ----------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------ |
| Strecke fahren                      | `5>242SSSSVVVVAAAA`     | `5<242SSSSVVVVAAAA`                             | `SSSS` Distanz in mm, `VVVV` Geschwindigkeit in mm/s, `AAAA` Beschleunigung in mm/s² |
| Verbleibende Distanz abfragen       | `5?242`                 | `5*24SSSS`                                      | Restdistanz in mm                                                                    |
| An Ort drehen                       | `5>24ASSSSVVVVAAAA`     | `5<24ASSSSVVVVAAAA`                             | `SSSS` Winkel in Grad, positiv = Uhrzeigersinn                                       |
| Konstante Radgeschwindigkeit setzen | `5>241LLLLRRRR`         | `5<241LLLLRRRR`                                 | `LLLL` links, `RRRR` rechts, jeweils mm/s, signiert                                  |
| Sollgeschwindigkeit abfragen        | `5?241`                 | `5*24LLLLRRRR`                                  | linke und rechte Sollgeschwindigkeit                                                 |
| Kurvenfahrt mit Radius              | `5>249SSSSRRRRVVVVAAAA` | `5<249SSSSRRRRVVVVAAAA`                         | `SSSS` Winkel, `RRRR` Radius, `VVVV` Geschwindigkeit, `AAAA` Beschleunigung          |
| Kalibrierfaktor Drehen setzen       | `5>24BNNNN`             | `5<24BNNNN`                                     | Kalibrierfaktor für Drehung                                                          |
| Quadraturgeschwindigkeit abfragen   | `5?220`                 | `5*220LLLLRRRR`                                 | aktuelle gemessene Radgeschwindigkeiten                                              |
| Quadraturdistanz abfragen           | `5?221`                 | `5*221LLLLRRRR`                                 | gemessene Radstrecken in mm                                                          |
| Quadraturdistanz zurücksetzen       | `5>220`                 | `5<220`                                         | setzt beide Distanzzähler auf 0                                                      |
| Quadratur-Distanzfaktor setzen      | `5>221NNNN`             | `5<221NNNN`                                     | Kalibrierfaktor für Encoderdistanz                                                   |
| Quadratur-Distanzfaktor abfragen    | `5?222`                 | dokumentiert, Payload nicht genauer beschrieben | liest den gesetzten Distanzfaktor                                                    |

### Beobachtete Antworten aus deinem Test

- `5?242` → `5*240048`
- `5?241` → `5*2402080127`
- `5?220` → `5*22001F70113`
- `5?221` → `5*221017800CF`

## RGB-LEDs (0x11 unten, 0x12 oben)

| Zweck                  | Senden         | Rückgabe                   | Bedeutung                           |
| ---------------------- | -------------- | -------------------------- | ----------------------------------- |
| Farbe setzen unten     | `5>11LLRRGGBB` | `5<11LLRRGGBB`             | `LL` Bitmaske, `RR/GG/BB` Farbwerte |
| Farbe setzen oben      | `5>12LLRRGGBB` | `5<12LLRRGGBB`             | gleiches Format wie unten           |
| LEDs unten ausschalten | `5>11OF`       | typischerweise Bestätigung | dokumentiert als Aus-Befehl         |
| LEDs oben ausschalten  | `5>12OF`       | typischerweise Bestätigung | dokumentiert als Aus-Befehl         |
| Farbe unten abfragen   | `5?11LL`       | `5*11LLRRGGBB`             | liefert Bitmaske plus RGB           |
| Farbe oben abfragen    | `5?12LL`       | `5*12LLRRGGBB`             | liefert Bitmaske plus RGB           |

### Beobachtete Antworten aus deinem Test

- `5>11FFFF0000` → `5<11FFFF0000`
- `5>12FF00FF00` → `5<12FF00FF00`
- `5?11FF` → `5*1101FF0000`
- `5>11FF000000` → `5<11FF000000`

## ColorSensor (0x31)

| Zweck                         | Senden    | Rückgabe                               | Bedeutung                                              |
| ----------------------------- | --------- | -------------------------------------- | ------------------------------------------------------ |
| Farbton abfragen              | `5?310`   | `5*31HHHH`                             | `HHHH` = Farbton in Grad                               |
| Schwarz-Kalibrierung          | `5>31600` | `5<31600`                              | schwarzen Untergrund als Referenz aufnehmen            |
| Weiss-Kalibrierung + Anwenden | `5>31601` | Bestätigung, in deinem Test abweichend | weissen Untergrund aufnehmen und Kalibrierung anwenden |

### Beobachtete Antworten aus deinem Test

- `5?310` → `5*31005E`
- `5>31600` → `5<31600`
- `5>31601` → `5<31$04$`

## Sound (0x50)

| Zweck           | Senden          | Rückgabe        | Bedeutung                                 |
| --------------- | --------------- | --------------- | ----------------------------------------- |
| Piepsen         | `5>500FFFFTTTT` | `5<500FFFFTTTT` | `FFFF` Frequenz in Hz, `TTTT` Dauer in ms |
| Musik abspielen | `5>501N`        | `5<501N`        | `N` = Song-Index im Low-Nibble            |

### Song-Indizes

| Index | Melodie           |
| ----- | ----------------- |
| `0`   | Knight Rider      |
| `1`   | Star Wars         |
| `2`   | Super Mario       |
| `3`   | Wasted            |
| `4`   | Harry Potter      |
| `5`   | Lord of the Rings |
| `6`   | Indiana Jones     |
| `7`   | James Bond 007    |
| `8`   | Pacman            |

### Beobachtete Antworten aus deinem Test

- `5>50001B80064` → `5<50001B80064`
- `5>5012` → `5<5012`

## Kalibrierung der Sensoren und Aktoren

Mechanische Toleranzen, Umgebungslicht oder Abnutzung können dazu führen, dass die theoretischen Werte der Sensoren und Motoren von der Realität abweichen. Um eine hohe Präzision zu gewährleisten, bietet das Zumo-System spezifische Kalibrierungsbefehle.

### Farbsensor: Schwarz-/Weiss-Kalibrierung
- **Ziel der Kalibrierung / Gelöstes Problem:** 
  Der Farbsensor liest Lichtreflexionen. Abhängig vom Umgebungslicht oder der Distanz zum Boden können die gemessenen Werte verfälscht werden (z. B. wird Weiss als Grau erkannt). Die Kalibrierung definiert die physikalischen Grenzen für "maximal dunkel" und "maximal hell", damit der errechnete Farbton (Hue) stets akkurat ist.
- **Wie wird kalibriert?**
  1. Den Roboter mit dem Sensor über einen **schwarzen** Untergrund platzieren.
  2. Die schwarze Referenz aufnehmen. Senden: `5>31600`.
  3. Den Roboter mit dem Sensor über einen **weissen** Untergrund platzieren.
  4. Die weisse Referenz aufnehmen. Senden: `5>31601`. 
  *(Mit dem zweiten Befehl wird die Kalibrierung abgeschlossen und sofort angewendet).*

### Antrieb: Streckenfahrt (Encoder-Distanzkalibrierung)
- **Ziel der Kalibrierung / Gelöstes Problem:** 
  Die Quadratur-Encoder zählen Radumdrehungen, um die zurückgelegte Distanz zu ermitteln. Wenn der tatsächliche Raddurchmesser minimal von der Software-Annahme abweicht, fährt der Roboter bei einem 1000-mm-Befehl vielleicht nur 950 mm. Die Kalibrierung korrigiert die Distanzauswertung.
- **Wie wird kalibriert?**
  1. Den Roboter den Befehl geben, eine exakte Strecke von 1000 mm geradeaus zu fahren.
  2. Die tatsächlich gefahrene Strecke manuell (z. B. mit einem Massband) auf dem Boden nachmessen.
  3. Den effektiv gemessenen Wert in Millimetern nehmen, in eine Hexadezimal-Zahl umwandeln und als neuen Faktor setzen (`5>221NNNN`).
  4. *Beispiel:* Wenn der Roboter physikalisch nur 950 mm gefahren ist, entspricht 950 der Hexadezimalzahl `03B6`. Der Befehl lautet dann: `5>22103B6`.

### Antrieb: Drehen an Ort (Winkelkalibrierung)
- **Ziel der Kalibrierung / Gelöstes Problem:** 
  Wenn der Roboter den Befehl erhält, sich um exakt 90° oder 180° zu drehen, kann er aufgrund von minimalen Unterschieden im Radabstand oder der Bodenhaftung über- oder unterdrehen. Diese Kalibrierung dient dem Feinabgleich des Drehwinkels.
- **Wie wird kalibriert?**
  1. Den aktuellen Skalierungsfaktor (Standard ist 130, also Faktor 1.3) auf `100` (Hexadezimal: `0064`) zurücksetzen. Senden: `5>24B0064`.
  2. Den Roboter einen bestimmten Winkel drehen lassen (z. B. 180° oder 360°).
  3. Den *tatsächlich* erreichten Winkel mit einem Winkelmesser messen.
  4. Den neuen Faktor berechnen: `Faktor = (Sollwinkel / Istwinkel) * 100`.
  5. Den neu berechneten Faktor in eine 16-Bit Hex-Zahl (`NNNN`) umwandeln und an den Roboter senden: `5>24BNNNN`.

## Beispielausgabe:

```txt
--- VERBINDUNGSCHECK ---
Verbindungs-Ping                    | TX: 5D1Ping              | [SENDEN] 5D1Ping
RX: 5<D1Ping

--- ANTRIEB (0x24 & 0x22) ---
Quadraturdistanz zurücksetzen       | TX: 5>220                | [SENDEN] 5>220
RX: 5<220
Fahre 100mm (v=1000, a=1000)        | TX: 5>242006403E803E8    | [SENDEN] 5>242006403E803E8
RX: 5<242006403E803E8
Verbleibende Distanz abfragen       | TX: 5?242                | [SENDEN] 5?242
RX: 5*240048
Drehe um 90°                        | TX: 5>24A005A03E803E8    | [SENDEN] 5>24A005A03E803E8
RX: 5<24A005A03E803E8
Konstante Geschw. (L/R 500)         | TX: 5>24101F401F4        | [SENDEN] 5>24101F401F4
RX: 5<24101F401F4
Kurvenfahrt (90°, R=200)            | TX: 5>249005A00C803E803E8 | [SENDEN] 5>249005A00C803E803E8
RX: 5<249005A00C803E803E8
Kalibrierfaktor setzen (1.0)        | TX: 5>24B0064            | [SENDEN] 5>24B0064
RX: 5<24B0064
Aktuelle Sollgeschwindigkeit        | TX: 5?241                | [SENDEN] 5?241
RX: 5*2402080127
Quadraturgeschwindigkeit            | TX: 5?220                | [SENDEN] 5?220
RX: 5*22001F70113
Quadraturdistanz                    | TX: 5?221                | [SENDEN] 5?221
RX: 5*221017800CF
Quadratur-Distanzfaktor setzen      | TX: 5>22103B6            | [SENDEN] 5>22103B6
RX: 5<22103B6
Antrieb stoppen (Sicherheit!)       | TX: 5>24100000000        | [SENDEN] 5>24100000000
RX: 5<24100000000

--- RGB-LEDs (0x11 & 0x12) ---
Alle unteren LEDs Rot               | TX: 5>11FFFF0000         | [SENDEN] 5>11FFFF0000
RX: 5<11FFFF0000
Alle oberen LEDs Grün               | TX: 5>12FF00FF00         | [SENDEN] 5>12FF00FF00
RX: 5<12FF00FF00
Farbe untere LEDs abfragen          | TX: 5?11FF               | [SENDEN] 5?11FF
RX: 5*1101FF0000
Alle unteren LEDs ausschalten       | TX: 5>11FF000000         | [SENDEN] 5>11FF000000
RX: 5<11FF000000

--- COLOR SENSOR (0x31) ---
Farbton abfragen                    | TX: 5?310                | [SENDEN] 5?310
RX: 5*31005E
Schwarz-Kalibrierung Start          | TX: 5>31600              | [SENDEN] 5>31600
RX: 5<31600
Weiss-Kalibrierung Start            | TX: 5>31601              | [SENDEN] 5>31601
RX: 5<31$04$

--- SOUND (0x50) ---
Piepsen (440Hz, 100ms)              | TX: 5>50001B80064        | [SENDEN] 5>50001B80064
RX: 5<50001B80064
Musik (Super Mario)                 | TX: 5>5012               | [SENDEN] 5>5012
RX: 5<5012
```