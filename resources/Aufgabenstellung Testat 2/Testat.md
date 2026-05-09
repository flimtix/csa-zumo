# Spezifikation: Fernsteuerung Zumo Roboter (Testataufgabe 2)

## 1. Systemarchitektur & Kommunikation
Das System muss aus zwei voneinander unabhängigen, aber kooperierenden Server-Komponenten bestehen, die über WLAN erreichbar sind:
- **Zumo Server (TCP/Iterativ):** Nimmt Steuerbefehle von einem externen Client (z. B. Telnet, Netcat) entgegen und führt diese sequenziell aus. Gleichzeitige Fahrten müssen durch das iterative Server-Design verhindert werden.
- **HTTP-File Server:** Stellt nach Abschluss einer Fahrt die generierten Protokolldateien zum Download bereit.

## 2. Fahrtsteuerung & Lidar-Integration
Der Roboter muss vordefinierte Routen autonom abfahren können. Dabei gelten folgende Sicherheits- und Ablaufregeln:
- **Routenauswahl:** Der Zumo Server muss dem Client mindestens drei verschiedene vordefinierte Strecken (z.B. aus `ZumoDrives`) zur Auswahl anbieten und die ausgewählte Strecke starten.
- **Kollisionsvermeidung:** Während der Fahrt muss das Lidar aktiv sein. Sobald ein Hindernis in Fahrtrichtung in einer vordefinierten, ausreichend grossen Distanz erkannt wird, muss der Roboter sofort anhalten.
- **Energie-Management:** Das Lidar muss am Ende der Fahrt (egal ob Ziel erreicht oder durch Hindernis gestoppt) zwingend wieder ausgeschaltet werden.

## 3. Protokollierung (Logging)
Jede durchgeführte Fahrt muss zwingend als ASCII-Textdatei auf dem Dateisystem des Roboters gespeichert werden, damit sie später vom HTTP-Server ausgeliefert werden kann.

### Formatierungsregeln für die Datei:
- Keine Leerzeilen, keine Fusszeilen.
- **Zeile 1:** Name des Studierenden (z.B. `// Vorname Nachname`).
- **Zeile 2:** Aktuelles Datum und Uhrzeit (z.B. `// 04/05/2026 11:02:39`).
- **Zeile 3:** Bestätigung des initialen Verbindungschecks (z.B. `5<D1Ping`).
- **Folgende Zeilen:** Die exakten UART-Antwortstrings jedes erfolgreich ausgeführten Fahrbefehls.

### Abbruch-Szenarien:
- **Ordentliches Ende:** Das Log endet mit dem Antwortstring des letzten Fahrbefehls.
- **Abbruch durch Hindernis:** Wenn der Roboter durch das Lidar gestoppt wird, muss das Protokoll dies eindeutig aufzeigen (z.B. mit einem abschliessenden String wie `5<24100000000...Stop`).

## 4. Ablauf-Zyklus
Das System muss für den kontinuierlichen Betrieb ausgelegt sein:
1. Client verbindet sich mit dem Zumo Server und wählt eine Route.
2. Roboter fährt, Lidar überwacht, System schreibt das Protokoll mit.
3. Fahrt endet (Ziel erreicht oder Hindernis), Lidar schaltet ab, Log-Datei wird gespeichert.
4. Client kann das Log über den HTTP-Server herunterladen.
5. Der Zumo Server ist bereit, den nächsten Strecken-Befehl entgegenzunehmen (Schleife beginnt von vorn).