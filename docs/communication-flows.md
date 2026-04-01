# Communication Flows

Diese Abläufe zeigen die wichtigsten realen Kommunikationspfade aus der aktuellen Implementierung.

## 1) Command/Query aus Service zur Hardware (Request/Response)

Beispiel: Drive-Service ruft SendRequest auf und wartet auf Antwort.

```mermaid
sequenceDiagram
    participant App as Consumer
    participant Svc as ZumoDriveService
    participant Br as RequestResponseBroker
    participant Cod as ZumoProtocolCodec
    participant Tr as ZumoConnectionTransport
    participant Hw as Zumo UART Firmware

    App->>Svc: DriveDistance(...) oder GetRemainingDistance()
    Svc->>Br: SendRequest(address, sdf, type, payload, timeout)
    Br->>Br: PendingRequest registrieren
    Br->>Cod: FormatLine(frame)
    Cod-->>Br: wire line
    Br->>Tr: WriteRawLine(line)
    Tr->>Hw: UART senden
    Hw-->>Tr: response line
    Tr-->>Br: RawLineReceived(line)
    Br->>Cod: TryParseLine(line)
    Cod-->>Br: ProtocolFrame
    Br->>Br: Pending match (exact/fallback)
    Br-->>Svc: ProtocolFrame response
    Svc->>Svc: Validate + Payload parse
    Svc-->>App: Ergebnis
```

## 2) Asynchrones Event bis zum Service-Callback

Beispiel: Button-Event wird an ZumoButtonService weitergereicht.

```mermaid
sequenceDiagram
    participant Hw as Zumo UART Firmware
    participant Tr as ZumoConnectionTransport
    participant Br as RequestResponseBroker
    participant Cod as ZumoProtocolCodec
    participant Ev as EventRouter
    participant Btn as ZumoButtonService
    participant App as Consumer

    Hw-->>Tr: Event line
    Tr-->>Br: RawLineReceived(line)
    Br->>Cod: TryParseLine(line)
    Cod-->>Br: ProtocolFrame(type=Event)
    Br->>Ev: Route(frame)
    Ev->>Btn: OnEvent(frame)
    Btn->>Btn: ParsePressed + state update
    Btn-->>App: ButtonChanged event
```

## 3) Service nutzt Com-Muster (Query, Command, Event)

```mermaid
flowchart LR
    Q[Query Pattern<br/>GetRequest->GetResponse] --> BR[RequestResponseBroker]
    C[Command Pattern<br/>SetRequest->SetResponse] --> BR
    E[Event Pattern<br/>Subscribe->Route] --> EV[EventRouter]

    BR --> COD[ZumoProtocolCodec]
    BR --> TR[ZumoConnectionTransport]
    TR --> UART[UART Device]
    UART --> TR
    BR --> EV
```

## Kerndatenfluss

- Service definiert Address, Sdf, Payload.
- Broker steuert Lebenszyklus der Anfrage.
- Codec übersetzt Wire-Format.
- Transport koppelt an UART und liefert eingehende Zeilen zurück.
- EventRouter bedient abonnierte Event-Handler.
