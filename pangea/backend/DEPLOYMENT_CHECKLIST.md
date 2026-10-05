# Deployment Checklist

Use this checklist before deploying to production.

## Pre-Deployment

### Code Preparation
- [ ] All code committed and pushed to repository
- [ ] No hardcoded secrets in code
- [ ] Environment variables properly configured
- [ ] Build passes locally: `npm run build`
- [ ] Tests pass: `npm test`
- [ ] Production mode works locally: `NODE_ENV=production npm run start:prod`

### Environment Variables
- [ ] `NODE_ENV=production` set
- [ ] `JWT_SECRET` is strong and random (32+ characters)
- [ ] `JWT_SECRET` is different from development
- [ ] `DATABASE_URL` or database connection vars configured
- [ ] `REDIS_URL` or Redis connection vars configured
- [ ] `OASIS_API_URL` and `OASIS_API_KEY` configured
- [ ] `CORS_ORIGIN` includes production frontend URL
- [ ] All blockchain RPC URLs configured (if needed)

### Database
- [ ] Database migrations are up to date
- [ ] Migration files are in repository
- [ ] Database backup strategy in place
- [ ] Test database connection works

### Redis
- [ ] Redis instance provisioned
- [ ] Redis connection tested
- [ ] Redis persistence configured (if needed)

### Security
- [ ] All API keys are secure and not in code
- [ ] JWT secret is strong
- [ ] CORS origins are restricted to production domains
- [ ] Rate limiting implemented (if planned)
- [ ] Security headers configured (if applicable)

## Deployment Steps

### Railway Setup
- [ ] Railway account created
- [ ] PostgreSQL service added
- [ ] Redis service added
- [ ] Backend service added and linked to repo
- [ ] Root directory set correctly (`pangea/backend` if needed)
- [ ] Build command configured: `npm install && npm run build`
- [ ] Start command configured: `npm run start:prod`
- [ ] Node version set (18 or 20)

### Environment Configuration
- [ ] All environment variables added to Railway
- [ ] Service references configured (`${{Postgres.DATABASE_URL}}`)
- [ ] Variables verified (no typos)

### Database Migrations
- [ ] Migrations run on production database
- [ ] Database schema verified
- [ ] Initial data seeded (if needed)

### Deployment
- [ ] Code pushed to repository
- [ ] Railway deployment triggered
- [ ] Build logs checked (no errors)
- [ ] Deployment logs checked (service started successfully)

## Post-Deployment

### Health Check
- [ ] Health endpoint responds: `GET /api/health`
- [ ] Database connection successful
- [ ] Redis connection successful
- [ ] Service is running and stable

### Functionality Tests
- [ ] Authentication endpoints work (`/api/auth/login`, `/api/auth/register`)
- [ ] Protected endpoints require JWT
- [ ] Public endpoints work without auth
- [ ] WebSocket connections work (if applicable)
- [ ] Order creation works
- [ ] Trade execution works
- [ ] Wallet operations work
- [ ] Transaction endpoints work

### Frontend Integration
- [ ] Frontend can connect to backend
- [ ] CORS configured correctly
- [ ] Frontend authentication flow works
- [ ] API calls succeed from frontend

### Monitoring
- [ ] Logs are accessible in Railway dashboard
- [ ] Error logging is working
- [ ] Health check endpoint configured (optional)
- [ ] Alerts configured for deployment failures

### Documentation
- [ ] Deployment URL documented
- [ ] Environment variables documented
- [ ] Database connection details secured
- [ ] Team has access to Railway dashboard

## Rollback Plan

- [ ] Know how to rollback to previous deployment
- [ ] Database migration rollback plan (if needed)
- [ ] Previous working version tagged in Git

## Ongoing Maintenance

- [ ] Regular database backups scheduled
- [ ] Monitor error logs regularly
- [ ] Set up alerts for critical errors
- [ ] Plan for scaling (if needed)
- [ ] Monitor resource usage (CPU, memory, disk)

## Emergency Contacts

- [ ] Railway support access
- [ ] Database admin access
- [ ] Team member contact list for deployment issues

---

## Quick Test Commands

After deployment, run these to verify:

```bash
# Health check
curl https://your-app.up.railway.app/api/health

# Test auth (should work)
curl -X POST https://your-app.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test123","username":"testuser"}'

# Test protected route (should fail without token)
curl https://your-app.up.railway.app/api/user/profile

# Test protected route with invalid token (should fail)
curl https://your-app.up.railway.app/api/user/profile \
  -H "Authorization: Bearer invalid-token"
```

All tests should pass before considering deployment complete.








