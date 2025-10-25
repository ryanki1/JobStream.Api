# Docker Deployment Guide

## 🏗️ Microservices Architecture

JobStream uses a containerized microservices architecture:

```
┌─────────────────────────────────────────────────────────┐
│                   Docker Compose Stack                   │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   API        │  │  PostgreSQL  │  │    Redis     │  │
│  │  (.NET 7)    │  │   Database   │  │   (Cache)    │  │
│  │  Port 5001   │  │  Port 5432   │  │  Port 6379   │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│         │                  │                  │           │
│         └──────────────────┴──────────────────┘          │
│                    jobstream-network                      │
└─────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- Docker Desktop installed
- Docker Compose v2.0+

### 1. Generate Encryption Keys

First, generate your encryption keys:

```bash
# Run the app locally once to generate keys
dotnet run

# Copy the keys from the console output:
# [Warning] Encryption:Key = abc123...
# [Warning] Encryption:IV = def456...
```

### 2. Create Environment File

```bash
# Copy example file
cp .env.example .env

# Edit .env and add your keys
nano .env
```

Update these values:
```env
DB_PASSWORD=YourSecurePassword123!
ENCRYPTION_KEY=<key from step 1>
ENCRYPTION_IV=<iv from step 1>
```

### 3. Start All Services

```bash
# Build and start all containers
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps
```

### 4. Access the API

- **API Base URL:** http://localhost:5001
- **Health Check:** http://localhost:5001/api/health
- **Swagger (if enabled):** http://localhost:5001/swagger

### 5. Test the API

```bash
# Health check
curl http://localhost:5001/api/health

# Start registration
curl -X POST http://localhost:5001/api/company/register/start \
  -H "Content-Type: application/json" \
  -d '{
    "companyEmail": "test@company.com",
    "primaryContactName": "John Doe"
  }'
```

## 📦 Container Details

### API Container
- **Image:** Custom .NET 7 image
- **Port:** 5001 (host) → 80 (container)
- **Volumes:** `./uploads` for file storage
- **Dependencies:** PostgreSQL, Redis

### Database Container
- **Image:** postgres:15-alpine
- **Port:** 5432
- **Volume:** `postgres-data` (persistent storage)
- **Credentials:** See `.env` file

### Redis Container
- **Image:** redis:7-alpine
- **Port:** 6379
- **Volume:** `redis-data` (persistent storage)
- **Purpose:** Caching, future blockchain event queue

## 🛠️ Development Mode

For development with hot reload:

```bash
# Use development compose file
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# API will run on port 5252 in development
# Swagger will be accessible
```

## 📊 Monitoring & Management

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f database
docker-compose logs -f redis
```

### Access Database

```bash
# Connect to PostgreSQL
docker-compose exec database psql -U jobstream_user -d jobstream

# Run queries
SELECT * FROM "CompanyRegistrations";
\q  # Exit
```

### Access Redis

```bash
# Connect to Redis CLI
docker-compose exec redis redis-cli

# Test Redis
PING
SET test "hello"
GET test
```

### Restart Services

```bash
# Restart all
docker-compose restart

# Restart specific service
docker-compose restart api
```

### Stop Services

```bash
# Stop (keeps data)
docker-compose stop

# Stop and remove containers (keeps data)
docker-compose down

# Stop and remove everything including data
docker-compose down -v
```

## 🔄 Database Migrations

When you update models:

```bash
# Stop the API
docker-compose stop api

# Run migrations
dotnet ef database update

# Or rebuild the container
docker-compose up -d --build api
```

## 🌍 Production Deployment

### Railway

```bash
# Railway automatically detects docker-compose.yml
railway up
```

### Azure Container Instances

```bash
# Build and push to Azure Container Registry
az acr build --registry myregistry --image jobstream-api:latest .

# Deploy
az container create \
  --resource-group myResourceGroup \
  --name jobstream-api \
  --image myregistry.azurecr.io/jobstream-api:latest \
  --ports 80
```

### AWS ECS (Elastic Container Service)

```bash
# Build and push to ECR
aws ecr get-login-password | docker login --username AWS --password-stdin <account>.dkr.ecr.us-east-1.amazonaws.com
docker build -t jobstream-api .
docker tag jobstream-api:latest <account>.dkr.ecr.us-east-1.amazonaws.com/jobstream-api:latest
docker push <account>.dkr.ecr.us-east-1.amazonaws.com/jobstream-api:latest

# Deploy using ECS CLI or console
```

## 🔐 Security Best Practices

1. **Never commit `.env` file** - It's in `.gitignore`
2. **Use strong passwords** in production
3. **Rotate encryption keys** periodically
4. **Use secrets management** in production (Azure Key Vault, AWS Secrets Manager)
5. **Enable HTTPS** in production (use reverse proxy like Nginx)

## 🐛 Troubleshooting

### Container won't start

```bash
# Check logs
docker-compose logs api

# Check if port is in use
lsof -i :5001

# Remove and rebuild
docker-compose down
docker-compose up --build
```

### Database connection errors

```bash
# Check database is healthy
docker-compose ps

# Verify connection string in logs
docker-compose logs api | grep "Connection"

# Test database connectivity
docker-compose exec database pg_isready -U jobstream_user
```

### Volume permission errors

```bash
# Fix upload directory permissions
chmod 777 uploads/

# Or use Docker volume
# Already configured in docker-compose.yml
```

## 🚀 Future: Blockchain Microservices

When adding IPFS and Zebec, the stack will expand:

```yaml
services:
  # ... existing services ...

  ipfs:
    image: ipfs/go-ipfs:latest
    ports:
      - "5001:5001"
      - "8080:8080"
    volumes:
      - ipfs-data:/data/ipfs

  solana-listener:
    build: ./services/blockchain-listener
    environment:
      - SOLANA_RPC_URL=${SOLANA_RPC_URL}
    depends_on:
      - redis
```

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)

---

**Current Status:** ✅ Microservices architecture ready for local development and cloud deployment
