-- Apply to an existing OASIS storage database before enrolling it for replication.
BEGIN;

CREATE TABLE public.oasis_storage_identity (
    slot integer PRIMARY KEY CHECK (slot = 1),
    store_id uuid NOT NULL CHECK (store_id <> '00000000-0000-0000-0000-000000000000')
);
INSERT INTO public.oasis_storage_identity (slot, store_id) VALUES (1, gen_random_uuid());
ALTER TABLE public.oasis_storage_identity ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.oasis_storage_identity FROM PUBLIC, anon, authenticated, service_role;
GRANT SELECT ON public.oasis_storage_identity TO service_role;

CREATE TABLE public.oasis_replica_receipts (
    operation_id uuid PRIMARY KEY,
    request_hash text NOT NULL CHECK (length(request_hash) = 64),
    result_json text NOT NULL
);
ALTER TABLE public.oasis_replica_receipts ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.oasis_replica_receipts FROM PUBLIC, anon, authenticated, service_role;
GRANT SELECT ON public.oasis_replica_receipts TO service_role;

CREATE FUNCTION public.oasis_read_replica_receipt(expected_store uuid, requested_operation uuid)
RETURNS TABLE (store_matches boolean, request_hash text, result_json text)
LANGUAGE sql STABLE SECURITY INVOKER SET search_path = ''
AS $$
    SELECT i.store_id = expected_store, r.request_hash, r.result_json
    FROM public.oasis_storage_identity i
    LEFT JOIN public.oasis_replica_receipts r
        ON i.store_id = expected_store AND r.operation_id = requested_operation
    WHERE i.slot = 1;
$$;
REVOKE ALL ON FUNCTION public.oasis_read_replica_receipt(uuid, uuid) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_read_replica_receipt(uuid, uuid) TO service_role;

CREATE TABLE public.oasis_replica_heads (
    id uuid PRIMARY KEY,
    source_id text NOT NULL,
    source_authority text NOT NULL,
    snapshot_json text NOT NULL,
    snapshot_digest text NOT NULL CHECK (length(snapshot_digest) = 64),
    revision_id uuid NOT NULL,
    hard_deleted boolean NOT NULL
);
ALTER TABLE public.oasis_replica_heads ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.oasis_replica_heads FROM PUBLIC, anon, authenticated, service_role;
GRANT SELECT ON public.oasis_replica_heads TO service_role;

-- Return one consistent observation per requested key, including keys that are absent.
-- The replica writer will compare the observed rows before mutating any graph data.
CREATE FUNCTION public.oasis_read_replica_state(expected_store uuid, requested_ids uuid[])
RETURNS TABLE (store_matches boolean, id uuid, current_row jsonb, replica_head jsonb)
LANGUAGE sql STABLE SECURITY INVOKER SET search_path = ''
AS $$
    SELECT i.store_id = expected_store, keys.id, to_jsonb(h), to_jsonb(r)
    FROM public.oasis_storage_identity i
    CROSS JOIN unnest(requested_ids) AS keys(id)
    LEFT JOIN public.oasis_holons h ON i.store_id = expected_store AND h.id = keys.id
    LEFT JOIN public.oasis_replica_heads r ON i.store_id = expected_store AND r.id = keys.id
    WHERE i.slot = 1;
$$;
REVOKE ALL ON FUNCTION public.oasis_read_replica_state(uuid, uuid[]) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_read_replica_state(uuid, uuid[]) TO service_role;

-- Commit a validated replica plan only while its complete native observations still hold.
CREATE FUNCTION public.oasis_commit_replica(
    expected_store uuid, requested_operation uuid, expected_hash text,
    receipt_json text, mutations jsonb)
RETURNS TABLE (outcome text, committed_json text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = ''
AS $$
DECLARE
    original public.oasis_replica_receipts%ROWTYPE;
    item jsonb;
    record_id uuid;
    observed_current jsonb;
    observed_head jsonb;
    next_current public.oasis_holons%ROWTYPE;
    next_head public.oasis_replica_heads%ROWTYPE;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.oasis_storage_identity i WHERE i.slot = 1 AND i.store_id = expected_store) THEN
        RETURN QUERY SELECT 'binding_mismatch'::text, NULL::text;
        RETURN;
    END IF;
    IF requested_operation IS NULL OR expected_hash IS NULL OR length(expected_hash) <> 64
        OR receipt_json IS NULL OR jsonb_typeof(mutations) IS DISTINCT FROM 'array'
        OR jsonb_array_length(mutations) = 0 THEN
        RAISE EXCEPTION 'Invalid Supabase replica commit input' USING ERRCODE = '22023';
    END IF;

    -- Include ordinary PostgREST writes in the comparison fence, including absent keys.
    LOCK TABLE public.oasis_holons IN SHARE ROW EXCLUSIVE MODE;
    SELECT r.* INTO original FROM public.oasis_replica_receipts r WHERE r.operation_id = requested_operation;
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
        RAISE EXCEPTION 'Supabase replica keys must be distinct and nonnull' USING ERRCODE = '22023';
    END IF;
    FOR item IN SELECT value FROM jsonb_array_elements(mutations) LOOP
        record_id := (item->>'id')::uuid;
        SELECT to_jsonb(h) INTO observed_current FROM public.oasis_holons h WHERE h.id = record_id;
        SELECT to_jsonb(r) INTO observed_head FROM public.oasis_replica_heads r WHERE r.id = record_id;
        IF NOT (item ?& ARRAY['expected_current','expected_head','current','head']) THEN
            RAISE EXCEPTION 'Incomplete Supabase replica mutation' USING ERRCODE = '22023';
        END IF;
        IF observed_current IS DISTINCT FROM NULLIF(item->'expected_current', 'null'::jsonb)
            OR observed_head IS DISTINCT FROM NULLIF(item->'expected_head', 'null'::jsonb) THEN
            RETURN QUERY SELECT 'state_conflict'::text, NULL::text;
            RETURN;
        END IF;
    END LOOP;

    FOR item IN SELECT value FROM jsonb_array_elements(mutations) LOOP
        record_id := (item->>'id')::uuid;
        next_head := jsonb_populate_record(NULL::public.oasis_replica_heads, item->'head');
        IF next_head.id IS DISTINCT FROM record_id
            OR next_head.hard_deleted IS DISTINCT FROM (item->'current' = 'null'::jsonb) THEN
            RAISE EXCEPTION 'Invalid Supabase replica head' USING ERRCODE = '22023';
        END IF;
        IF next_head.hard_deleted THEN
            DELETE FROM public.oasis_holons h WHERE h.id = record_id;
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Supabase replica hard delete requires a current row' USING ERRCODE = '22023';
            END IF;
        ELSE
            next_current := jsonb_populate_record(NULL::public.oasis_holons, item->'current');
            IF next_current.id IS DISTINCT FROM record_id THEN
                RAISE EXCEPTION 'Invalid Supabase replica current row' USING ERRCODE = '22023';
            END IF;
            INSERT INTO public.oasis_holons (id,parent_holon_id,holon_type,is_deleted,data_json)
            VALUES (record_id,next_current.parent_holon_id,next_current.holon_type,next_current.is_deleted,next_current.data_json)
            ON CONFLICT (id) DO UPDATE SET parent_holon_id=EXCLUDED.parent_holon_id,
                holon_type=EXCLUDED.holon_type,is_deleted=EXCLUDED.is_deleted,data_json=EXCLUDED.data_json;
        END IF;
        INSERT INTO public.oasis_replica_heads VALUES (record_id,next_head.source_id,next_head.source_authority,
            next_head.snapshot_json,next_head.snapshot_digest,next_head.revision_id,next_head.hard_deleted)
        ON CONFLICT (id) DO UPDATE SET source_id=EXCLUDED.source_id,source_authority=EXCLUDED.source_authority,
            snapshot_json=EXCLUDED.snapshot_json,snapshot_digest=EXCLUDED.snapshot_digest,
            revision_id=EXCLUDED.revision_id,hard_deleted=EXCLUDED.hard_deleted;
    END LOOP;
    INSERT INTO public.oasis_replica_receipts VALUES (requested_operation,expected_hash,receipt_json);
    RETURN QUERY SELECT 'committed'::text, receipt_json;
END;
$$;
REVOKE ALL ON FUNCTION public.oasis_commit_replica(uuid, uuid, text, text, jsonb) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.oasis_commit_replica(uuid, uuid, text, text, jsonb) TO service_role;

COMMIT;
