# Lumina J5 Bootstrap - OASIS Runtime + Governance Gates

A sophisticated AI governance system built for the OASIS ecosystem, implementing Johnny-5 AI bootstrap with comprehensive governance, truth anchoring, and compliance mechanisms.

## 🚀 Features

### Core Governance
- **Values-Based Decision Making**: OASIS values framework integration
- **Compliance Checking**: Automated policy compliance validation
- **Quorum Management**: Configurable quorum requirements
- **Herz Coherence Monitoring**: Real-time coherence tracking
- **Timelock Mechanisms**: Time-delayed execution for sensitive operations

### Truth Anchoring
- **Blockchain Integration**: Truth chain anchoring for immutable records
- **Policy Decision Anchoring**: Permanent record of governance decisions
- **Transaction Anchoring**: Immutable transaction history

### Service Architecture
- **Microservices**: Modular service handlers
- **Event-Driven**: NATS-based event system
- **Queue Management**: Redis-based job queuing
- **State Management**: PostgreSQL for persistent state
- **Audit Logging**: S3-compatible audit trail

### Security & Identity
- **DID-Based Identity**: Decentralized identifier integration
- **KMS/Vault Integration**: Secure key management
- **Permission System**: Fine-grained access control
- **Attestation Framework**: Cryptographic attestations

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Lumina J5     │    │   Governance    │    │  Truth Anchor   │
│   Bootstrap     │◄──►│    Engine       │◄──►│    Service      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Service       │    │   Event Bus     │    │   Storage       │
│   Handlers      │    │   (NATS)        │    │   (PostgreSQL)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Queue         │    │   Telemetry     │    │   Audit         │
│   (Redis)       │    │   (OTLP)        │    │   (S3/MinIO)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🛠️ Quick Start

### Prerequisites
- Docker & Docker Compose
- Go 1.21+ (for local development)
- Make (optional, for convenience commands)

### Using Docker Compose (Recommended)

1. **Clone and Setup**
   ```bash
   git clone <repository>
   cd lumina-j5-bootstrap
   ```

2. **Start the Stack**
   ```bash
   docker-compose up -d
   ```

3. **Verify Services**
   ```bash
   # Check service health
   curl http://localhost:8080/healthz
   
   # Check readiness
   curl http://localhost:8080/readyz
   ```

4. **Access Services**
   - **Lumina J5 API**: http://localhost:8080
   - **Grafana**: http://localhost:3000 (admin/admin123)
   - **Prometheus**: http://localhost:9090
   - **MinIO**: http://localhost:9001 (minioadmin/minioadmin123)
   - **Vault**: http://localhost:8200 (root token: root)

### Local Development

1. **Install Dependencies**
   ```bash
   cd bootcode
   go mod download
   ```

2. **Start Dependencies**
   ```bash
   # Start only the infrastructure services
   docker-compose up -d postgres redis nats minio vault
   ```

3. **Run the Application**
   ```bash
   go run main.go handlers.go api.go
   ```

## 📋 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `RUNTIME_ENV` | Runtime environment | `prod` |
| `SERVICE_DOMAIN` | Service domain | `lumina.local` |
| `KMS_KEY_ID` | KMS key identifier | Required |
| `VAULT_PATH_SECRETS` | Vault secrets path | `secret/data/j5` |
| `NATS_URL` | NATS connection URL | `nats://nats:4222` |
| `REDIS_URL` | Redis connection URL | `redis:6379` |
| `PG_CONN` | PostgreSQL connection string | Required |
| `S3_AUDIT_BUCKET` | S3 audit bucket name | `lumina-audit` |
| `OTLP_ENDPOINT` | OpenTelemetry endpoint | `http://otel:4317` |

### Governance Configuration

```yaml
governance:
  require_attestations:
    - gov.values-check
    - gov.compliance-check
  quorum:
    min: 0.72
  herz_coherence:
    min: 0.62
    window_seconds: 900
  timelock:
    enabled: true
    seconds: 86400
    applies_to:
      - "econ.treasury.move.amount >= 100000 HERZ"
      - "econ.treasury.move.asset == CASA && amount >= 100000"
```

## 🔌 API Endpoints

### Health & Status
- `GET /healthz` - Liveness probe
- `GET /readyz` - Readiness probe

### Governance
- `POST /api/v1/attestations` - Create attestation
- `GET /api/v1/attestations` - List attestations
- `POST /api/v1/policy-decisions` - Create policy decision
- `GET /api/v1/policy-decisions` - List policy decisions

### Transactions
- `POST /api/v1/transactions` - Propose transaction
- `GET /api/v1/transactions` - List transactions
- `POST /api/v1/transactions/:id/approve` - Approve transaction
- `POST /api/v1/transactions/:id/execute` - Execute transaction

### Herz Coherence
- `GET /api/v1/herz-coherence` - Get coherence metrics
- `POST /api/v1/herz-coherence` - Update coherence metrics

### Services
- `POST /api/v1/services/:name/handle` - Handle service request

## 🔧 Service Handlers

### Values Check Handler
Validates actions against OASIS values framework:
- Love, Wisdom, Truth, Justice, Freedom
- Unity, Harmony, Creativity, Compassion, Integrity

### Compliance Check Handler
Validates compliance with governance policies:
- Quorum requirements
- Timelock mechanisms
- Authority validation
- Herz coherence thresholds

### Execute Handler
Executes approved transactions:
- Treasury moves
- Token minting
- Policy creation
- Event publishing

### Anchor Handler
Performs truth anchoring:
- Policy decision anchoring
- Transaction anchoring
- Blockchain integration

## 📊 Monitoring & Observability

### Metrics
- Governance decision metrics
- Transaction execution metrics
- Herz coherence trends
- Service performance metrics

### Logging
- Structured JSON logging
- Audit trail in S3/MinIO
- Event correlation IDs
- Security event logging

### Tracing
- OpenTelemetry integration
- Distributed tracing
- Performance monitoring
- Error tracking

## 🔐 Security

### Identity Management
- DID-based identity system
- Cryptographic attestations
- Multi-signature support
- Key rotation capabilities

### Access Control
- Role-based permissions
- Policy-based authorization
- Audit logging
- Compliance monitoring

### Data Protection
- Encryption at rest
- Encryption in transit
- Secure key management
- Data integrity verification

## 🚀 Deployment

### Production Deployment

1. **Configure Secrets**
   ```bash
   # Set up Vault
   vault kv put secret/j5 \
     kms_key_id="your-kms-key" \
     pg_conn="your-postgres-conn" \
     s3_audit_bucket="your-audit-bucket"
   ```

2. **Deploy Infrastructure**
   ```bash
   # Deploy to Kubernetes
   kubectl apply -f k8s/
   ```

3. **Configure Monitoring**
   ```bash
   # Set up Prometheus/Grafana
   helm install prometheus prometheus-community/kube-prometheus-stack
   ```

### Scaling Considerations

- **Horizontal Scaling**: Stateless service design
- **Database Scaling**: Read replicas for queries
- **Cache Scaling**: Redis cluster for high availability
- **Message Scaling**: NATS clustering for throughput

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

### Development Guidelines

- Follow Go best practices
- Write comprehensive tests
- Document API changes
- Update configuration examples
- Maintain backward compatibility

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [Wiki](https://github.com/your-org/lumina-j5-bootstrap/wiki)
- **Issues**: [GitHub Issues](https://github.com/your-org/lumina-j5-bootstrap/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/lumina-j5-bootstrap/discussions)
- **Discord**: [OASIS Community](https://discord.gg/oasis)

## 🙏 Acknowledgments

- OASIS Foundation for the governance framework
- The Go community for excellent tooling
- OpenTelemetry for observability standards
- The open-source community for inspiration

---

**Built with ❤️ for the OASIS ecosystem**

