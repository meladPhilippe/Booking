# Generic Payment Module — Backlog

**Status:** Proposed backlog for refinement  
**Target platform:** .NET 10 / ASP.NET Core  
**Planning horizon:** Six 2-week sprints, preceded by a short discovery and readiness activity  
**Delivery style:** Vertical slices; simulated providers first, then sandbox-backed Stripe and PayPal integrations

---

This package splits the original backlog into smaller, linked Markdown documents. The content is organized by topic and sprint so it can be reviewed, versioned, and refined more easily in a Git repository.

## Included reference copy

The [`reference/original-consolidated-backlog.md`](reference/original-consolidated-backlog.md) file preserves the earlier single-document version. Use the split documents below as the primary, maintained version.

## Start here

Read [`META.md`](META.md) for the Agile/Scrum concepts and planning practices behind this backlog. It explains how the epic, user stories, acceptance criteria, sprint goals, dependencies, and quality criteria fit together, and distinguishes official Scrum elements from common team practices.

## Recommended reading order

| Order | File | Purpose |
|---|---|---|
| 1 | [`docs/01-epic-and-scope.md`](docs/01-epic-and-scope.md) | Epic statement, business outcomes, in-scope/out-of-scope items, assumptions |
| 2 | [`docs/02-architecture-and-integration.md`](docs/02-architecture-and-integration.md) | Design principles, modular-monolith vs independent API, integration flows, solution structure, API concepts, payment states |
| 3 | [`docs/03-discovery-and-readiness.md`](docs/03-discovery-and-readiness.md) | Decisions and prerequisites to resolve before Sprint 1 |
| 4 | [`docs/sprints/sprint-1-foundation.md`](docs/sprints/sprint-1-foundation.md) | Foundation and simulated end-to-end payment workflow (PAY-001–PAY-005) |
| 5 | [`docs/sprints/sprint-2-persistence-and-idempotency.md`](docs/sprints/sprint-2-persistence-and-idempotency.md) | Persistence, idempotency, failure semantics, and safe metadata (PAY-006–PAY-009) |
| 6 | [`docs/sprints/sprint-3-stripe.md`](docs/sprints/sprint-3-stripe.md) | Stripe sandbox integration and webhooks (PAY-010–PAY-012) |
| 7 | [`docs/sprints/sprint-4-paypal.md`](docs/sprints/sprint-4-paypal.md) | PayPal sandbox integration and webhooks (PAY-013–PAY-015) |
| 8 | [`docs/sprints/sprint-5-refunds-and-integration.md`](docs/sprints/sprint-5-refunds-and-integration.md) | Refunds, reconciliation, business-app notifications, and optional cancellation (PAY-016–PAY-019) |
| 9 | [`docs/sprints/sprint-6-production-readiness.md`](docs/sprints/sprint-6-production-readiness.md) | Observability, security, deployment, documentation, acceptance, release readiness (PAY-020–PAY-023) |
| 10 | [`docs/04-delivery-governance.md`](docs/04-delivery-governance.md) | Sprint dependencies, Definition of Ready/Done, risks, priorities, team practices, refinement questions |

## Architectural baseline

The Payment Module is a **logical module, not a required deployment unit**. It can be hosted inside a modular monolith or run as an independent API/service. Its payment domain and application contracts should remain independent of the consuming business domain, while host adapters, transport, infrastructure composition, and deployment can vary.

The consuming application owns the business transaction (for example, a seat reservation or e-commerce order), decides the authoritative amount, and handles business fulfilment after payment. The Payment Module owns payment processing and tracking, provider integration, idempotency, payment outcomes, and supported refunds.

Keep these three paths distinct:

1. Consuming application → Payment Module: payment initiation and status query.
2. Provider → Payment Module: inbound, authenticated provider webhook.
3. Payment Module → consuming application: payment outcome propagation through an in-process event/handler, message broker, outbound webhook, or status polling as appropriate to the host model.

## Sprint overview

The proposed plan uses a discovery/readiness activity followed by six two-week sprints. Two weeks is a planning assumption, not a delivery commitment; refine and estimate the stories with the delivery team before committing to scope.

| Phase | Goal |
|---|---|
| Preparation | Resolve provider eligibility, credentials, host integration, and architecture decisions |
| Sprint 1 | Create the solution foundation and prove the workflow using a simulated provider |
| Sprint 2 | Add persistence and safe idempotent operations |
| Sprint 3 | Integrate Stripe |
| Sprint 4 | Integrate PayPal |
| Sprint 5 | Complete refund, reconciliation, and outcome propagation workflows |
| Sprint 6 | Complete production hardening and release readiness |

## How to maintain these documents

- Treat the Epic and Scope file as the product boundary and outcome reference.
- Put architectural agreements and deployment-neutral design decisions in the Architecture and Integration file; record lasting decisions as ADRs in the implementation repository.
- Refine each sprint's stories independently. Keep acceptance criteria testable and update dependencies when scope changes.
- Keep delivery policies and cross-sprint risks in the Delivery Governance file rather than duplicating them in every sprint document.
