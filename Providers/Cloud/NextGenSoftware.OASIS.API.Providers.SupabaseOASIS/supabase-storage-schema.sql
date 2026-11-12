-- Apply once to a new dedicated OASIS database with PostgREST's public schema.
-- The Supabase service_role is the only API role granted table access.
BEGIN;

CREATE TABLE public.oasis_avatars (
    id uuid PRIMARY KEY,
    username text NOT NULL,
    email text NOT NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    data_json text NOT NULL
);

CREATE TABLE public.oasis_avatar_details (
    id uuid PRIMARY KEY REFERENCES public.oasis_avatars(id) ON DELETE CASCADE,
    username text NOT NULL,
    email text NOT NULL,
    data_json text NOT NULL
);

CREATE TABLE public.oasis_holons (
    id uuid PRIMARY KEY,
    parent_holon_id uuid,
    holon_type integer NOT NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    data_json text NOT NULL
);

ALTER TABLE public.oasis_avatars ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.oasis_avatar_details ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.oasis_holons ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public.oasis_avatars, public.oasis_avatar_details, public.oasis_holons
    FROM PUBLIC, anon, authenticated;

GRANT SELECT, INSERT, UPDATE, DELETE ON public.oasis_avatars, public.oasis_avatar_details, public.oasis_holons
    TO service_role;

COMMIT;
