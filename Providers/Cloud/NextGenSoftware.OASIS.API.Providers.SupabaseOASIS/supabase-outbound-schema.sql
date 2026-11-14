-- Apply after supabase-replication-schema.sql as the database owner.
BEGIN;

CREATE TABLE public.oasis_source_receipts (
    operation_id uuid PRIMARY KEY,
    request_hash text NOT NULL CHECK (length(request_hash) = 64),
    result_json text NOT NULL
);

CREATE TABLE public.oasis_outbound_deliveries (
    delivery_sequence bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    operation_id uuid NOT NULL REFERENCES public.oasis_source_receipts(operation_id),
    target_id text COLLATE "C" NOT NULL CHECK (length(target_id) BETWEEN 1 AND 256),
    payload_json text NOT NULL,
    payload_digest text NOT NULL CHECK (length(payload_digest) = 64),
    delivery_state text NOT NULL DEFAULT 'Pending'
        CHECK (delivery_state IN ('Pending', 'InFlight', 'Completed', 'ReconciliationRequired')),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    worker_id text,
    lease_token uuid,
    lease_until timestamptz,
    retry_at timestamptz,
    error_code text,
    error_message text,
    recovery_json text,
    recovery_digest text,
    UNIQUE (operation_id, target_id),
    CHECK (
        (delivery_state = 'InFlight' AND worker_id IS NOT NULL AND lease_token IS NOT NULL AND lease_until IS NOT NULL)
        OR (delivery_state <> 'InFlight' AND worker_id IS NULL AND lease_token IS NULL AND lease_until IS NULL)
    ),
    CHECK (delivery_state = 'Pending' OR retry_at IS NULL),
    CHECK ((recovery_json IS NULL) = (recovery_digest IS NULL))
);

CREATE INDEX oasis_outbound_pending_route
    ON public.oasis_outbound_deliveries (target_id, delivery_sequence)
    WHERE delivery_state <> 'Completed';

ALTER TABLE public.oasis_source_receipts ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.oasis_outbound_deliveries ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.oasis_source_receipts, public.oasis_outbound_deliveries
    FROM PUBLIC, anon, authenticated, service_role;
REVOKE ALL ON SEQUENCE public.oasis_outbound_deliveries_delivery_sequence_seq
    FROM PUBLIC, anon, authenticated, service_role;

COMMIT;
