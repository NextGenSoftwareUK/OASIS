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

CREATE FUNCTION public.oasis_read_source_receipt(expected_store uuid, requested_operation uuid)
RETURNS TABLE (store_matches boolean, request_hash text, result_json text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
AS $$
    SELECT i.store_id = expected_store, r.request_hash, r.result_json
    FROM public.oasis_storage_identity i
    LEFT JOIN public.oasis_source_receipts r
        ON i.store_id = expected_store AND r.operation_id = requested_operation
    WHERE i.slot = 1;
$$;
REVOKE ALL ON FUNCTION public.oasis_read_source_receipt(uuid, uuid) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_read_source_receipt(uuid, uuid) TO service_role;

CREATE FUNCTION public.oasis_claim_outbound(
    expected_store uuid, requested_target text, requested_worker text, lease_milliseconds bigint)
RETURNS SETOF public.oasis_outbound_deliveries
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = ''
AS $$
DECLARE
    head public.oasis_outbound_deliveries%ROWTYPE;
    observed_at timestamptz;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.oasis_storage_identity i WHERE i.slot = 1 AND i.store_id = expected_store) THEN
        RAISE EXCEPTION 'Supabase outbound store binding mismatch' USING ERRCODE = 'PT409';
    END IF;
    IF requested_target IS NULL OR length(requested_target) NOT BETWEEN 1 AND 256
        OR requested_worker IS NULL OR length(btrim(requested_worker)) NOT BETWEEN 1 AND 256
        OR length(requested_worker) > 256
        OR lease_milliseconds IS NULL OR lease_milliseconds NOT BETWEEN 1 AND 3600000 THEN
        RAISE EXCEPTION 'Invalid Supabase outbound claim' USING ERRCODE = '22023';
    END IF;

    SELECT d.* INTO head FROM public.oasis_outbound_deliveries d
    WHERE d.target_id = requested_target COLLATE "C" AND d.delivery_state <> 'Completed'
    ORDER BY d.delivery_sequence LIMIT 1 FOR UPDATE;
    observed_at := clock_timestamp();
    IF NOT FOUND OR head.delivery_state = 'ReconciliationRequired'
        OR head.lease_until > observed_at OR head.retry_at > observed_at THEN
        RETURN;
    END IF;

    RETURN QUERY
    UPDATE public.oasis_outbound_deliveries d
    SET delivery_state = 'InFlight', attempt_count = d.attempt_count + 1, worker_id = requested_worker,
        lease_token = gen_random_uuid(), lease_until = observed_at + lease_milliseconds * interval '1 millisecond',
        retry_at = NULL
    WHERE d.delivery_sequence = head.delivery_sequence
    RETURNING d.*;
END;
$$;
REVOKE ALL ON FUNCTION public.oasis_claim_outbound(uuid, text, text, bigint) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_claim_outbound(uuid, text, text, bigint) TO service_role;

CREATE FUNCTION public.oasis_renew_outbound(
    expected_store uuid, requested_sequence bigint, requested_operation uuid, requested_target text,
    requested_worker text, requested_token uuid, lease_milliseconds bigint)
RETURNS boolean
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = ''
AS $$
DECLARE
    observed_at timestamptz;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.oasis_storage_identity i WHERE i.slot = 1 AND i.store_id = expected_store) THEN
        RAISE EXCEPTION 'Supabase outbound store binding mismatch' USING ERRCODE = 'PT409';
    END IF;
    IF lease_milliseconds IS NULL OR lease_milliseconds NOT BETWEEN 1 AND 3600000 THEN
        RAISE EXCEPTION 'Invalid Supabase outbound lease duration' USING ERRCODE = '22023';
    END IF;
    PERFORM 1 FROM public.oasis_outbound_deliveries d WHERE d.delivery_sequence = requested_sequence FOR UPDATE;
    observed_at := clock_timestamp();
    UPDATE public.oasis_outbound_deliveries d
    SET lease_until = greatest(d.lease_until, observed_at + lease_milliseconds * interval '1 millisecond')
    WHERE d.delivery_sequence = requested_sequence AND d.operation_id = requested_operation
        AND d.target_id = requested_target COLLATE "C" AND d.worker_id = requested_worker COLLATE "C"
        AND d.lease_token = requested_token AND d.delivery_state = 'InFlight' AND d.lease_until > observed_at;
    RETURN FOUND;
END;
$$;
REVOKE ALL ON FUNCTION public.oasis_renew_outbound(uuid, bigint, uuid, text, text, uuid, bigint) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_renew_outbound(uuid, bigint, uuid, text, text, uuid, bigint) TO service_role;

CREATE FUNCTION public.oasis_finish_outbound(
    expected_store uuid, requested_sequence bigint, requested_operation uuid, requested_target text,
    requested_worker text, requested_token uuid, next_state text, next_attempt timestamptz,
    failure_code text, failure_message text, recovery text, recovery_hash text)
RETURNS boolean
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = ''
AS $$
DECLARE
    observed_at timestamptz;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.oasis_storage_identity i WHERE i.slot = 1 AND i.store_id = expected_store) THEN
        RAISE EXCEPTION 'Supabase outbound store binding mismatch' USING ERRCODE = 'PT409';
    END IF;
    PERFORM 1 FROM public.oasis_outbound_deliveries d WHERE d.delivery_sequence = requested_sequence FOR UPDATE;
    observed_at := clock_timestamp();
    IF next_state IS NULL OR next_state NOT IN ('Completed', 'Pending', 'ReconciliationRequired')
        OR (next_state = 'Pending' AND (next_attempt IS NULL OR next_attempt <= observed_at))
        OR (next_state <> 'Pending' AND next_attempt IS NOT NULL)
        OR (next_state = 'Completed' AND (failure_code IS NOT NULL OR failure_message IS NOT NULL))
        OR (next_state <> 'Completed' AND COALESCE(length(btrim(failure_code)), 0) = 0)
        OR length(failure_code) > 128 OR length(failure_message) > 4096
        OR ((recovery IS NULL) <> (recovery_hash IS NULL))
        OR length(recovery_hash) <> 64 THEN
        RAISE EXCEPTION 'Invalid Supabase outbound completion' USING ERRCODE = '22023';
    END IF;
    UPDATE public.oasis_outbound_deliveries d
    SET delivery_state = next_state, worker_id = NULL, lease_token = NULL, lease_until = NULL,
        retry_at = next_attempt, error_code = failure_code, error_message = failure_message,
        recovery_json = recovery, recovery_digest = recovery_hash
    WHERE d.delivery_sequence = requested_sequence AND d.operation_id = requested_operation
        AND d.target_id = requested_target COLLATE "C" AND d.worker_id = requested_worker COLLATE "C"
        AND d.lease_token = requested_token AND d.delivery_state = 'InFlight' AND d.lease_until > observed_at;
    RETURN FOUND;
END;
$$;
REVOKE ALL ON FUNCTION public.oasis_finish_outbound(uuid, bigint, uuid, text, text, uuid, text, timestamptz, text, text, text, text)
    FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_finish_outbound(uuid, bigint, uuid, text, text, uuid, text, timestamptz, text, text, text, text)
    TO service_role;

COMMIT;
