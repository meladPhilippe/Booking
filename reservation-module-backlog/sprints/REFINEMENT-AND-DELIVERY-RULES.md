# Backlog Refinement, Estimation, and Delivery Rules

## Scrum roles and accountabilities
- **Product Owner:** owns value, ordering, MVP scope, and business decisions such as hold duration, pricing lock, cancellation, and refund rules.
- **Scrum Master:** facilitates refinement, sprint planning, review, and retrospective; removes impediments and protects focus. The Scrum Master does not assign story points or dictate technical solutions.
- **Tech Lead:** guides architecture, technical discovery, quality attributes, risk reduction, and implementation options. The Tech Lead does not unilaterally decide business policy.
- **Developers / QA:** estimate collaboratively, design tests, implement, verify, and adapt the plan to the Sprint Goal.
- **Business Analyst:** clarifies actors, rules, edge cases, acceptance criteria, and traceability between epic outcomes and stories.

One person may cover multiple roles in a small team, but accountabilities should remain explicit.

## Definition of Ready (working checklist)
A story is ready for sprint planning when:
- The user/business outcome is clear.
- Acceptance criteria are testable and not contradictory.
- Dependencies and external owners are identified.
- Relevant policy decisions are made or explicitly bounded.
- Data, API, security, and failure-path implications are understood enough to estimate.
- The story is small enough to complete and demonstrate in a sprint.
- The team can identify a credible testing approach.
- Unknowns requiring a spike are separated from delivery commitments.

“Definition of Ready” is a team working agreement, not a formal Scrum requirement. Do not use it to hide unresolved product decisions indefinitely.

## Definition of Done
A story is Done when:
- Acceptance criteria are met.
- Code is integrated and reviewed.
- Unit and relevant integration/API tests pass.
- Security and authorization implications are checked.
- Persistence migrations/configuration are included when needed.
- Logging and failure handling are adequate for the change.
- Documentation is updated where behavior or operations changed.
- The increment is integrated and usable.

## Estimation
- Estimate collaboratively after refinement.
- Use the team's chosen relative sizing method (e.g., story points) consistently.
- Include testing, review, migration, documentation, and failure handling in the estimate.
- Split stories that combine unrelated outcomes or cannot be demonstrated.
- Do not infer team velocity from this proposed plan.

## Recommended spikes before commitment if uncertainty is high
1. **Atomic hold spike:** compare SQL Server conditional update, optimistic concurrency, and locking approaches using realistic concurrent requests.
2. **Payment lifecycle spike:** document provider event semantics, signature validation, idempotency, retries, and late success.
3. **Inventory ownership spike:** confirm whether inventory is local or external and define source-of-truth boundaries.
4. **Operational spike:** estimate expected inventory size, request rate, and expiration throughput.

A spike should have a timebox and a decision artifact, not just code.

## Sprint events and artifacts
- **Refinement:** clarify and split stories; update acceptance criteria and dependencies.
- **Sprint Planning:** agree Sprint Goal, select work based on capacity, and create a plan.
- **Daily Scrum:** inspect progress toward the Sprint Goal and adapt the plan.
- **Sprint Review:** demonstrate a usable increment and gather stakeholder feedback.
- **Retrospective:** select one or two concrete improvements for the next sprint.
- **Sprint Backlog:** selected stories plus the team's plan to achieve the goal.
- **Increment:** integrated, tested, potentially releasable product capability.

## Risk-first sequencing
The most technically risky requirement is exclusive seat ownership under concurrency. Prove the database guarantee early. Payment late-success handling is the next major business/integration risk. Do not defer either until after most of the user-facing API is built.
