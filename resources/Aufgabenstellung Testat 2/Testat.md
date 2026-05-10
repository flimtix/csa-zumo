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

### Art der Steuerung
Es wird **keine** Live-Steuerung (WASD) und **kein** Upload von komplexen Befehlsdateien (z.B. "Fahre Links, Fahre Rechts") gefordert. 
Die Fahrstrecken sind bereits fest im C#-Code einprogrammiert (z.B. in der Klasse `ZumoDrives`). Der Client dient lediglich als drahtloser "Startknopf", um eine dieser vordefinierten Strecken fernzuzünden.

### Warum rohes TCP? (Didaktischer Hintergrund)
Dass für die Übermittlung eines einzelnen Buchstabens ein TCP-Socket anstelle einer modernen REST-API verwendet wird, hat einen reinen Lernzweck: Es soll das Verständnis für grundlegende Socket-Programmierung bewiesen werden. Die Nutzdaten (Payload) werden absichtlich minimal gehalten (`"A"`, `"B"`, `"C"`), damit der Fokus vollständig auf der korrekten Handhabung der Netzwerk-Architektur liegt (`TcpListener`, Streams lesen/schreiben, Ressourcen sauber schliessen).

### Kommunikationsablauf (Iterativer TCP-Server)
1. **Verbindungsaufbau:** Ein Client (z.B. Telnet oder Netcat) verbindet sich über einen definierten Port mit dem Roboter.
2. **Prompt:** Der TCP-Server auf dem Roboter sendet sofort eine Textaufforderung an den Client (z.B. `"Wähle eine Strecke: A, B oder C und drücke Enter..."`).
3. **Eingabe:** Der Benutzer sendet genau einen Buchstaben (z.B. `"A"`).
4. **Verarbeitung:** Der Server liest den Stream, schliesst die TCP-Verbindung (oder beendet den Dialog), ordnet den Buchstaben der entsprechenden Route aus `ZumoDrives` zu und startet die physische Fahrt.

## 3. Protokollierung (Logging)
Jede durchgeführte Fahrt muss zwingend als ASCII-Textdatei auf dem Dateisystem des Roboters gespeichert werden, damit sie später vom HTTP-Server ausgeliefert werden kann.

### Formatierungsregeln für die Datei:
- Keine Leerzeilen, keine Fusszeilen.
- **Zeile 1:** Name des Studierenden (z.B. `// Vorname Nachname`).
- **Zeile 2:** Aktuelles Datum und Uhrzeit (z.B. `// 04/05/2026 11:02:39`).
- **Zeile 3:** Bestätigung des initialen Verbindungschecks (z.B. `5<D1Ping`).
- **Folgende Zeilen:** Die exakten UART-Antwortstrings jedes erfolgreich ausgeführten Fahrbefehls.

Beispiel Protokolldatei:
```txt
// Roger Diehl 
// 04/05/2026 11:02:39 
5<D1Ping 
5<24C01F40064006400 
5<24A008700640064 
5<24C02BC0064006400 
5<24AFF7900640064 
5<24C01F40064006400 
5<24AFF7900640064 
5<24C02BC0064006400
```

Protokoll mit Abbruch:
```txt
// Roger Diehl 
// 04/05/2026 11:05:12 
5<D1Ping 
5<24C01F40064006400 
5<24A008700640064 
5<24C02BC0064006400 
5<24A008700640064 
5<24C01F40064006400 
5<24A008700640064 
5<24C02BC0064006400 
5<24100000000...Stop
```

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