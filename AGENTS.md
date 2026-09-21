# Systemcel agent notes

## Delegation

- Keep the primary agent on `gpt-6-astra` with medium reasoning; increase effort only when needed. Keep architecture, ambiguous decisions, security-sensitive work, and final review with the primary agent.
- Delegate bounded repository searches, documentation reading, tests, logs, and simple edits to `astra_worker` (`gpt-5.6-luna`, low reasoning) when useful. Give workers narrow ownership, minimal context, and no permission to spawn more workers. Reuse a worker for follow-ups; prefer one worker unless parallelism materially helps.
- Avoid repeating a worker's verified work. Prefer targeted checks over broad suites when they answer the question.

## UI finish check

- Before completing an interface change, inspect the actual rendered result in light and dark themes and at desktop and narrow mobile widths. Check long lists and scrollbars, hover/focus and selected states, loading/error and sign-in/out transitions, and whether raised elements or borders are clipped.
- Check text and control contrast on every state touched by the change, especially table rows, badges, disabled controls, AI panels, and notification actions. A visible action must be a working link or button with a clear destination; its size and alignment must fit its container.
- Review user-facing AI output for internal IDs or anonymized placeholders. Restore recognizable names locally after the response while preserving the privacy boundary to external models.
- Run the smallest meaningful tests and build checks, then verify the visible behavior. Fix regressions found during this check before handoff.

## CI and production deployment

- On `main`, CI runs lint, typecheck, unit tests, build, the full Playwright browser suite, .NET checks, security checks, and operational smoke tests. When a UI/API response contract changes, update its E2E fixtures in the same change and run the affected browser flow locally before pushing. For release pushes, run the full browser suite once; a targeted pass alone does not reproduce the CI gate.
- Evidence from 2026-09-21: CI run `35545332416` for `5be6e1b` failed at `Run critical browser flows` because the supplier marketplace receipt fixture omitted the new `malKabulYetkisi` field. Commit `cfdcdc2` added it; CI run `35546593597` and Deploy run `35547392309` passed. Earlier CI runs `35388745002` and `35391688558` also failed at the browser-flow step, but their exact assertions were not recovered; inspect their Playwright artifacts before assigning a narrower cause.
- Do not retry CI or push another commit merely to see whether a failure clears. Read the failing step and artifact, reproduce the specific flow locally, fix its cause, then push once. A failed CI skips production deployment by design.
- Production deploy starts only after a successful push CI on the default branch, and it verifies that the candidate SHA is still the branch head. Wait for the current candidate's deploy before another push; Deploy run `35479044663` failed this SHA check. Deploy runs `35394278098` and `35395891766` failed inside the Oracle deploy step, but the public step summaries do not establish why; inspect that step's logs and server status before any retry. Verify the deployed SHA and public smoke result, not only CI success.
