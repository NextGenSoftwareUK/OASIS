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

CREATE FUNCTION public.oasis_commit_source(
    expected_store uuid, requested_operation uuid, expected_hash text,
    receipt_json text, mutations jsonb, deliveries jsonb)
RETURNS TABLE (outcome text, committed_json text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = ''
AS $$
DECLARE
    original public.oasis_source_receipts%ROWTYPE;
    item jsonb;
    record_id uuid;
    observed jsonb;
    next_current public.oasis_holons%ROWTYPE;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.oasis_storage_identity i WHERE i.slot = 1 AND i.store_id = expected_store) THEN
        RETURN QUERY SELECT 'binding_mismatch'::text, NULL::text;
        RETURN;
    END IF;
    IF requested_operation IS NULL OR expected_hash IS NULL OR length(expected_hash) <> 64
        OR receipt_json IS NULL OR jsonb_typeof(mutations) IS DISTINCT FROM 'array'
        OR jsonb_array_length(mutations) = 0 OR jsonb_typeof(deliveries) IS DISTINCT FROM 'array' THEN
        RAISE EXCEPTION 'Invalid Supabase source commit input' USING ERRCODE = '22023';
    END IF;

    -- Serialize source commits and include ordinary PostgREST writes in the state comparison.
    LOCK TABLE public.oasis_holons IN SHARE ROW EXCLUSIVE MODE;
    SELECT r.* INTO original FROM public.oasis_source_receipts r WHERE r.operation_id = requested_operation;
    IF FOUND THEN
        IF original.request_hash <> expected_hash THEN
            RETURN QUERY SELECT 'operation_reused'::text, NULL::text;
        ELSE
            RETURN QUERY SELECT 'committed'::text, original.result_json;
        END IF;
        RETURN;
    END IF;
    IF (SELECT count(DISTINCT (value->>'id')::uuid) FROM jsonb_array_elements(mutations))
        <> jsonb_array_length(mutations) THEN
        RAISE EXCEPTION 'Supabase source keys must be distinct and nonnull' USING ERRCODE = '22023';
    END IF;
    FOR item IN SELECT value FROM jsonb_array_elements(mutations) LOOP
        record_id := (item->>'id')::uuid;
        IF NOT (item ?& ARRAY['expected_current','current']) THEN
            RAISE EXCEPTION 'Incomplete Supabase source mutation' USING ERRCODE = '22023';
        END IF;
        SELECT to_jsonb(h) INTO observed FROM public.oasis_holons h WHERE h.id = record_id;
        IF observed IS DISTINCT FROM NULLIF(item->'expected_current', 'null'::jsonb)
            OR EXISTS (SELECT 1 FROM public.oasis_replica_heads r WHERE r.id = record_id) THEN
            RETURN QUERY SELECT 'state_conflict'::text, NULL::text;
            RETURN;
        END IF;
    END LOOP;
    FOR item IN SELECT value FROM jsonb_array_elements(mutations) LOOP
        record_id := (item->>'id')::uuid;
        IF item->'current' = 'null'::jsonb THEN
            DELETE FROM public.oasis_holons h WHERE h.id = record_id;
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Supabase source hard delete requires a current row' USING ERRCODE = '22023';
            END IF;
        ELSE
            next_current := jsonb_populate_record(NULL::public.oasis_holons, item->'current');
            IF next_current.id IS DISTINCT FROM record_id THEN
                RAISE EXCEPTION 'Invalid Supabase source current row' USING ERRCODE = '22023';
            END IF;
            INSERT INTO public.oasis_holons (id,parent_holon_id,holon_type,is_deleted,data_json)
            VALUES (record_id,next_current.parent_holon_id,next_current.holon_type,next_current.is_deleted,next_current.data_json)
            ON CONFLICT (id) DO UPDATE SET parent_holon_id=EXCLUDED.parent_holon_id,
                holon_type=EXCLUDED.holon_type,is_deleted=EXCLUDED.is_deleted,data_json=EXCLUDED.data_json;
        END IF;
    END LOOP;
    INSERT INTO public.oasis_source_receipts VALUES (requested_operation,expected_hash,receipt_json);
    INSERT INTO public.oasis_outbound_deliveries (operation_id,target_id,payload_json,payload_digest)
    SELECT requested_operation, value->>'target_id', value->>'payload_json', value->>'payload_digest'
    FROM jsonb_array_elements(deliveries)
    ORDER BY (value->>'target_id') COLLATE "C";
    RETURN QUERY SELECT 'committed'::text, receipt_json;
END;
$$;
REVOKE ALL ON FUNCTION public.oasis_commit_source(uuid, uuid, text, text, jsonb, jsonb) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_commit_source(uuid, uuid, text, text, jsonb, jsonb) TO service_role;

COMMIT;
