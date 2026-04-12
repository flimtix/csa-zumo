# Zumo Labyrinth Navigator

Ein C# / .NET 10 basiertes Robotik-Projekt für den Raspberry Pi (CM4), mit dem ein Zumo Robot autonom ein vorgegebenes Labyrinth mithilfe von LiDAR- und Farbsensor-Daten lösen kann.

## 🤖 Über das Projekt

Ziel dieses Projekts ist es, einen kleinen Roboter so zu steuern, dass er selbstständig aus einem Labyrinth navigiert. Dabei darf er niemals in eine Wand fahren und muss sich dynamisch orientieren. Da die Hardware-Rotations- und Distanzmessungen ungenau sind, verlässt sich die Software nicht auf feste Koordinaten. Stattdessen nutzt sie einen Continuous-Checking-Ansatz ("Virtual Funneling" und State Machine) in Kombination mit Sensorfusion (LiDAR & Farbsensor), um die Umgebung dynamisch abzutasten.

### Erfüllungskriterien
- Start in der Mitte (Blickrichtung nach unten).
- Kein Wandkontakt (Fehlertolerante Navigation).
- Ausschließlich markierte Wege befahren (Rot = Erlaubter Ringwechsel, Grün = Ausgang, Grau = Sackgasse).
- Visuelles Feedback über die hintere RGB-LED während der Navigation.
- Akustisches Feedback beim Erkennen von Markierungen und beim Lösen des Labyrinths.

---

## 🏗 Architektur

Das Projekt folgt strengen Clean Code und SOLID-Prinzipien. Es ist in zwei wesentliche Hauptbestandteile unterteilt:

1. **`Zumo.Library` (Hardware-Abstraktion)**:
   Eine domänenagnostische Bibliothek. Sie kommuniziert über UART mit dem Roboter und kapselt die Hardware (LiDAR, Farbsensor, Motoren, LEDs) in leicht verständliche C#-Interfaces (z.B. `IDriveService`, `ILidarAnalyzer`).
2. **`Zumo.Labyrinth` (Applikationsschicht)**:
   Enthält die eigentliche Wegfindung, die State Machine und die Sensorfusion. Diese Schicht konsumiert die Library via Dependency Injection und steuert den 
   Roboter basierend auf den Navigationsregeln.

Die aktuelle Dokumentation kann unter [`/docs`](/docs/) angeschaut werden.

### Das 5-Schichten-Modell (Com & Runtime)
Die Architektur der Library ist nach einem 5-Schichten-Modell aufgebaut:
1. **Consumer:** Nutzt die Library API (Labyrinth App, Calibration App).
2. **Runtime:** Baut alle Abhängigkeiten auf (`ZumoRuntime`).
3. **Services:** Kapselt fachliche Befehle (`ZumoDriveService`, `ZumoColorSensorService` etc.).
4. **Communication:** Löst technische Kommunikation (`RequestResponseBroker`, `ZumoProtocolCodec`).
5. **Hardware:** Physische Ansteuerung (UART, GPIO, LiDAR).

---

## 🗺 Das Labyrinth & Wegfindung

Das Labyrinth besteht aus drei ineinanderliegenden Ringen (Ebenen). Die Wandelemente sind ca. 200mm lang (Toleranzen bis zu 30mm Spaltmaß).

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

### Der "Always-Right"-Algorithmus
Das System tastet sich in extrem kleinen Schritten (z.B. 5cm) vorwärts und zentriert sich kontinuierlich an den Wänden (Auto-Centering). 
1. **Kreuzungserkennung:** Der LiDAR erkennt physische Lücken im Gang.
2. **Farbvalidierung:** Der Roboter dreht sich in die priorisierte Lücke (Rechte-Hand-Regel), fährt ein kleines Stück hinein und scannt den Boden.
3. **Entscheidung:**
   - **Rot:** Korrekter Übergang in den äußeren Ring. Der Weg wird in die Karte übernommen.
   - **Grau / Keine Farbe:** Sackgasse. Der Roboter setzt zurück und probiert den nächsten Weg.
   - **Grün:** Ausgang gefunden! Die Melodie wird abgespielt.

### Demo 🚗

<video height="400" controls>
  <source src="timelaps.mp4" type="video/mp4">
</video>

---

## 🔌 Hardware & UART-Protokoll

Die `Zumo.Library` kommuniziert mit der Zumo-Firmware über ein spezifisches UART-Protokoll. 
Befehle folgen dem Muster `5>...` (Set) oder `5?...` (Get). Antworten kommen als `5<...` oder `5*...`.

**Beispiele unterstützter Domänen:**
- **Antrieb (0x24):** Streckenfahrt (`5>242...`), Drehen auf der Stelle, Konstante Radgeschwindigkeiten.
- **Odometrie (0x22):** Quadraturdistanz, Kalibrierungsfaktoren setzen (`5>221NNNN`).
- **Farbsensor (0x31):** Farbton in Grad (`5?310`), Schwarz/Weiß Kalibrierung (`5>31600`).
- **RGB-LEDs (0x11/0x12):** Bitmasken und Hex-Farbwerte setzen.
- **Sound (0x50):** Frequenz-Piepsen oder vordefinierte Melodien abspielen (`0` = Knight Rider, `2` = Super Mario).

### UART-Broker-Logik
Der integrierte `RequestResponseBroker` verarbeitet asynchrone UART-Anfragen. Er formatiert Nachrichten über den `ZumoProtocolCodec`, sendet sie über den `ZumoConnectionTransport` und ordnet eintreffende Responses (bzw. asynchrone Hardware-Events via `EventRouter`) dem aufrufenden `Task` wieder zu.

---

## 🛠 Setup & Deployment

Das System ist primär für die Ausführung auf einem **Raspberry Pi Compute Module 4 (CM4)** konzipiert.

### Lokales Bauen und Testen
Um das System zu bauen und C#-Unit-Tests auszuführen:
```bash
dotnet restore
dotnet build -p:RunPostBuildEvent=Never
dotnet test
```
*(Tests können dank Dependency Injection lokal ohne physische Hardware via Mocking ausgeführt werden).*

### Automatisches Deployment (Post-Build)
Für den reibungslosen Workflow ist in der `.csproj` ein Post-Build-Event konfiguriert, welches die Artefakte per Bash-Skript auf den Raspberry Pi kopiert:
```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent" Condition="'$(GITHUB_ACTIONS)' != 'true'">
  <Exec Command="bash $(SolutionDir)deploy.sh '$(TargetDir)' '$(ProjectName)'" />
</Target>
```
> Einstellungen für das Deployment können im `deploy.sh` vorgenommen werden.

*Hinweis: Dieser Schritt wird während CI/CD-Pipelines (z.B. GitHub Actions) automatisch übersprungen.*