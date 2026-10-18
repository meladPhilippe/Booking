# META — Agile and Scrum Knowledge Behind This Planning

**Purpose:** Explain the concepts, decisions, and planning practices needed to understand how the Generic Payment Module epic, user stories, acceptance criteria, dependencies, and sprint plan were created.

**Audience:** Anyone reviewing, refining, estimating, delivering, or maintaining this backlog—whether acting as a Business Analyst, Product Owner, Scrum Master, Tech Lead, Developer, QA Engineer, or stakeholder.

**How to use this document:** Read it before reviewing the sprint files if you want to understand not only *what* the backlog contains, but *why* it is structured this way. This is a practical guide for this project, not a replacement for the official Scrum Guide.

---

## 1. The mental model: Agile, Scrum, and project planning are not the same thing

### Agile

Agile is a set of values and principles that favor delivering useful outcomes, collaborating with stakeholders, adapting to learning, and maintaining technical quality. It does not prescribe one universal project plan, fixed sprint length, job title, or user-story format.

For this payment project, Agile thinking means:

- Validate the generic payment contract with a simulated provider before investing heavily in real integrations.
- Deliver usable vertical slices instead of spending several iterations on architecture documents alone.
- Learn from provider sandbox integration and revise estimates or scope when the facts change.
- Treat security, idempotency, webhook handling, and recovery as product quality—not merely as future cleanup.

### Scrum

Scrum is a lightweight framework for addressing complex work through transparency, inspection, and adaptation. The official Scrum Guide describes accountabilities, events, artifacts, and the commitments associated with them.

The Scrum Guide does **not** prescribe epics, user stories, story points, MoSCoW, a Definition of Ready, or a six-sprint release plan. These are common product-management or team practices that can be used alongside Scrum; they should not be confused with mandatory Scrum rules.

### This backlog

The files in this package are a **proposed Agile/Scrum-oriented delivery plan**. They help a team prepare and discuss the work, but they do not replace the team's own refinement, estimates, Sprint Planning, stakeholder feedback, or adaptation during delivery.

---

## 2. The roles and perspectives used to create the plan

The planning used three complementary perspectives. These are useful working responsibilities; they do not map one-to-one to three formal Scrum accountabilities.

### Business Analyst perspective

Focuses on understanding business needs and translating them into clear, testable behavior.

Questions this perspective asks:

- What problem is the payment module solving?
- Who uses it, and what result do they need?
- What is included in the payment module, and what remains the consuming application's responsibility?
- What happens on success, decline, timeout, duplicate request, refund, or delayed confirmation?
- How will stakeholders know whether a story meets its acceptance criteria?

In this backlog, that perspective informed the epic statement, scope boundaries, generic merchant reference, workflows, user stories, acceptance criteria, and discovery questions.

### Scrum Master perspective

Focuses on enabling an effective way of working, transparency, collaboration, and continuous improvement. The Scrum Master is not simply a meeting scheduler or the person who assigns all work.

Questions this perspective asks:

- Is the work understandable enough to discuss and estimate?
- Are external blockers, dependencies, and risks visible?
- Can the team produce a useful, potentially releasable increment and inspect it?
- Are planning assumptions being mistaken for commitments?
- How will the team use review and retrospective feedback to adapt?

In this backlog, that perspective informed sprint goals, readiness questions, dependency visibility, risk tracking, reviewable outcomes, and the recommendation to re-plan using real evidence after early sprints.

### Technical Lead perspective

Focuses on technical feasibility, architecture, integration boundaries, engineering quality, security, and operational reliability.

Questions this perspective asks:

- Which technical dependencies must be resolved before implementation?
- Can business behavior be tested independently of provider SDKs?
- What must happen if the provider succeeds but the application times out?
- Are retries and webhook deliveries safe to repeat?
- Does the proposed design work in a modular monolith as well as an independently hosted API?

In this backlog, that perspective informed the provider-independent core, simulated-provider-first approach, payment state model, persistence and idempotency sequencing, webhook verification, reconciliation, and production-readiness criteria.

### How these perspectives fit with Scrum

Scrum formally defines three accountabilities: **Product Owner, Scrum Master, and Developers**. A Business Analyst or Technical Lead may contribute valuable skills, but those job titles are not extra Scrum accountabilities. One person may perform several skills; the Scrum Team collectively remains responsible for delivering a valuable, usable Increment.

For this project, ensure that someone with clear Product Owner accountability can make priority and scope decisions and that business stakeholders are available to clarify rules.

---

## 3. The planning hierarchy: from outcome to executable work

The backlog uses several levels of detail. They serve different purposes.

| Level | Purpose | Example in this project |
|---|---|---|
| Product outcome / goal | Describes the value the product or module should achieve. | Provide reusable payment processing for multiple business applications. |
| Epic | Groups a substantial business capability or outcome that requires multiple stories. It is a common backlog convention, not a required Scrum artifact. | Deliver a reusable, provider-independent Payment Module. |
| User story / Product Backlog Item (PBI) | Describes a valuable, discussable unit of behavior. Scrum calls the work items Product Backlog items; a user story is one popular format. | Persist and retrieve payment records. |
| Acceptance criteria | State the observable conditions that must be true for the item to be accepted. | Repeating the same idempotency key does not create another logical payment operation. |
| Tasks / implementation notes | Help the team coordinate the work needed to complete a story. | Add a database uniqueness constraint; write concurrency tests. |
| Sprint Goal | Expresses the coherent outcome the Sprint aims to achieve. | Make payment creation durable and retry-safe. |
| Increment | The integrated, verified result produced by the work. | A working flow that persists payment records and safely handles duplicate requests. |

An epic is not necessarily one Sprint's work. A story is not automatically small enough to fit a Sprint just because it is written in story form. The team refines and splits work when necessary.

### Why user stories are phrased from a user or system-consumer perspective

A conventional template is:

> As a **[user or consumer]**, I want **[capability]**, so that **[benefit]**.

This template is a prompt to clarify the need, not a mandatory Scrum format. For platform work, the “user” may be a consuming application or an operator rather than an end customer.

A story should communicate the outcome without dictating every code-level implementation detail. Architecture and technical constraints can be documented separately where they help the team make a sound solution.

---

## 4. What makes acceptance criteria useful?

Acceptance criteria describe observable behavior that can be verified. They should answer, “What evidence would show this requirement has been met?”

For payment software, success-only criteria are insufficient. Criteria should consider relevant alternative and failure paths, including:

- Valid and invalid input.
- Successful payment and customer decline.
- Provider timeout or an indeterminate outcome.
- Repeated or concurrent requests.
- Duplicate and out-of-order webhooks.
- Unsupported provider capabilities.
- Refund limits and invalid state transitions.
- Authorization, privacy, and safe logging.

For example, “Implement idempotency” is too broad by itself. More useful criteria explain the externally visible behavior: the same key and same logical request return or reconcile the existing operation; conflicting reuse of a key is handled deterministically; and concurrent duplicate requests cannot create two logical operations.

Acceptance criteria are not a substitute for the Definition of Done, security standards, or a test strategy. They describe an individual item's expected behavior; the Definition of Done describes the quality required of every completed item or Increment.

---

## 5. Backlog refinement and the Definition of Ready

**Backlog refinement** is ongoing work to make Product Backlog items sufficiently understood, ordered, and sized for future selection. It is not a one-time phase that permanently freezes requirements.

During refinement, the team should:

1. Confirm the user/business outcome and scope.
2. Identify dependencies, assumptions, risks, and external access needs.
3. Clarify acceptance criteria and important failure cases.
4. Split work that is too broad or cannot provide useful feedback.
5. Discuss design options without unnecessarily prescribing every implementation detail.
6. Estimate using the team's agreed approach, if estimation is useful.
7. Reorder the backlog as value, uncertainty, and constraints become clearer.

A **Definition of Ready (DoR)** is an optional team working agreement describing when an item is sufficiently prepared for selection. It is not a formal Scrum requirement and should not become a gate that prevents collaboration or discovery. The DoR in `docs/04-delivery-governance.md` is a proposed checklist for this project.

Readiness is particularly important here because provider eligibility, merchant accounts, currencies, secrets, webhook endpoints, and checkout capabilities may block a story regardless of how well it is written.

---

## 6. The Definition of Done and quality ownership

A **Definition of Done (DoD)** is the shared description of the quality state required for an Increment to be considered done. Unlike the optional DoR, the Scrum Guide explicitly includes the Definition of Done as a commitment associated with the Increment.

For this project, completion should include more than “the code compiles” or “the provider returned HTTP 200.” Depending on the story, evidence may include:

- Reviewed code and passing automated tests.
- Correct domain-state transitions and provider-result mapping.
- Integration tests for persistence or provider mappings.
- Verified and deduplicated webhook processing.
- Idempotent behavior under retries and concurrency.
- Safe configuration, secret handling, and logs.
- Updated public contracts and integration documentation.
- A reproducible demonstration of the acceptance criteria.

The project-specific DoD is documented in `docs/04-delivery-governance.md`. The team should agree on it and improve it as it learns.

---

## 7. How Sprint Planning works

In Scrum, Sprint Planning addresses why the Sprint is valuable, what can be done, and how the selected work will get done. The team establishes a **Sprint Goal**, selects items with the Product Owner through discussion, and creates a plan for delivering the Increment.

For this backlog, each sprint file provides a proposed starting point:

- **Goal:** The outcome that should unite the work.
- **Stories:** Candidate backlog items that contribute to the goal.
- **Acceptance criteria:** Evidence of expected behavior.
- **Dependencies:** Constraints that may affect selection or ordering.
- **Exit outcome/demo:** A practical way to inspect the result.

The listed stories are not an instruction to commit to every item regardless of capacity. During actual Sprint Planning, the Developers assess capacity, uncertainty, the Definition of Done, and the work required to meet the Sprint Goal. They may change the selected scope in collaboration with the Product Owner.

The six two-week sprints in the documents are a **forecasting assumption**, not a Scrum rule or guaranteed delivery date. The team should adjust the plan when discovery, estimates, actual delivery, or provider onboarding produce new evidence.

---

## 8. Why this plan sequences work this way

The order follows risk reduction and end-to-end learning, not merely a list of technical layers.

### Preparation: resolve high-impact unknowns

Check merchant eligibility, countries and currencies, provider account access, required payment methods, host deployment shape, authentication, and the intended capture model. These items could invalidate assumptions or block real integration work.

### Sprint 1: prove the generic contract with a simulated provider

Establish the payment state model, API/application contract, host integration, and a usable end-to-end path. A fake provider makes success, failure, timeout, and other cases deterministic without moving real money.

### Sprint 2: make operations durable before real payment processing

Persistence and idempotency are foundational because a provider might process a payment while the application loses the response. Retrying without a durable operation record and safe retry strategy can lead to duplicate charges.

### Sprint 3 and 4: add real providers behind the established boundaries

Integrate Stripe and PayPal through adapters that map provider-specific concepts to agreed internal payment operations and outcomes. Validate capabilities with sandbox accounts rather than assuming every provider supports every feature in the same way.

### Sprint 5: close lifecycle and cross-system gaps

Refunds, reconciliation, and outcome propagation exercise the system beyond the immediate checkout request. The consuming application still owns its own business decision—for example, whether a reservation should be confirmed after a payment succeeds late.

### Sprint 6: demonstrate operational readiness

Prove security, observability, recovery, documentation, and consumer acceptance. Production readiness must include failure scenarios as well as the happy path.

This sequence is a reasoned initial plan, not a universal template. If discovery shows a different risk order, the Product Backlog and sprint forecast should change.

---

## 9. Dependencies, risks, and vertical slices

### Dependencies

A dependency is something that must be available or resolved before another item can be completed effectively. Dependencies should be made visible because they affect ordering and forecasting.

Examples here include:

- A stable payment contract before provider adapters can be tested against it.
- Persistent operation records and idempotency before production payment initiation.
- Common webhook verification and deduplication rules before complete provider webhook flows.
- Successful/captured payment state before a refund can be processed.
- Merchant sandbox accounts and credentials before real-provider acceptance testing.

A dependency map does not mean every task must be fully completed before anyone can explore or prototype the dependent area. It highlights constraints that the team should manage deliberately.

### Risk-first ordering

Risks are not limited to code complexity. Provider eligibility, legal/commercial setup, security, data correctness, duplicate charges, and unreliable callbacks can all affect delivery. The plan moves high-impact uncertainty earlier when doing so is useful.

### Vertical slices

A vertical slice delivers a small amount of end-to-end behavior across the necessary layers, rather than completing one horizontal layer (for example, all database code) without a usable path through the system.

The simulated payment flow in Sprint 1 is a vertical slice. It should demonstrate that the API/host, application workflow, domain model, and provider abstraction work together. Later slices make that workflow durable and add real providers.

---

## 10. Prioritization and estimation

### Prioritization

The backlog uses **Must / Should / Later** as a practical prioritization aid. This resembles MoSCoW-style prioritization, but the labels are not part of Scrum itself.

- **Must:** Required for the agreed first release to be viable and safe.
- **Should:** Valuable, but may be deferred if capacity is constrained and the release remains acceptable.
- **Later:** Intentionally outside the initial release.

Priorities must be validated with the Product Owner and relevant stakeholders. A feature involving financial correctness or security should not be demoted merely because it is inconvenient; the release boundary may need to change instead.

### Estimation

The current backlog does not contain effort estimates or a team-validated velocity. That is intentional: estimates are context-dependent and should be produced by the people doing the work after refinement.

Teams may use story points, relative sizing, ideal days, or another lightweight approach. Scrum does not mandate story points. Estimate uncertainty as well as the visible coding effort, including tests, reviews, provider account setup, security checks, operational work, and integration delays.

Do not infer that six two-week sprints is achievable solely because six sprint files exist.

---

## 11. The Scrum events and how the backlog supports them

| Event | Purpose | How these documents help |
|---|---|---|
| Sprint Planning | Establish the Sprint Goal and plan the selected work. | Sprint goals, candidate stories, dependencies, and acceptance criteria provide material for the conversation. |
| Daily Scrum | Developers inspect progress toward the Sprint Goal and adapt their plan. | A clear goal and visible blockers make it easier to focus on progress and adjustments. The meeting is not required to be a manager's status report. |
| Sprint Review | Inspect the Increment with stakeholders and discuss adaptations. | The demo/exit outcomes suggest evidence to show, such as a simulated end-to-end flow or a sandbox payment. |
| Sprint Retrospective | Inspect how the team worked and identify improvements. | Delivery risks, testing gaps, provider blockers, rework, and handoffs provide evidence for process improvements. |
| Backlog refinement | Ongoing clarification, ordering, and breakdown of future work. | Epic scope, acceptance criteria, discovery questions, risks, and dependencies give the team a starting point. |

The official Scrum Guide defines the events and their purposes. The table above describes how this project documentation can support them; it is not a substitute for the team's actual conversations.

---

## 12. What must be decided by people—not by this document

This plan contains assumptions that require validation. Before committing to sprint scope, the team and stakeholders should decide:

- Who holds Product Owner accountability and can make timely scope/prioritization decisions?
- Which merchant legal entity, country, currencies, and payment methods will be supported?
- Which Stripe and PayPal account capabilities are available and approved?
- Is immediate capture enough, or is authorization followed by later capture required?
- Which host model will be used first: modular monolith, standalone API, or both?
- How will payment outcomes reach the consuming business workflow in that host model?
- What are the launch requirements for authentication, availability, latency, retention, audit, support, and disaster recovery?
- What team capacity and Definition of Done apply to the first release?

The backlog must change when the answers alter scope, risk, or dependencies.

---

## 13. A practical learning path

For someone who wants enough knowledge to review and participate in this planning, study these topics in order:

1. **Agile mindset:** outcomes, collaboration, feedback, adaptation, and technical quality.
2. **Scrum foundations:** empirical process control; Scrum Team accountabilities; Sprint, events, artifacts, and commitments.
3. **Product backlog management:** goals, ordering, refinement, PBIs, epics as a common convention, and user-story formats.
4. **Requirements quality:** user outcomes, scope boundaries, examples, acceptance criteria, and non-functional requirements.
5. **Delivery planning:** Sprint Goals, selection based on capacity, dependencies, risk, and iterative forecasting.
6. **Engineering quality:** vertical slices, testability, the Definition of Done, automation, security, and operational readiness.
7. **Inspect and adapt:** Sprint Reviews, Retrospectives, and changing the backlog based on evidence.
8. **Payment-domain risks:** idempotency, asynchronous outcomes, webhook trust, state transitions, reconciliation, refunds, and cross-system consistency.

You do not need to memorize every Agile technique before doing useful planning. The essential skill is being able to explain the desired outcome, identify uncertainty, make work testable, expose dependencies, and adapt the plan based on team and stakeholder feedback.

---

## 14. Authoritative starting resources

The resources below are the best starting points for the concepts referenced here.

1. **The Scrum Guide (official current English version listed by Scrum Guides):** <https://scrumguides.org/scrum-guide.html>  
   Read for Scrum's definition, accountabilities, events, artifacts, commitments, and the purpose of the framework. The Scrum Guides site lists the November 2020 English Guide as its current official version.
2. **Scrum Guide download and revision information:** <https://scrumguides.org/download.html>
3. **Manifesto for Agile Software Development:** <https://agilemanifesto.org/>  
   Read for the four values that inform the Agile mindset.
4. **Principles behind the Agile Manifesto:** <https://agilemanifesto.org/principles.html>  
   Read for the principles behind frequent delivery, stakeholder collaboration, technical excellence, simplicity, and adaptation.

Use the Scrum Guide to distinguish formal Scrum elements from optional team practices such as epics, user-story templates, story points, MoSCoW, and Definition of Ready.

---

## 15. How to connect this primer to the backlog files

| When reviewing… | Use this guide to ask… | Then inspect… |
|---|---|---|
| Epic scope | Is the outcome valuable and are boundaries clear? | `docs/01-epic-and-scope.md` |
| Architecture | Are business responsibilities and technical boundaries separated? | `docs/02-architecture-and-integration.md` |
| Unknowns before starting | What could block or invalidate the plan? | `docs/03-discovery-and-readiness.md` |
| A sprint's stories | Is the Sprint Goal coherent, and can the criteria be demonstrated? | `docs/sprints/` |
| Readiness and completion | Is there a shared quality bar, and are risks visible? | `docs/04-delivery-governance.md` |

**Bottom line:** Agile provides the mindset of delivering value and adapting to evidence; Scrum provides a framework for inspection and adaptation; business analysis makes needs and acceptance testable; technical leadership makes the design and risks explicit; and delivery governance helps the team turn that understanding into a realistic, inspectable plan. The backlog is a starting hypothesis to refine and improve—not a promise that the future will unfold exactly as written.
