# Reservation Module — Agile Planning Pack

## Purpose
This pack defines a delivery-ready backlog for a SOLID-oriented reservation module that supports assigned seats for flights, train journeys, and stadium events.

It is written from four complementary perspectives:
- **Business analysis:** scope, actors, business rules, and acceptance criteria.
- **Scrum Master:** sprint goals, sequencing, dependencies, and review checkpoints.
- **Technical leadership:** architecture boundaries, concurrency, reliability, and quality attributes.
- **Product ownership:** prioritization around demonstrable customer value.

## Initial product decisions and assumptions
1. **MVP scope:** assigned seats only. General admission, rooms, parking, and other non-seat resources are future scope.
2. **Inventory is occurrence-specific:** a seat is reserved for a particular flight, train journey, or stadium event occurrence—not globally.
3. **Architecture:** .NET modular monolith with Domain, Application, Infrastructure, and API projects.
4. **Persistence:** SQL Server is the initial source of truth for inventory ownership and reservations.
5. **Reservation flow:** view availability → acquire temporary hold → initiate payment through an abstraction → process payment outcome → confirm or recover.
6. **Payment provider:** use an adapter/contract or test double initially. Real provider onboarding and credentials are a separate integration decision.
7. **Concurrency:** the database operation that acquires a seat must be atomic. A prior availability read is informational and cannot guarantee a later hold.
8. **Delivery cadence:** four two-week sprints are a planning baseline, not a commitment. The team should estimate stories during refinement and adjust scope based on capacity.
9. **Team assumptions:** one cross-functional delivery team with engineering, QA, product/business analysis, and access to infrastructure support as needed.
10. **No dates or story points are assigned:** these depend on team capacity, Definition of Done, and technical discovery.

## How to use this pack
1. Read `epics/EPIC-RES-001-seat-reservation.md`.
2. Read `sprints/SPRINT-PLAN.md`.
3. Refine stories with the team; validate rules and external dependencies before committing to a sprint.
4. Use story files as the source of truth for acceptance criteria and implementation notes.
5. During sprint planning, select stories based on capacity and dependencies, not simply the order shown here.
6. Keep the backlog outcome-oriented. Split stories if they cannot be completed, tested, and demonstrated within a sprint.

## File structure
- `epics/`: epic outcome, scope, business rules, and success measures.
- `sprints/`: sprint goals, candidate stories, dependencies, and review outcomes.
- `user-stories/`: one Markdown file per story, including acceptance criteria and technical notes.

## Working agreements
- Acceptance criteria describe observable behavior, not just implementation tasks.
- A story is not Done merely because code compiles; tests, API behavior, security, and relevant operational concerns must meet the Definition of Done.
- Prefer domain rules and interfaces that express real variation. Do not create one class per transport type unless behavior actually differs.
- Treat payment success, reservation confirmation, and notification delivery as distinct facts.
- Use UTC instants for hold expiration and an injectable clock in tests.
- Never rely on a cache as the sole authority for confirmed seat ownership.
