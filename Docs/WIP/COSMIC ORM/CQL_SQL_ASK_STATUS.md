# CQL SQL-like and free-text endpoints (roadmap phase 2a)

Branch `work/cql-sql-ask`. Written 26 Nov 2026.

## Status

**6 / 6 task items done.** Not yet verified on a deployed server.

| # | Item | State |
|---|---|---|
| 1 | `POST api/data/query-cql/sql` and `/ask` in WEB4 (Jint 4.4.1, embedded parser and prompt, middleware, `X-OASIS-CQL-Parse-Ms`) | done; compiler, prompt, path-rewrite and CORS tests 35/35 |
| 2 | WEB4 Postman collection, with example bodies | done (Development only; master has no such routes yet) |
| 3 | JavaScript SDK `oasis.data.sql` / `oasis.data.ask` | done, 10/10 tests, main `c3ace9c` |
| 4 | STAR CLI `cql sql` / `cql ask [--no-run]` | done, 7/7 tests, STAR-ODK Development `b8e4f78` |
| 5 | Docs site, Learn site, `Docs/Devs/CQL_WEB4_AND_STAR_CLI.md` | done (sites: main `11153de`, `71def41`) |
| 6 | Sandbox page calls the server only (Web4Site; `cql-text.js` and the browser prompt removed) | done; server adds `execute:false`, example `text` and ask mapping/example options |

Not covered by tests: the controller dispatch to the typed endpoints over real HTTP with usage admission, and a live
WEB6 draft.

The parent gitlink for `STAR ODK` and the Railway manifest pin are not advanced, so nothing in WEB5 redeploys. The
CLI change reaches the parent checkout with the next `STAR ODK` pin update.

## Design note

Usage admission is looked up by request path and CQL tariffs and attempt budgets differ per path. A `/sql` route
would have been refused with `CQL_ADMISSION_REQUIRED`. `CqlTextMiddleware` therefore compiles before admission and
gives the request the compiled query's path. No Railway or admission configuration change is needed.

## Left to do after this branch

- Check both endpoints on the deployed Development API (not verified live; no live WEB6 call has been made).
- Run the sandbox page's preview, run and ask buttons in a signed-in demo session (not clicked through yet).
- Text and ask on the page work against the Development API; the Production option needs these routes promoted to master.
