import os
import sys
from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.units import inch
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether, HRFlowable, ListFlowable, ListItem
)
from reportlab.pdfgen import canvas

class NumberedCanvas(canvas.Canvas):
    """
    Two-pass canvas to add running headers and footers with accurate 'Page X of Y' numbering.
    Suppresses headers and footers on the cover page.
    """
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        num_pages = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_page_decorations(num_pages)
            super().showPage()
        super().save()

    def draw_page_decorations(self, page_count):
        if self._pageNumber == 1:
            # Skip header and footer on cover page
            return

        self.saveState()
        self.setFont("Helvetica", 9)
        self.setFillColor(colors.HexColor("#64748B")) # Slate gray

        # Draw Running Header
        self.drawString(54, 750, "EMCORE / StockOut Platform — Developer Onboarding & Architecture Guide")
        self.setStrokeColor(colors.HexColor("#CBD5E1"))
        self.setLineWidth(0.75)
        self.line(54, 742, 558, 742)

        # Draw Running Footer
        self.line(54, 50, 558, 50)
        self.drawString(54, 38, "CONFIDENTIAL & PROPRIETARY — STOCKOUT / EMCORE PLATFORM TEAM")
        page_str = f"Page {self._pageNumber} of {page_count}"
        self.drawRightString(558, 38, page_str)
        self.restoreState()

def create_onboarding_pdf(output_filename):
    # Page Setup: Letter, 0.75 in (54 pt) margins
    # Printable width = 612 - 108 = 504 pt
    doc = SimpleDocTemplate(
        output_filename,
        pagesize=letter,
        leftMargin=54,
        rightMargin=54,
        topMargin=64,
        bottomMargin=64
    )

    styles = getSampleStyleSheet()
    
    # Custom Palette
    c_primary = colors.HexColor("#0F172A")    # Deep Navy / Slate 900
    c_secondary = colors.HexColor("#2563EB")  # Vivid Blue / Blue 600
    c_accent = colors.HexColor("#0D9488")     # Teal 600
    c_text = colors.HexColor("#1E293B")       # Slate 800
    c_muted = colors.HexColor("#475569")      # Slate 600
    c_bg_light = colors.HexColor("#F8FAFC")   # Slate 50
    c_border = colors.HexColor("#E2E8F0")     # Slate 200

    # Typography Styles
    title_style = ParagraphStyle(
        'CoverTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=28,
        leading=34,
        textColor=c_primary,
        alignment=0, # Left aligned
        spaceAfter=15
    )

    subtitle_style = ParagraphStyle(
        'CoverSubtitle',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=14,
        leading=20,
        textColor=c_muted,
        alignment=0,
        spaceAfter=30
    )

    h1_style = ParagraphStyle(
        'Heading1_Custom',
        parent=styles['Heading1'],
        fontName='Helvetica-Bold',
        fontSize=20,
        leading=24,
        textColor=c_primary,
        spaceBefore=18,
        spaceAfter=10,
        keepWithNext=True
    )

    h2_style = ParagraphStyle(
        'Heading2_Custom',
        parent=styles['Heading2'],
        fontName='Helvetica-Bold',
        fontSize=14,
        leading=18,
        textColor=c_secondary,
        spaceBefore=14,
        spaceAfter=6,
        keepWithNext=True
    )

    h3_style = ParagraphStyle(
        'Heading3_Custom',
        parent=styles['Heading3'],
        fontName='Helvetica-Bold',
        fontSize=11,
        leading=15,
        textColor=c_primary,
        spaceBefore=10,
        spaceAfter=4,
        keepWithNext=True
    )

    body_style = ParagraphStyle(
        'Body_Custom',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=10,
        leading=14.5,
        textColor=c_text,
        spaceAfter=8
    )

    body_bold = ParagraphStyle(
        'Body_Bold',
        parent=body_style,
        fontName='Helvetica-Bold'
    )

    callout_style = ParagraphStyle(
        'Callout',
        parent=body_style,
        fontSize=9.5,
        leading=14,
        textColor=colors.HexColor("#1E3A8A"),
        backColor=colors.HexColor("#EFF6FF"),
        borderColor=colors.HexColor("#93C5FD"),
        borderWidth=1,
        borderPadding=10,
        spaceBefore=10,
        spaceAfter=12
    )

    table_header_style = ParagraphStyle(
        'TableHeader',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=9.5,
        leading=13,
        textColor=colors.white
    )

    table_cell_style = ParagraphStyle(
        'TableCell',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9,
        leading=13,
        textColor=c_text
    )

    table_cell_bold = ParagraphStyle(
        'TableCellBold',
        parent=table_cell_style,
        fontName='Helvetica-Bold',
        textColor=c_primary
    )

    code_style = ParagraphStyle(
        'CodeStyle',
        parent=styles['Normal'],
        fontName='Courier',
        fontSize=8.5,
        leading=11,
        textColor=colors.HexColor("#0F172A"),
        backColor=colors.HexColor("#F1F5F9"),
        borderPadding=8,
        spaceAfter=8
    )

    story = []

    # =========================================================================
    # COVER PAGE
    # =========================================================================
    story.append(Spacer(1, 40))
    story.append(Paragraph("EMCORE PLATFORM BACKEND", ParagraphStyle('CoverPre', fontName='Helvetica-Bold', fontSize=12, leading=14, textColor=c_secondary, spaceAfter=10)))
    story.append(Paragraph("Developer Onboarding & Architecture Guide", title_style))
    story.append(Paragraph("A comprehensive manual for Software Engineers, Architecture Reviewers, and DevOps Specialists onboarding onto the EMCORE / StockOut cloud-native enterprise microservices ecosystem.", subtitle_style))
    
    story.append(HRFlowable(width="100%", thickness=3, color=c_secondary, spaceBefore=10, spaceAfter=40))

    meta_data = [
        [Paragraph("<b>Document Version:</b>", table_cell_style), Paragraph("1.0 (Production Setup Reference)", table_cell_style)],
        [Paragraph("<b>Target Audience:</b>", table_cell_style), Paragraph("New Engineering Team Members, Developers & DevOps", table_cell_style)],
        [Paragraph("<b>Core Framework:</b>", table_cell_style), Paragraph(".NET 10.0 / ASP.NET Core 10 (C# 13+)", table_cell_style)],
        [Paragraph("<b>Architecture Pattern:</b>", table_cell_style), Paragraph("Clean Architecture, Domain-Driven Design (DDD), Microservices", table_cell_style)],
        [Paragraph("<b>Primary Database & ORM:</b>", table_cell_style), Paragraph("Microsoft SQL Server with Dapper & Stored Procedures", table_cell_style)],
        [Paragraph("<b>Messaging & Async:</b>", table_cell_style), Paragraph("MassTransit with self-hosted RabbitMQ (Outbox/Inbox pattern)", table_cell_style)],
        [Paragraph("<b>Cloud Deployment Target:</b>", table_cell_style), Paragraph("AWS ECS Fargate (UAE Region) via GitHub Actions CI/CD", table_cell_style)],
    ]
    meta_table = Table(meta_data, colWidths=[140, 364])
    meta_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), c_bg_light),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 8),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
    ]))
    story.append(meta_table)
    
    story.append(Spacer(1, 60))
    callout_box = Paragraph(
        "<b>👋 Welcome to the EMCORE / StockOut Engineering Team!</b><br/>"
        "This manual is your definitive blueprint for understanding how our backend operates. Whether you are adding a new domain feature, drafting a migration, tuning Dapper queries, or spinning up local .NET Aspire profiles, this guide ensures you build software that aligns with our enterprise reliability, scalability, and strict layering standards.",
        callout_style
    )
    story.append(callout_box)
    
    story.append(PageBreak())

    # =========================================================================
    # SECTION 1: EXECUTIVE SUMMARY & TECH STACK
    # =========================================================================
    story.append(Paragraph("1. Executive Summary & Core Technology Stack", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "The <b>EMCORE Platform</b> serves as the enterprise cloud-native backend powering <b>StockOut</b> (Production Domain: <code>api.stockout.com</code> | Dev Domain: <code>stockout.flowb.io</code>). Designed to handle complex marketplace workflows—including auction bidding, escrow payments, verified grading inspections, realtime chats, and media catalogs—EMCORE is partitioned into <b>12 specialized business microservices</b>, <b>5 API Gateways/BFFs</b>, and an orchestration layer.",
        body_style
    ))
    
    story.append(Paragraph("Platform Technical Stack", h2_style))
    
    stack_data = [
        [Paragraph("Layer / Capability", table_header_style), Paragraph("Technology Choice", table_header_style), Paragraph("Architectural Rationale", table_header_style)],
        [Paragraph("<b>Runtime Framework</b>", table_cell_bold), Paragraph(".NET 10 / ASP.NET Core 10", table_cell_style), Paragraph("Flagship performance, multi-platform container efficiency, advanced OpenAPI and diagnostics support.", table_cell_style)],
        [Paragraph("<b>Architecture Pattern</b>", table_cell_bold), Paragraph("Clean Architecture + DDD", table_cell_style), Paragraph("Strict isolation of business Domain logic from infrastructure frameworks and external adapters.", table_cell_style)],
        [Paragraph("<b>Data Access</b>", table_cell_bold), Paragraph("Dapper + SQL Server", table_cell_style), Paragraph("High-speed micro-ORM using Stored Procedures exclusively for secure, database-optimized operations.", table_cell_style)],
        [Paragraph("<b>Event-Driven Messaging</b>", table_cell_bold), Paragraph("MassTransit + RabbitMQ", table_cell_style), Paragraph("Asynchronous pub/sub architecture utilizing transactional Inbox and Outbox patterns for reliable delivery.", table_cell_style)],
        [Paragraph("<b>Caching & State</b>", table_cell_bold), Paragraph("StackExchange.Redis", table_cell_style), Paragraph("Distributed in-memory caching for sub-millisecond lookups and transient state synchronization.", table_cell_style)],
        [Paragraph("<b>Search Engine</b>", table_cell_bold), Paragraph("OpenSearch", table_cell_style), Paragraph("Full-text search, multi-faceted filtering, and rapid listing indexing for marketplace discovery.", table_cell_style)],
        [Paragraph("<b>Object Storage</b>", table_cell_bold), Paragraph("AWS S3 (Prod) / MinIO (Local)", table_cell_style), Paragraph("Signed URL upload/download generation for inspection media, listing photos, and PDF invoices.", table_cell_style)],
        [Paragraph("<b>Observability & Logs</b>", table_cell_bold), Paragraph("OpenTelemetry (OTEL)", table_cell_style), Paragraph("Unified distributed tracing, metrics, and structured logging exported via OTLP to telemetry collectors.", table_cell_style)],
        [Paragraph("<b>Local Orchestration</b>", table_cell_bold), Paragraph(".NET Aspire & Docker Compose", table_cell_style), Paragraph("Allows selective multi-project debugging profiles without exhausting developer workstation CPU/RAM.", table_cell_style)],
    ]
    
    stack_table = Table(stack_data, colWidths=[110, 140, 254])
    stack_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), c_primary),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 6),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, c_bg_light])
    ]))
    story.append(stack_table)
    story.append(Spacer(1, 15))

    # =========================================================================
    # SECTION 2: REPOSITORY ARCHITECTURE & LAYER BOUNDARIES
    # =========================================================================
    story.append(Paragraph("2. Repository Architecture & Clean Architecture Layering", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "EMCORE is structured as a <b>GitHub Private Monorepository</b>. We manage dependencies centrally using <code>Directory.Packages.props</code> and share compiler settings via <code>Directory.Build.props</code> and <code>global.json</code>. Every deployable microservice implements a rigid Clean Architecture directory structure.",
        body_style
    ))
    
    story.append(Paragraph("Monorepo Directory Layout", h2_style))
    story.append(Paragraph("When you clone the repository, you will navigate the following primary root folders:", body_style))
    
    tree_text = (
        "emcore-platform/\n"
        "├── Emcore.Platform.slnx          # Central solution referencing all services and tests\n"
        "├── Directory.Packages.props      # Central Package Management (CPM) version definitions\n"
        "├── gateways/                     # External doors: API Gateway, BFFs, MCP, Realtime Hubs\n"
        "├── orchestration/                # .NET Aspire AppHost and ServiceDefaults\n"
        "├── services/                     # The 12 independent business microservices\n"
        "├── building-blocks/              # Reusable technical support libraries (No domain code!)\n"
        "├── contracts/                    # Central shared contracts (OpenAPI, Events, Webhooks, MCP)\n"
        "├── infrastructure/               # AWS ECS docs, Terraform placeholders, & local Docker setup\n"
        "├── scripts/                      # Developer utility & automation PowerShell/bash scripts\n"
        "└── docs/                         # Architecture specification specifications & onboarding guides"
    )
    story.append(Paragraph(f"<pre>{tree_text}</pre>", code_style))
    story.append(Spacer(1, 10))

    story.append(Paragraph("Clean Architecture Dependency Graph", h2_style))
    story.append(Paragraph(
        "In EMCORE, <b>dependency flow is strictly inwards</b>. Domain concepts never depend on technical frameworks or databases. Our continuous integration pipeline runs automated architecture tests (using <code>NetArchTest</code>) on every PR; violations will fail your build!",
        body_style
    ))
    
    dep_text = (
        "┌─────────────────────────────────────────────────────────────┐\n"
        "│                      Api & Worker Projects                  │  <-- Entry points & Hosting\n"
        "└──────────────────────────────┬──────────────────────────────┘\n"
        "                               │ references (Composition Root only)\n"
        "                               ▼\n"
        "┌─────────────────────────────────────────────────────────────┐\n"
        "│              Application & Infrastructure Layers            │  <-- Handlers, Repositories, Adapters\n"
        "└──────────────────────────────┬──────────────────────────────┘\n"
        "                               │ references\n"
        "                               ▼\n"
        "┌─────────────────────────────────────────────────────────────┐\n"
        "│                Domain Layer & Pure Contracts                │  <-- ZERO Infrastructure Dependencies\n"
        "└─────────────────────────────────────────────────────────────┘"
    )
    story.append(Paragraph(f"<pre>{dep_text}</pre>", code_style))
    
    story.append(Paragraph("<b>Strict Architectural Rules:</b>", body_bold))
    story.append(Paragraph("• <b>Domain Layer:</b> Contains Entities, Value Objects, Enums, Domain Events, and Domain Exceptions. Must NEVER reference Dapper, EF, ASP.NET Core, or Infrastructure assemblies.", body_style))
    story.append(Paragraph("• <b>Application Layer:</b> Contains Commands, Queries, Validation, Behaviors, and Abstractions. Never depends on API or Worker projects.", body_style))
    story.append(Paragraph("• <b>Infrastructure Layer:</b> Implements Dapper <code>IStoredProcedureExecutor</code> repositories, MassTransit consumers, Redis caches, and S3 integrations.", body_style))
    story.append(Paragraph("• <b>Service Isolation:</b> One microservice must NEVER directly reference the assembly or database of another microservice! Communication across services is strictly via MassTransit event publishing or Gateway HTTP contracts.", body_style))

    story.append(PageBreak())

    # =========================================================================
    # SECTION 3: MICROSERVICES INVENTORY
    # =========================================================================
    story.append(Paragraph("3. EMCORE Microservices Portfolio", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "The backend is decomposed into 12 autonomous microservices. Each service owns an `.Api` project (REST endpoints, Problem Details, OpenAPI) and a `.Worker` project (MassTransit background consumers and scheduled job runners).",
        body_style
    ))
    
    ms_summary_data = [
        [Paragraph("Service Key", table_header_style), Paragraph("Namespace", table_header_style), Paragraph("Port", table_header_style), Paragraph("Logical Database Name", table_header_style)],
        [Paragraph("<b>identity-access</b>", table_cell_style), Paragraph("Emcore.IdentityAccess", table_cell_style), Paragraph("7101", table_cell_style), Paragraph("<code>EMCORE_IDENTITY_DB</code>", table_cell_style)],
        [Paragraph("<b>user-organization</b>", table_cell_style), Paragraph("Emcore.UserOrganization", table_cell_style), Paragraph("7102", table_cell_style), Paragraph("<code>EMCORE_ORGANIZATION_DB</code>", table_cell_style)],
        [Paragraph("<b>catalog-listing</b>", table_cell_style), Paragraph("Emcore.CatalogListing", table_cell_style), Paragraph("7103", table_cell_style), Paragraph("<code>EMCORE_CATALOG_LISTING_DB</code>", table_cell_style)],
        [Paragraph("<b>inventory-media</b>", table_cell_style), Paragraph("Emcore.InventoryMedia", table_cell_style), Paragraph("7104", table_cell_style), Paragraph("<code>EMCORE_INVENTORY_MEDIA_DB</code>", table_cell_style)],
        [Paragraph("<b>search-discovery</b>", table_cell_style), Paragraph("Emcore.SearchDiscovery", table_cell_style), Paragraph("7105", table_cell_style), Paragraph("<code>EMCORE_SEARCH_DB</code>", table_cell_style)],
        [Paragraph("<b>bidding-deal</b>", table_cell_style), Paragraph("Emcore.BiddingDeal", table_cell_style), Paragraph("7106", table_cell_style), Paragraph("<code>EMCORE_BIDDING_DEAL_DB</code>", table_cell_style)],
        [Paragraph("<b>inspection-trust</b>", table_cell_style), Paragraph("Emcore.InspectionTrust", table_cell_style), Paragraph("7107", table_cell_style), Paragraph("<code>EMCORE_INSPECTION_TRUST_DB</code>", table_cell_style)],
        [Paragraph("<b>subscription-payment</b>", table_cell_style), Paragraph("Emcore.SubscriptionPayment", table_cell_style), Paragraph("7108", table_cell_style), Paragraph("<code>EMCORE_SUBSCRIPTION_PAYMENT_DB</code>", table_cell_style)],
        [Paragraph("<b>conversation-realtime</b>", table_cell_style), Paragraph("Emcore.ConversationRealtime", table_cell_style), Paragraph("7109", table_cell_style), Paragraph("<code>EMCORE_CONVERSATION_DB</code>", table_cell_style)],
        [Paragraph("<b>notification-integration</b>", table_cell_style), Paragraph("Emcore.NotificationIntegration", table_cell_style), Paragraph("7110", table_cell_style), Paragraph("<code>EMCORE_NOTIFICATION_INTEGRATION_DB</code>", table_cell_style)],
        [Paragraph("<b>workflow-scheduler</b>", table_cell_style), Paragraph("Emcore.WorkflowScheduler", table_cell_style), Paragraph("7111", table_cell_style), Paragraph("<code>EMCORE_WORKFLOW_DB</code>", table_cell_style)],
        [Paragraph("<b>audit-reporting</b>", table_cell_style), Paragraph("Emcore.AuditReporting", table_cell_style), Paragraph("7112", table_cell_style), Paragraph("<code>EMCORE_AUDIT_REPORTING_DB</code>", table_cell_style)],
    ]
    ms_table = Table(ms_summary_data, colWidths=[120, 140, 50, 194])
    ms_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), c_primary),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 5),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, c_bg_light])
    ]))
    story.append(ms_table)
    story.append(Spacer(1, 15))

    story.append(Paragraph("Detailed Microservice Capabilities & Uses", h2_style))
    
    services_detail = [
        ("1. Identity Access Service (Port 7101)", "Emcore.IdentityAccess", 
         "Acts as the paramount authentication and security gatekeeper. Responsible for user credential validation, JWT token issuance, multi-factor authentication (MFA), role definitions, and dynamic permission decision evaluation. All API requests across the platform rely on security context generated by this service."),
        
        ("2. User Organization Service (Port 7102)", "Emcore.UserOrganization", 
         "Manages corporate tenancies, company profiles, user organizational roles, buyer/seller verification onboarding workflows, and address/branch rosters. In a B2B marketplace, this service enables enterprise accounts to govern staff access and trading credentials."),
        
        ("3. Catalog Listing Service (Port 7103)", "Emcore.CatalogListing", 
         "The foundational bedrock of StockOut. Manages inventory product hierarchies, category attributes, lot item classifications, SKU specs, pricing metadata, listing lifecycle states (Draft, Published, Suspended, Expired), and inventory stock quantities."),
        
        ("4. Inventory Media Service (Port 7104)", "Emcore.InventoryMedia", 
         "Processes all media attachments associated with listings, auctions, and inspection certificates. Communicates with AWS S3 / MinIO to issue cryptographic signed upload/download URLs, ensuring secure high-throughput image and video transfers without bottlenecking application servers."),
        
        ("5. Search Discovery Service (Port 7105)", "Emcore.SearchDiscovery", 
         "Integrates deeply with OpenSearch to deliver sub-millisecond keyword search, multi-faceted filtering (price range, category, condition, location), relevance grading, and discovery feeds. Consumes listing integration events to ensure near-real-time index synchronization."),
        
        ("6. Bidding Deal Service (Port 7106)", "Emcore.BiddingDeal", 
         "Implements high-frequency transactional marketplace workflows: live real-time auctions, sealed-bid mechanisms, automatic reserve price checks, negotiation room offers, deal acceptance protocols, and legal binding agreement generation between buyers and sellers."),
        
        ("7. Inspection Trust Service (Port 7107)", "Emcore.InspectionTrust", 
         "Enforces marketplace credibility by managing certified third-party item grading, quality compliance inspection reports, trust scoring algorithms, vendor reliability ratings, and dispute verification evidence trails."),
        
        ("8. Subscription Payment Service (Port 7108)", "Emcore.SubscriptionPayment", 
         "Governs monetization models, member tier billing cycles (Free, Gold, Enterprise), escrow payment retention for large transactions, invoice generation, and secure third-party payment provider integrations (Stripe/Bank Gateway abstraction)."),
        
        ("9. Conversation Realtime Service (Port 7109)", "Emcore.ConversationRealtime", 
         "Facilitates live negotiation communication between trading counterparts. Manages chat thread persistence, message encryption, read recipes, and coordinates with the Realtime Gateway for SignalR WebSocket message delivery."),
        
        ("10. Notification Integration Service (Port 7110)", "Emcore.NotificationIntegration", 
         "The multi-channel dispatch center for platform communication. Consumes domain events from RabbitMQ and formats outbox deliveries for transactional email, SMS OTPs, mobile push notifications, and external client webhook endpoints."),
        
        ("11. Workflow Scheduler Service (Port 7111)", "Emcore.WorkflowScheduler", 
         "Orchestrates scheduled cron-style background jobs, stateful multi-service sagas, automated expired listing cleanups, auction closing sweeps, and retry mechanisms across the distributed architecture."),
        
        ("12. Audit Reporting Service (Port 7112)", "Emcore.AuditReporting", 
         "Implements tamper-resistant compliance audit trails, recording administrative overrides, financial transaction changes, security policy updates, and feeding analytics aggregators for corporate reporting dashboards.")
    ]

    for title, ns, desc in services_detail:
        block = []
        block.append(Paragraph(title, h3_style))
        block.append(Paragraph(f"<b>Namespace:</b> <code>{ns}</code> | <b>Architecture Type:</b> Clean Architecture (Api + Worker + Domain + Application + Infra)", ParagraphStyle('Sub', parent=body_style, fontSize=8.5, textColor=c_muted, spaceAfter=4)))
        block.append(Paragraph(desc, body_style))
        block.append(Spacer(1, 6))
        story.append(KeepTogether(block))

    story.append(PageBreak())

    # =========================================================================
    # SECTION 4: GATEWAYS & BFFs
    # =========================================================================
    story.append(Paragraph("4. Gateways & Backend-for-Frontend (BFF) Architecture", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "To protect internal microservices and optimize client experiences, EMCORE exposes <b>no internal microservice ports to public Internet networks</b>. All traffic flows through specialized gateways and BFFs located in the <code>gateways/</code> directory.",
        body_style
    ))

    gw_data = [
        [Paragraph("Gateway / BFF Project", table_header_style), Paragraph("Port", table_header_style), Paragraph("Core Function & Target Consumer", table_header_style)],
        [Paragraph("<b>Emcore.ApiGateway</b>", table_cell_bold), Paragraph("7000", table_cell_style), Paragraph("Built on Microsoft <b>YARP</b> (Yet Another Reverse Proxy). Acts as the primary ingress routing layer, enforcing correlation ID injection, rate limiting, SSL termination, and systemic health verifications.", table_cell_style)],
        [Paragraph("<b>Emcore.PublicBff</b>", table_cell_bold), Paragraph("7010", table_cell_style), Paragraph("<b>Backend-for-Frontend</b> tailored for public, unauthenticated traffic. Aggregates category trees, promotional banners, and public catalog search results into streamlined single-request mobile/web views.", table_cell_style)],
        [Paragraph("<b>Emcore.PortalBff</b>", table_cell_bold), Paragraph("7020", table_cell_style), Paragraph("<b>Backend-for-Frontend</b> for authenticated domain actors (Buyers, Sellers, Inspectors, Admins). Handles session security, aggregates private bidding dashboards, escrow status, and organizational analytics.", table_cell_style)],
        [Paragraph("<b>Emcore.McpGateway</b>", table_cell_bold), Paragraph("7030", table_cell_style), Paragraph("<b>Model Context Protocol (MCP)</b> Host Server. Exposes standardized AI tool registries and schemas, allowing trusted LLMs and AI agents (like Antigravity) to securely query and operate platform features.", table_cell_style)],
        [Paragraph("<b>Emcore.RealtimeGateway</b>", table_cell_bold), Paragraph("7040", table_cell_style), Paragraph("Dedicated <b>SignalR WebSocket Hub</b>. Maintains persistent real-time connections to client web/mobile apps, pushing instant auction bid out-bid alerts, live chat messages, and notification toasts.", table_cell_style)],
    ]
    gw_table = Table(gw_data, colWidths=[130, 45, 329])
    gw_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), c_primary),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 6),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, c_bg_light])
    ]))
    story.append(gw_table)
    story.append(Spacer(1, 15))

    # =========================================================================
    # SECTION 5: TECHNICAL BUILDING BLOCKS
    # =========================================================================
    story.append(Paragraph("5. Reusable Technical Building Blocks", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "Our modular strategy forbids duplicating utility boilerplate across services. Instead, cross-cutting technical capabilities reside in <code>building-blocks/</code> as reusable NuGet-style project libraries. <br/><b>CRITICAL RULE:</b> Building blocks must contain zero domain concepts (No Listing, Bid, Deal, or Organization classes!).",
        body_style
    ))
    
    bb_list = [
        ("Emcore.BuildingBlocks.Core", "Provides core foundational paradigms: functional <code>Result&lt;T&gt;</code> wrappers, semantic exception base classes (<code>DomainException</code>, <code>NotFoundException</code>, <code>ConflictException</code>), deterministic Ulid ID generation, and testable <code>IClock</code> services."),
        ("Emcore.BuildingBlocks.Api", "Implements standardized REST APIs: ASP.NET Core Problem Details error formatting, <code>GlobalExceptionHandler</code>, structured pagination envelopes (<code>PagedResponse&lt;T&gt;</code>, <code>CursorResponse&lt;T&gt;</code>), security headers, and Correlation ID middleware."),
        ("Emcore.BuildingBlocks.Data", "<b>The Heart of Data Access:</b> Wraps <code>Dapper</code> and <code>Microsoft.Data.SqlClient</code>. Provides <code>ISqlConnectionFactory</code> and <code>IStoredProcedureExecutor</code>. Enforces that all database execution occurs via stored procedures with explicit command timeouts and cancellation token safety."),
        ("Emcore.BuildingBlocks.Messaging", "Wraps <code>MassTransit</code> and RabbitMQ. Enforces the event-driven backbone via <code>IntegrationEvent</code> envelopes. Provides transactional <b>Outbox and Inbox store pattern abstractions</b> to guarantee zero-data-loss and idempotent message handling."),
        ("Emcore.BuildingBlocks.Security", "Delivers identity evaluation: <code>ICurrentUser</code>, <code>IOrganizationContext</code>, and <code>IPermissionChecker</code>. Includes automatic sensitive value masking helpers to prevent PII leaks in Application diagnostics."),
        ("Emcore.BuildingBlocks.Observability", "Centralizes <b>OpenTelemetry</b> metrics, traces, and activity sources. Ensures every request across HTTP, MassTransit, and Dapper generates distributed OTLP trace headers and uniform structural logging resource attributes."),
        ("Emcore.BuildingBlocks.Caching", "Encapsulates distributed caching via <code>StackExchange.Redis</code>. Provides robust cache-key builder helpers, graceful failover capabilities, and a mockable <code>ICacheService</code> abstraction."),
        ("Emcore.BuildingBlocks.Storage", "Object storage abstraction over AWS SDK S3 and MinIO. Handles generating secure cryptographic signed upload/download tokens (<code>SignedUploadRequest</code>) so clients interact directly with buckets."),
        ("Emcore.BuildingBlocks.Idempotency", "Provides validation engines (<code>IdempotencyKeyValidator</code>) that prevent duplicate execution of high-risk operational requests (such as auction bids or recurring payments)."),
        ("Emcore.BuildingBlocks.Testing", "Standardized testing fixtures: WebApplicationFactory wrappers, deterministic mock clocks, in-memory configuration builders, and automated assembly scanners for architectural compliance assertion.")
    ]

    for bb_name, bb_desc in bb_list:
        block = []
        block.append(Paragraph(f"<b>{bb_name}</b>", h3_style))
        block.append(Paragraph(bb_desc, body_style))
        block.append(Spacer(1, 4))
        story.append(KeepTogether(block))

    story.append(PageBreak())

    # =========================================================================
    # SECTION 6: LOCAL DEVELOPMENT SETUP & ORCHESTRATION
    # =========================================================================
    story.append(Paragraph("6. Local Development Setup & Orchestration", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph(
        "Running all 12 microservice APIs, 12 workers, and 5 gateways simultaneously on a single machine would cripple developer productivity. EMCORE solves this by integrating <b>.NET Aspire (<code>Emcore.AppHost</code>)</b> and granular <b>Docker Compose profiles</b>.",
        body_style
    ))

    story.append(Paragraph("A. Infrastructure Dependencies (Docker Compose)", h2_style))
    story.append(Paragraph(
        "Located in <code>infrastructure/docker/docker-compose.local.yml</code>, our Compose setup leverages selective profiles so you only launch what you need. <br/>"
        "<b>IMPORTANT NOTE ON SQL SERVER:</b> We intentionally <b>DO NOT</b> host SQL Server in local Docker containers! Developers connect to an established, centrally-managed Development SQL Server instance to maintain uniform stored procedure schema synchronicity.",
        body_style
    ))

    infra_data = [
        [Paragraph("Container Profile", table_header_style), Paragraph("Local Host Port(s)", table_header_style), Paragraph("Role in Local Environment", table_header_style)],
        [Paragraph("<b>rabbitmq</b>", table_cell_style), Paragraph("5672 (AMQP) | 15672 (Web UI)", table_cell_style), Paragraph("Message broker for MassTransit event bus integration & queue inspection.", table_cell_style)],
        [Paragraph("<b>redis</b>", table_cell_style), Paragraph("6379", table_cell_style), Paragraph("In-memory database for rapid caching and session state persistence.", table_cell_style)],
        [Paragraph("<b>opensearch</b>", table_cell_style), Paragraph("9200", table_cell_style), Paragraph("Local search cluster for testing index sync and marketplace faceted filtering.", table_cell_style)],
        [Paragraph("<b>minio</b>", table_cell_style), Paragraph("9000 (API) | 9001 (Web Console)", table_cell_style), Paragraph("S3-compatible local object storage for testing image/media signed uploads.", table_cell_style)],
        [Paragraph("<b>otel</b>", table_cell_style), Paragraph("4317 (gRPC) | 4318 (HTTP)", table_cell_style), Paragraph("OpenTelemetry Collector for capturing traces, spans, and metrics locally.", table_cell_style)],
    ]
    infra_table = Table(infra_data, colWidths=[110, 150, 244])
    infra_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), c_primary),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 5),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, c_bg_light])
    ]))
    story.append(infra_table)
    story.append(Spacer(1, 12))

    story.append(Paragraph("B. .NET Aspire Selective Launch Groups", h2_style))
    story.append(Paragraph(
        "Instead of hitting F5 on the entire solution, <code>Emcore.AppHost</code> supports targeted group execution. When debugging in Visual Studio or CLI, specify your target subsystem profile:",
        body_style
    ))
    
    aspire_data = [
        [Paragraph("Aspire Group Profile", table_header_style), Paragraph("Services Launched", table_header_style), Paragraph("Use Case / Focus Area", table_header_style)],
        [Paragraph("<b>foundation</b>", table_cell_bold), Paragraph("API Gateway, BFFs, Local Infra", table_cell_style), Paragraph("Gateway routing, YARP verification, AI MCP integration.", table_cell_style)],
        [Paragraph("<b>access</b>", table_cell_bold), Paragraph("Identity Access, User Organization", table_cell_style), Paragraph("Auth workflows, user login, JWT tokens, tenant roles.", table_cell_style)],
        [Paragraph("<b>marketplace-core</b>", table_cell_bold), Paragraph("Catalog Listing, Inventory Media, Inspection Trust", table_cell_style), Paragraph("Core listing lifecycle, S3 image uploads, inspection trust reports.", table_cell_style)],
        [Paragraph("<b>search</b>", table_cell_bold), Paragraph("Search Discovery + OpenSearch", table_cell_style), Paragraph("Search index consumption and recommendation queries.", table_cell_style)],
        [Paragraph("<b>commercial</b>", table_cell_bold), Paragraph("Bidding Deal, Subscription Payment", table_cell_style), Paragraph("Live auctions, bid execution, billing invoices, Stripe payment simulation.", table_cell_style)],
        [Paragraph("<b>engagement</b>", table_cell_bold), Paragraph("Conversation Realtime, Notification Integration", table_cell_style), Paragraph("Live chat rooms, SMS/email outbox queues, SignalR testing.", table_cell_style)],
        [Paragraph("<b>operations</b>", table_cell_bold), Paragraph("Workflow Scheduler, Audit Reporting", table_cell_style), Paragraph("Background cron sagas, administrative auditing trails.", table_cell_style)],
    ]
    aspire_table = Table(aspire_data, colWidths=[120, 170, 214])
    aspire_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), c_primary),
        ('BOX', (0, 0), (-1, -1), 1, c_border),
        ('INNERGRID', (0, 0), (-1, -1), 0.5, c_border),
        ('PADDING', (0, 0), (-1, -1), 5),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, c_bg_light])
    ]))
    story.append(aspire_table)
    story.append(Spacer(1, 15))

    story.append(Paragraph("C. Configuration Hierarchy & Health Checks", h2_style))
    story.append(Paragraph("We enforce a standard configuration override hierarchy across all projects: <br/><code>appsettings.json</code> ➔ <code>appsettings.{Environment}.json</code> ➔ User Secrets (Local Only) ➔ Environment Variables ➔ AWS Secrets Manager.", body_style))
    story.append(Paragraph("<b>Out-of-the-Box Compilation:</b> In the <code>Local</code> environment, services are preconfigured with external connections (Database, Redis, Messaging) set to <code>Enabled = false</code>. This guarantees new engineers can compile and spin up endpoints without facing immediate SQL connection timeout failures!", body_style))
    story.append(Paragraph("<b>Health Probing:</b> Every service automatically registers two standardized health endpoints:<br/>"
                           "• <code>/health/live</code> : Liveness verification (confirms process runtime status).<br/>"
                           "• <code>/health/ready</code> : Readiness probe (validates active connectivity to enabled external dependencies like SQL Server and RabbitMQ). In Local mode, disabled dependencies are reported cleanly without flagging a readiness failure.", body_style))

    story.append(PageBreak())

    # =========================================================================
    # SECTION 7: CI/CD & DEVELOPER GOLDEN RULES
    # =========================================================================
    story.append(Paragraph("7. CI/CD Pipelines & Developer Golden Rules", h1_style))
    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=2, spaceAfter=12))
    
    story.append(Paragraph("Continuous Integration & Container Deployment (GitHub Actions)", h2_style))
    story.append(Paragraph(
        "Our pipeline is fully orchestrated via GitHub Actions workflows residing in <code>.github/workflows/</code>:",
        body_style
    ))
    story.append(Paragraph("<b>1. PR Validation (<code>pr-validation.yml</code>):</b> Triggered on every pull request to <code>main</code>. Enforces SDK lock-file verification, formatting validation, builds in Release mode, runs Unit and Architecture tests, and executes security vulnerability scans on NuGet dependencies.", body_style))
    story.append(Paragraph("<b>2. Main Build & Tag (<code>main-validation.yml</code>):</b> Triggered upon merging to <code>main</code>. Repeats validation gates, detects changed deployable microservices, compiles optimized multi-stage OCI Docker container images (running under a hardened non-root user), and tags them with git commit SHAs.", body_style))
    story.append(Paragraph("<b>3. Manual Container Build (<code>manual-container-build.yml</code>):</b> Enables on-demand manual image compilation and optional deployment pushing for specific target deployable services.", body_style))
    story.append(Spacer(1, 10))

    story.append(Paragraph("🏆 EMCORE Engineering Golden Rules", h2_style))
    
    rules_box = Paragraph(
        "To preserve system architecture integrity, every team member must adhere to these inviolable standards:<br/><br/>"
        "<b>1. ZERO inline SQL in REST Endpoints or Workers:</b> All relational database queries MUST flow through <code>IStoredProcedureExecutor</code> inside the Infrastructure layer using explicit SQL Stored Procedures.<br/><br/>"
        "<b>2. NEVER leak Business Entities into Building Blocks:</b> <code>Emcore.BuildingBlocks.*</code> libraries must remain totally generic technical utilities. If a code piece mentions an auction, product, or account, it belongs in a service domain!<br/><br/>"
        "<b>3. Respect Microservice Boundary Isolation:</b> Service A cannot directly invoke Service B's assemblies or access Service B's database tables. Cross-service workflows MUST be handled asynchronously via MassTransit integration event publishing or HTTP Gateway REST requests.<br/><br/>"
        "<b>4. NO Secrets in Version Control:</b> Never commit database credentials, API secrets, or encryption keys in JSON configuration files. Use Visual Studio / .NET CLI User Secrets locally (<code>dotnet user-secrets</code>).<br/><br/>"
        "<b>5. Preserve Correlation Contexts:</b> Always ensure OpenTelemetry distributed trace headers and Correlation IDs are carried through HTTP headers and message envelopes to enable end-to-end observability across microservice hops.",
        callout_style
    )
    story.append(rules_box)
    story.append(Spacer(1, 20))

    story.append(Paragraph("Next Steps for Onboarding Developers", h2_style))
    story.append(Paragraph(
        "1. Verify your machine matches <code>global.json</code> (.NET 10 SDK) and has Docker Desktop installed.<br/>"
        "2. Open a terminal in the root directory and run <code>dotnet build Emcore.Platform.slnx</code> to verify zero-error compilation.<br/>"
        "3. Run architecture and unit test suites via <code>dotnet test</code>.<br/>"
        "4. Explore local infrastructure by running <code>docker compose -f infrastructure/docker/docker-compose.local.yml --profile rabbitmq --profile redis up -d</code>.<br/>"
        "5. Reach out to your team lead or CODEOWNERS for access credentials to the Development SQL Server instance and AWS ECS testing environments.",
        body_style
    ))
    story.append(Spacer(1, 25))

    story.append(HRFlowable(width="100%", thickness=1, color=c_border, spaceBefore=10, spaceAfter=15))
    story.append(Paragraph("<b>EMCORE Platform Backend Ecosystem</b> — Built with precision by the StockOut Engineering & Antigravity AI Team.", ParagraphStyle('FootNote', parent=body_style, fontSize=9, textColor=c_muted, alignment=1)))

    doc.build(story, canvasmaker=NumberedCanvas)
    print(f"[SUCCESS] Developer Onboarding PDF successfully created at: {output_filename}")

if __name__ == "__main__":
    out_dir = r"C:\DEV\API PROJECT\STOCKOUT\docs"
    root_dir = r"C:\DEV\API PROJECT\STOCKOUT"
    
    pdf_docs_path = os.path.join(out_dir, "EMCORE_Developer_Onboarding_Guide.pdf")
    pdf_root_path = os.path.join(root_dir, "EMCORE_Developer_Onboarding_Guide.pdf")
    
    create_onboarding_pdf(pdf_docs_path)
    # Make a copy in root as well for immediate visibility
    create_onboarding_pdf(pdf_root_path)
