# EMCORE Platform — Realtime & Event Contract Architecture Reference

**Scope:** `Emcore.RealtimeGateway` (Port 5225), `Emcore.ConversationRealtime.Api` (Port 5208), and `Emcore.NotificationIntegration.Api` (Port 5201).
**Protocols:** HTTP/2 REST Webhook Event Callbacks, Server-Sent Events (SSE), and WebSockets / SignalR Negotiation.

---

## 1. OpenAPI Representation of Real-Time & Webhook Interfaces

While OpenAPI 3.0 traditionally models request-reply RESTful HTTP invocations, modern event-driven architectures require formal specification of asynchronous event notifications, persistent connection handshakes, and outgoing webhook deliveries. The EMCORE Platform integrates these asynchronous capabilities directly into our generated OpenAPI contracts.

---

## 2. WebSocket / SignalR Connection Handshakes (`Emcore.RealtimeGateway`)

Real-time streaming channels (such as live auction bidding feed updates, system alert broadcasts, and active chat threads) initiate over standard HTTP REST protocol handshakes before upgrading to persistent binary WebSocket frames.

### 2.1 Connection Negotiation Endpoints
The OpenAPI contract for `Emcore.RealtimeGateway` exposes explicit endpoint definitions for protocol handshake initiation:
* **`POST /api/v1/realtime/negotiate`:**
  * **Description:** Negotiates real-time transport protocol capabilities and issues ephemeral streaming connection tokens.
  * **Authentication:** Require JWT Bearer token authentication.
  * **Response (`200 OK`):** Returns supported transport schemes (`WebSockets`, `ServerSentEvents`), active server timeout durations, and signed connection access tokens.
* **WebSocket Token Security Guarantee:** Because standard browser JavaScript WebSocket APIs cannot natively attach HTTP Authorization headers during initial TCP upgrade handshakes, negotiation contracts specify that connection authentication tokens must be transmitted securely either via ephemeral TLS query parameters (`?access_token=<token>`) or dedicated initial auth authentication messaging frames immediately upon socket open.

---

## 3. Webhook Subscription & Callback Contracts (`Emcore.NotificationIntegration.Api`)

To enable enterprise third-party system integrations, `Emcore.NotificationIntegration.Api` manages event subscription webhooks. Rather than leaving outgoing event schema undocumented, OpenAPI 3.0 Webhook definitions specify exact payload delivery contracts.

### 3.1 Webhook Subscription Management
* **`POST /api/v1/webhooks/register`:** Allows authorized organization administrators to register target delivery HTTPS URLs, assign cryptographic HMAC signing secret keys, and filter subscribing domain event topic types (e.g., `deal.accepted`, `payment.cleared`, `inspection.completed`).

### 3.2 Outgoing Event Delivery Protocol (OpenAPI Callbacks)
When an asynchronous domain event fires, the integration gateway dispatches structured JSON payloads to registered external callback endpoints. The OpenAPI contract describes this outgoing message schema under OpenAPI Callback specifications:

```json
{
  "event_id": "01J2K45W8Q0000000000000000",
  "event_type": "deal.accepted",
  "timestamp": "2026-08-06T11:00:00Z",
  "tenant_id": "01J2K45M3H0000000000000000",
  "payload": {
    "deal_id": "8923a-f3bc-49de-a78b-00129",
    "final_price": 145000.00,
    "currency": "USD",
    "buyer_org_id": "01J2K46N9D0000000000000000"
  }
}
```

* **Cryptographic Delivery Verification:** Every webhook delivery request includes an immutable HTTP verification header:
  * `X-Emcore-Signature: t=1722942000,v1=9e8c7b6f5d4e3a2b1c0d9e8f7a6b5c4d3e2f1a0b9c8d7e6f5a4b3c2d1e0f`
  * Subscribers compute an HMAC-SHA256 digest over the raw timestamp and JSON payload using their shared webhook registration secret to guarantee zero tamper verification.

---

## 4. Server-Sent Events (SSE) Streaming Contracts

For unidirectional low-latency dashboards and compliance log tailing, services expose Server-Sent Events streaming endpoints:
* **ContentType:** `text/event-stream`
* **Specification Behavior:** OpenAPI contracts document SSE stream operations with persistent HTTP 200 responses emitting continuous newline-delimited event data blocks without connection closure.
