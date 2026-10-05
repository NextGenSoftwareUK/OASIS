# Brief 04 – Backend Hardening & Payments

## Objective
Elevate `ride-scheduler-be` from prototype to demo-ready backend by fully implementing payment recording, enforcing validation/authorization, and improving observability/documentation.

## Current State
- Ride lifecycle handled in `services/rideService.js` with stubs for `recordPayment`.
- Booking schema includes `payment`, `timeline`, `cancellation`, but payment fields are not guaranteed to be populated.
- Twilio SMS disabled/stubbed; SendGrid/Cloudinary placeholders.
- No centralized error handling or rate limiting; limited logging.

## Deliverables
1. **Payment recording**
   - Flesh out `recordPayment` to accept method (wallet, cash, card), transaction reference, amount, and status transitions.
   - Integrate with OASIS Wallet API (mock call if credentials not ready) to credit driver on completion and debit passenger or sponsor wallet.
   - Add `/api/bookings/:id/payment` validation (schema, status checks).
2. **Lifecycle guards**
   - Ensure status transitions obey allowed graph (pending → accepted → started → completed).
   - Enforce driver ownership when calling accept/start/complete.
   - Add booking OTP verification hooks if required.
3. **Input validation & security**
   - Use middleware (e.g., celebrate/Joi) to validate request bodies/params for booking + driver routes.
   - Add rate limiting (per IP) for sensitive endpoints.
   - Audit logging (Mongo collection `audit_logs`) for payment + status changes with actor info.
4. **Observability**
   - Structured logging (winston/pino) with correlation IDs.
   - Health endpoint summarizing Mongo status, pending bookings, payment queue depth.
5. **Docs & scripts**
   - Update `README.md` with new env vars (wallet API, rate limit settings), payment workflow, and troubleshooting guide.
   - Expand `scripts/seedDatabase.js` to create sample paid/unpaid bookings for testing.
   - Add Postman collection or REST Client file demonstrating payment-related endpoints.

## Acceptance Criteria
- Payment endpoints return deterministic responses, update Mongo documents, and (when wallet API mocked) emit ledger events.
- Unauthorized or invalid transitions are rejected with descriptive errors and logged.
- Logs + audit entries confirm who performed each payment action.
- Documentation enables another engineer to configure and test payments within 45 minutes.

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Files: `rideService.js`, `bookingController.js`, `bookingRoutes.js`, `models/bookingModal.js`.
- External: OASIS Wallet API specs (see `ONODE` docs).***

