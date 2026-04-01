# Com Focus

## Verantwortung des Com-Bereichs

- Senden und Empfangen von Protokollzeilen über UART.
- Umwandlung zwischen Raw-Line und strukturierter Nachricht.
- Korrelation von Requests und Responses.
- Verteilung asynchroner Events an Subscriber.

## Bausteine und Rollen

```mermaid
flowchart LR
    SVC[Service] --> BR[RequestResponseBroker]
    BR --> COD[ZumoProtocolCodec]
    BR --> TR[ZumoConnectionTransport]
    TR --> BR
    BR --> EVT[EventRouter]
    EVT --> SUB[Event Subscriber <br/> z.B. ZumoButtonService]

    PF[ProtocolFrame / MessageType / EventSubscription] -.shared model.-> BR
    PF -.shared model.-> COD
    PF -.shared model.-> EVT
```

## Request/Response-Organisation

```mermaid
flowchart TD
    A[SendRequest address+sdf+type] --> B[PendingRequest im Dictionary anlegen]
    B --> C[Frame formatieren und senden]
    C --> D[RawLineReceived]
    D --> E[Line parsen]
    E --> F{Response?}
    F -- Nein --> G{Event oder OneWay?}
    G -- Ja --> H[EventRouter.Route]
    G -- Nein --> X[Verwerfen]
    F -- Ja --> I{Exact Match<br/>Address+Sdf+Type}
    I -- Ja --> J[PendingRequest erfüllen]
    I -- Nein --> K{Unique Fallback<br/>Address+Type}
    K -- Ja --> L[Sdf normalisieren und erfüllen]
    K -- Nein --> M[Keine Zuordnung]
```

## Klassen kurz erklärt

| Klasse                  | Kurzbeschreibung                                                                          |
| ----------------------- | ----------------------------------------------------------------------------------------- |
| RequestResponseBroker   | Zentrale Koordination von SendRequest/SendOneWay, Pending-Matching und Timeout-Verhalten. |
| ZumoProtocolCodec       | Übersetzt zwischen Raw-Line und ProtocolFrame inkl. Operator-Mapping.                     |
| ZumoConnectionTransport | Liest und schreibt UART-Zeilen, schützt den Reader-Thread vor Consumer-Fehlern.           |
| EventRouter             | Verwaltet Subscriptions und routed Event-Frames auf Handler.                              |
| ProtocolFrame           | Transportobjekt für Address, MessageType, Sdf, Payload und RawLine.                       |
| EventSubscription       | Definiert Filter für Event-Routing (Address/Type/Sdf).                                    |

## Parsing und Formatierung

| Operator | Bedeutung   |
| -------- | ----------- |
| ?        | GetRequest  |
| *        | GetResponse |
| >        | SetRequest  |
| <        | SetResponse |
| !        | Event       |
| :        | OneWay      |

```mermaid
flowchart LR
    IN[Raw line] --> P[TryParseLine]
    P --> T[MessageType bestimmen]
    T --> R[Address/Sdf/Payload extrahieren]
    R --> OUT[ProtocolFrame]

    FR[ProtocolFrame] --> FMT[FormatLine]
    FMT --> LINE[Wire line]
```

## Synchronisation und Robustheit

- Broker schützt Attach/Detach und aktive Referenzen über Lock.
- Pending-Requests liegen in ConcurrentDictionary.
- Transport hat getrennte Locks für Zustand und Writes.
- Reader-Thread bleibt bei Consumer-Fehlern aktiv.

Weiter mit konkreten Laufzeitabläufen: [Communication Flows](communication-flows.md)
