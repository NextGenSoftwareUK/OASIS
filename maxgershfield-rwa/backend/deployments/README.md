# Docker Build Guide for Quantum Street Bridge

This document provides full instructions to build, run, and push Docker images for the **Quantum Street Bridge** project, including file structure, ignore rules, and useful commands.

---

## Prerequisites

Ensure the following tools are installed:

- Docker (Desktop or CLI)
- Stable internet connection (for pulling base images)
- Docker Hub account (for pushing the image)

---
### Navigate to Project Root
Before running any Docker commands, navigate to the root of the project:
```sh
cd oasis-bridge
```

### Building the Docker Image
To build the Docker image for the backend service using a specific Dockerfile, use the following command:
```sh
docker build -f backend/deployments/Dockerfile.quantum_exchange -t username/quantum_exchange:vn .
```
### Running the Docker Container
To run the built image locally, use:
```sh
docker run -d -p 8080:80 --name quantum_exchange-backend username/quantum_exchange:vn
```
### Pushing the Image to Docker Hub
Once built and tested, push the image to Docker Hub:
```sh
docker push username/quantum_exchange:vn
```

# Docker Build Guide for Quantum Street Exchange Db Migrator

### Navigate to Project Root

Before running any Docker commands, navigate to the root of the project:

```sh
cd oasis-bridge
```

### Building the Docker Image

To build the Docker image for the backend service using a specific Dockerfile, use the following command:

```sh
docker build -f backend/deployments/Dockerfile.quantum_exchange_migrator_db -t username/quantum_exchange_migrator_db:vn .
```
### Running the Docker Container

To run the built image locally, use:

```sh
docker run -d -p 8080:80 --name quantum_exchange_migrator_db username/quantum_exchange_migrator_db:vn
```

### Pushing the Image to Docker Hub

Once built and tested, push the image to Docker Hub:

```sh
docker push username/quantum_exchange_migrator_db:vn
```
