2026-11-19 VERIFIED DEV IMAGE BUILD ACTIVE:44593951627/job135807120822 builds exact integration38d4320ef7cffa3ce8d6c77cb312dee3f42acb9d with existing teaching runtime enabled. Source145 paths match qualified2e996828; Core8cffa72d and pin paired; unchanged Dev workflows/other pins/worker gates preserved; unfinished HTTP tests excluded. Existing Railway sign-in reused through the public API (no CLI reinstall/sign-in); Dev auto-deploy is enabled and current running revision remains18675. Saved DNA hash0755d6b0... unchanged, saved AutoLoadBalanceEnabled false; no running-setting claim. Package verifier corrected to separate inspection scope from image flavor, enabling cql-api checks of the actual teaching-tagged image without retagging/rebuilding or enabling native/API startup. Exactly3 NEW pure Python flavor contracts passed from frozen RAM source; no database/runtime/download/output installed locally. Next exact package qualification, then one normal Development push and paced read-only verification. No PIPER writes or staging/production deployment.

# Development batch —19 November2026

Status: prepared for immutable image qualification; not yet merged or deployed.

David clarified that verified Development rollout is authorized. Heavy MongoDB qualification remains isolated; no PIPER writes or automatic routing/worker setting changes are authorized.

Base Development:18675d6fad4544f1a2ab9b6dc992bffd4ca2ecc4.
Qualified feature checkpoint:80a87f4f3b421b049d5a53e4f27c56a90a494031.
Exact runtime/test source:2e9968288db769f075568c285e315248e656de97.
Core:8cffa72d17439e465e96183186512dfebcc89b97, a descendant of deployed source9e1ee412.

The batch copies only reviewed CQL/COSMIC API/provider/Core changes and their qualified tests, preserving Development workflows, other dependency pins and unrelated worker-gate scripts. It includes the accepted S05 property-entry work and scoped mixed/SQL/grouped distinct support. Current unfinished HTTP fixture changes from1f19ff81 are excluded. No source DNA values are changed.

Evidence includes combined-source44421493104 (WEB4,565 selected+22 configured+3 native), completed S05 acceptance audit, wire11 run44576502179, scoped native13 run44581607449 and primitive3 run44586155910. The latter independently verified22 owned writes/store, four typed domains, all10 flags, four query timings and cleanup; archive14561588731/2918bytes was RAM-only. SQL client12 and mocked Ask2 are qualified separately on the Website branch.

Next: build and qualify the immutable WEB4 image from this exact integrated tree, then normally push the prepared merge to Development and verify the running revision with a few bounded checks. No staging/production promotion. No image/build/runtime/database is downloaded to the laptop.
