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

COMMIT;
