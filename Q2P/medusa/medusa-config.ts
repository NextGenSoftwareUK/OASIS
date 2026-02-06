import { loadEnv, defineConfig } from '@medusajs/framework/utils'

loadEnv(process.env.NODE_ENV || 'development', process.cwd())

// Prefer DATABASE_URL (Railway internal). Fallback to public URL with SSL for connectivity.
function getDatabaseUrl() {
  const url = process.env.DATABASE_URL || process.env.DATABASE_PUBLIC_URL || ''
  if (!url) return url
  // If using public URL, ensure SSL and longer connect timeout (Railway).
  const isPublic = url.includes('proxy.rlwy.net') || url.includes('railway.app')
  const separator = url.includes('?') ? '&' : '?'
  const opts = isPublic ? `${separator}sslmode=require&connect_timeout=30` : ''
  return url + opts
}

module.exports = defineConfig({
  projectConfig: {
    databaseUrl: getDatabaseUrl(),
    http: {
      storeCors: process.env.STORE_CORS!,
      adminCors: process.env.ADMIN_CORS!,
      authCors: process.env.AUTH_CORS!,
      jwtSecret: process.env.JWT_SECRET || "supersecret",
      cookieSecret: process.env.COOKIE_SECRET || "supersecret",
    }
  },
  modules: [
    {
      resolve: '@medusajs/medusa/payment',
      options: {
        providers: [
          {
            resolve: '@medusajs/medusa/payment-stripe',
            id: 'stripe',
            options: {
              apiKey: process.env.STRIPE_API_KEY,
              webhookSecret: process.env.STRIPE_WEBHOOK_SECRET,
              capture: true,
            },
          },
        ],
      },
    },
  ],
})
