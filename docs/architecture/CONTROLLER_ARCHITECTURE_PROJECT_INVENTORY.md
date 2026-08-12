# EMCORE / STOCKOUT Controller Architecture Project Inventory

| Project | Project Type | Current Endpoint Style | Controller Migration Required | Existing Controllers | Minimal API Count | Business Logic in Program.cs | Database Logic in Program.cs | Target Architecture | Status |
|---|---|---|---|---|---|---|---|---|---|
| Emcore.ApiGateway | HTTP Gateway | YARP / Minimal API | No (Excluded) | 0 | 6 | No | No | Unchanged | NOT APPLICABLE |
| Emcore.PublicBff | BFF | Minimal API | Yes | 0 | 3 | No | No | Controller-based | PENDING |
| Emcore.PortalBff | BFF | Minimal API | Yes | 0 | 3 | No | No | Controller-based | PENDING |
| Emcore.McpGateway | BFF / HTTP Gateway | Minimal API | Yes (for normal HTTP) | 0 | 3 | No | No | Controller-based | PENDING |
| Emcore.RealtimeGateway | Realtime HTTP Host | SignalR / Minimal | Yes (for non-Hubs) | 0 | 3 | No | No | Hubs + Controllers | PENDING |
| Emcore.IdentityAccess.Api | HTTP API | Minimal API | Yes | 0 | 40 | Yes (routing/auth) | No | Controller-based | PENDING |
| Emcore.UserOrganization.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.CatalogListing.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.InventoryMedia.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.SearchDiscovery.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.BiddingDeal.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.InspectionTrust.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.SubscriptionPayment.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.ConversationRealtime.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.NotificationIntegration.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.WorkflowScheduler.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.AuditReporting.Api | HTTP API | Minimal API | Yes | 0 | 6 | No | No | Controller-based | PENDING |
| Emcore.IdentityAccess.Worker | Worker | None | No | 0 | 0 | No | No | Unchanged | NOT APPLICABLE |
| Emcore.IdentityAccess.Migrator | Migrator | None | No | 0 | 0 | No | No | Migrator-friendly | NOT APPLICABLE |
