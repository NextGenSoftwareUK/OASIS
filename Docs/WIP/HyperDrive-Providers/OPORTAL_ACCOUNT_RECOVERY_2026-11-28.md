# OPORTAL account recovery qualification

Actual activation/reset pages incorrectly return to landing `/`; OPORTAL candidate12527afb corrects `/portal.html` and same-host home links, and activation messages use safe text nodes. One new recovery journey uses the existing isolated shipping API/native Mongo with emails explicitly disabled: real forgot form, native reset token read from run-owned Avatar record, real reset page, consumed token, portal redirect, new-password login and old-password refusal. Tokens remain in memory and are never logged or saved.

All three scenario declarations and strict outcome schema extended together; JavaScript/Python syntax checked before CI. No response mocks, secret copies, new local environment/build/download/database, shared Atlas or paid/email requests. Founder activation and email delivery remain separate. Acceptance pending.
