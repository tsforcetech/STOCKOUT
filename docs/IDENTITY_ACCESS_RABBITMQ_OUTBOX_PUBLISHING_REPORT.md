# Identity Access RabbitMQ Outbox Publishing Report

## Delivery Model
**AT-LEAST-ONCE**

### Why:
RabbitMQ message publishing and the database state update (marking as Published) are not committed atomically in a single distributed transaction. 

### Duplicate possibility:
There is a scenario where RabbitMQ confirms the publish successfully, but the worker process crashes before it can update the SQL database to mark the outbox row as Published. Upon restart, the worker will claim the same message and publish it again. 

### Consumer requirement:
Because of the at-least-once delivery model, consumers of these events **must deduplicate** messages using the `EventId` (which is mapped to the RabbitMQ `MessageId`).

## RabbitMQ Topology

- **Exchange**: `emcore.events`
- **Type**: `topic`
- **Durable**: yes
- **Routing key strategy**: `identity.<event-name>` (e.g., `identity.user.registered.v1`) based on the lowercased message type.
- **Publisher confirms**: enabled
- **Mandatory publishing**: enabled where supported (unroutable messages are treated as failure).
- **Messages**: persistent (`DeliveryMode = DeliveryModes.Persistent`)

## Configuration

Required configuration keys (No credentials included):

```json
"RabbitMq": {
  "Enabled": true,
  "HostName": "...",
  "Port": 5672,
  "VirtualHost": "/",
  "UserName": "...",
  "Password": "...",
  "Exchange": "emcore.events",
  "ConnectionName": "identity-access-worker",
  "PublisherConfirmTimeoutSeconds": 10
},
"Outbox": {
  "Enabled": true,
  "BatchSize": 50,
  "PollingIntervalSeconds": 5,
  "MaxPublishAttempts": 10
}
```

## External Tests

**REAL SQL CLAIM CONCURRENCY**: DEFERRED
External SQL integration service was unavailable in the coding-agent environment. Replaced with atomic stored procedure claim semantics relying on SQL Server locks (`UPDLOCK`, `READPAST`, `ROWLOCK`).

**REAL RABBITMQ BROKER TEST**: DEFERRED
External RabbitMQ broker was unavailable in the coding-agent environment. Validated via publisher abstraction tests and unit tests.
