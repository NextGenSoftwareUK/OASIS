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

COMMIT;
