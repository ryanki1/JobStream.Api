# Port Configuration Reference

## 🔌 Port Mapping Guide

Understanding where your API is accessible in different environments.

---

## Local Development (Without Docker)

When you run:
```bash
dotnet run
```

**Configuration:** `Properties/launchSettings.json`
```json
"applicationUrl": "https://localhost:7088;http://localhost:5252"
```

**Access Points:**
- API Base: http://localhost:5252
- Swagger: http://localhost:5252/swagger
- HTTPS: https://localhost:7088

**VS Code Debugger:** Uses HTTP profile (port 5252)

---

## Local Development (With Docker)

When you run:
```bash
docker-compose up
```

**Configuration:** `docker-compose.yml`
```yaml
ports:
  - "5001:80"  # host_port:container_port
```

**How it works:**
- **Inside container:** API listens on port **80**
- **On your machine:** Exposed as port **5001**
- **Mapping:** `localhost:5001` → `container:80`

**Access Points:**
- API Base: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health Check: http://localhost:5001/api/health

**Note:** Port 5001 is used instead of 5000 because macOS uses port 5000 for AirPlay Receiver.

**Important:**
- Port 5252 is **NOT** used in Docker
- The container always uses port 80 internally
- Docker maps it to whatever host port you specify

---

## Cloud Deployment (Railway)

When you deploy to Railway:

**No localhost!** Railway assigns a public URL.

**How Railway works:**
```
Container runs on port 80 internally
         ↓
Railway's internal proxy
         ↓
HTTPS proxy (automatically adds SSL)
         ↓
Public URL: https://jobstream-api-production.up.railway.app
```

**Railway automatically:**
- ✅ Detects your container exposes port 80
- ✅ Adds HTTPS/SSL certificate
- ✅ Gives you a public domain
- ✅ Handles load balancing

**Access Points:**
- API Base: https://jobstream-api-production.up.railway.app
- Swagger: https://jobstream-api-production.up.railway.app/swagger
- Health: https://jobstream-api-production.up.railway.app/api/health

**Port Configuration:**
- You **don't specify** external ports
- Railway reads your Dockerfile's `EXPOSE` directive
- Or it auto-detects the port your app listens on

**Environment Variable (optional):**
```yaml
environment:
  PORT: 80  # Railway may set this automatically
```

---

## Cloud Deployment (Azure App Service)

When you deploy to Azure:

**Configuration:** Automatic or via Azure Portal

**How Azure works:**
```
Container runs on port 80 internally
         ↓
Azure App Service proxy
         ↓
HTTPS (Azure-managed SSL)
         ↓
Public URL: https://jobstream-api.azurewebsites.net
```

**Azure automatically:**
- ✅ Detects container port (reads EXPOSE in Dockerfile)
- ✅ Provides HTTPS with Azure SSL certificate
- ✅ Assigns *.azurewebsites.net domain
- ✅ Can map custom domains

**Access Points:**
- API Base: https://jobstream-api.azurewebsites.net
- Swagger: https://jobstream-api.azurewebsites.net/swagger
- Health: https://jobstream-api.azurewebsites.net/api/health

**Port Configuration:**
```bash
# Azure CLI deployment
az webapp config appsettings set \
  --settings WEBSITES_PORT=80
```

Or in Azure Portal:
- Configuration → Application Settings
- Add: `WEBSITES_PORT = 80`

---

## Cloud Deployment (AWS ECS)

**Configuration:** Task Definition

```json
{
  "portMappings": [
    {
      "containerPort": 80,
      "hostPort": 80,
      "protocol": "tcp"
    }
  ]
}
```

**Access via Load Balancer:**
- Public URL: https://your-load-balancer.amazonaws.com
- Swagger: https://your-load-balancer.amazonaws.com/swagger

---

## Summary Table

| Environment | Internal Port | External Access | Swagger URL |
|-------------|---------------|-----------------|-------------|
| `dotnet run` | 5252 | localhost:5252 | http://localhost:5252/swagger |
| `docker-compose` | 80 | localhost:5001 | http://localhost:5001/swagger |
| Railway | 80 | yourapp.railway.app | https://yourapp.railway.app/swagger |
| Azure | 80 | yourapp.azurewebsites.net | https://yourapp.azurewebsites.net/swagger |
| AWS ECS | 80 | load-balancer.amazonaws.com | https://load-balancer.amazonaws.com/swagger |

---

## Key Concepts

### **Container Port vs Host Port**

```yaml
ports:
  - "5001:80"
    #  ↑    ↑
    #  │    └─ Port inside container (where app listens)
    #  └────── Port on host machine (how you access it)
```

**Example:**
```yaml
ports:
  - "5001:80"   # Access: localhost:5001 → container:80
  - "5252:80"   # Access: localhost:5252 → container:80
  - "8080:80"   # Access: localhost:8080 → container:80
```

All three would work! The container always listens on 80, you just change the host port.

### **Cloud Platforms Don't Use localhost**

On Railway/Azure/AWS:
- ❌ No "localhost"
- ❌ No port mapping like `5000:80`
- ✅ Cloud proxy handles everything
- ✅ You get a public URL
- ✅ HTTPS is automatic

### **How Cloud Detects Your Port**

1. **Dockerfile EXPOSE:**
   ```dockerfile
   EXPOSE 80
   ```

2. **Environment Variable:**
   ```dockerfile
   ENV ASPNETCORE_URLS=http://+:80
   ```

3. **Platform Auto-detection:**
   - Railway/Azure scan your running container
   - Detect which port is listening
   - Route traffic to that port

---

## Troubleshooting

### "Swagger not found on localhost:5252 with Docker"

**Problem:** You're using the wrong port!

**Solution:**
```bash
# Docker uses port 5001, not 5252
http://localhost:5001/swagger  ✅
http://localhost:5252/swagger  ❌
```

### "Cloud deployment not accessible"

**Check:**
1. Container is listening on port 80
2. `ASPNETCORE_URLS=http://+:80` is set
3. Cloud platform detected the port (check logs)
4. Firewall/security groups allow traffic

### "Want to use port 5252 in Docker"

Edit `docker-compose.yml`:
```yaml
ports:
  - "5252:80"  # Now accessible on localhost:5252
```

---

## Best Practice Recommendations

### **Development:**
- Use `dotnet run` for debugging (port 5252)
- Use `docker-compose up` for testing full stack (port 5001)

### **Production:**
- Always use port 80 inside containers
- Let cloud platform handle external routing
- Use HTTPS (cloud providers add this automatically)

### **Configuration:**
```dockerfile
# In Dockerfile - standard port
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
```

```yaml
# In docker-compose.yml - developer's choice
ports:
  - "5001:80"  # or "5252:80" or "8080:80"
```

---

## FAQ

**Q: Why does Railway ignore my port mapping?**
A: Railway doesn't use your `docker-compose.yml` ports section. It only reads the container's internal port.

**Q: Can I use HTTPS in Docker locally?**
A: Yes, but you need to generate SSL certificates. Use a reverse proxy like nginx or Caddy.

**Q: What port does Railway use?**
A: Railway uses the port your container listens on (80 in our case) and exposes it via their proxy on standard HTTPS (443).

**Q: Do I need to change ports for production?**
A: No! Keep port 80 in the container. Cloud platforms handle the rest.

---

**Summary:** Container always uses port 80. Local Docker maps it to 5000. Cloud platforms give you a public HTTPS URL. No localhost in the cloud! 🚀
