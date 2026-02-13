# Prepare to build OASIS API Docker image

Quick checklist and commands before building the production image.

---

## Pre-flight

- **Docker** running (`docker info` succeeds).
- **OASIS_DNA.json** in `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/` (Solana, MongoDB, etc.). It is copied into the image at build time via the project’s publish output.
- **Build context** is the repo root (the script uses `docker/build.sh` from repo root).

---

## Build (from repo root)

```bash
cd /Users/maxgershfield/OASIS_CLEAN   # or your repo path
./docker/build.sh
```

Optional tag (e.g. for a release):

```bash
./docker/build.sh 4.4.4
# → oasis-api:4.4.4
```

---

## Run locally

```bash
docker run -p 5003:80 oasis-api:latest
```

Then open:

- API: http://localhost:5003
- Swagger: http://localhost:5003/swagger
- Health: http://localhost:5003/api/health

---

## Docker Compose (ONODE + STAR)

From repo root:

```bash
docker-compose -f docker/docker-compose.yml up
```

Or from the `docker` directory:

```bash
cd docker && docker-compose up
```

OASIS API (ONODE) will be on port 5003, STAR API on 50564.

---

## Image details

- **Dockerfile:** `docker/Dockerfile`
- **Context:** repo root (so `COPY . .` in the Dockerfile sees the full repo).
- **Target:** .NET 8.0, published then run with `ASPNETCORE_URLS=http://+:80`.
- **Health:** `GET /api/health` (used in the Dockerfile and in docker-compose for oasis-api).
