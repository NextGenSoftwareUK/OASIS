# OPORTAL account recovery qualification

Actual activation/reset pages incorrectly return to landing `/`; OPORTAL candidate12527afb corrects `/portal.html` and same-host home links, and activation messages use safe text nodes. One new recovery journey uses the existing isolated shipping API/native Mongo with emails explicitly disabled: real forgot form, native reset token read from run-owned Avatar record, real reset page, consumed token, portal redirect, new-password login and old-password refusal. Tokens remain in memory and are never logged or saved.

All three scenario declarations and strict outcome schema extended together; JavaScript/Python syntax checked before CI. No response mocks, secret copies, new local environment/build/download/database, shared Atlas or paid/email requests. Founder activation and email delivery remain separate. Acceptance pending.

## Root failure and candidate

Real46119731964 reached successful forgot-password, then reset returned IsError with a native NullReferenceException; all cleanup assertions passed. Core98952d72 corrects the anonymous global-cache dereference, returns write failures on the actual recovery result, requires saved acknowledgement, issues the one-day token promised by the email, and uses the actual default reset page. Async Forgot now awaits its existing asynchronous save. Public method signatures/declaration line positions are unchanged; no new API route or DNA setting.

OPORTAL8d66cb77 also supplies the existing ReturnUrl field for the current host. The focused browser case now asserts that body and native one-day expiry before reset/new-password login/old-password refusal. A new exact shipping image is required; the failed old image is not relabelled as passing.

46121396024passed exact package inspection/boot but stopped at owned registration; all5fixture cleanup flags and package container absence true. Current Core explicitly returns the verification token when emails are disabled, so no verification policy override is justified from this failure. Added bounded/redacted status/application failure facts at the existing test boundary to identify the actual current-image blocker. Shipping image/source unchanged; no rebuild or production security relaxation.
