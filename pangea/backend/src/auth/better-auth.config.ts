import { betterAuth } from 'better-auth';
import { typeormAdapter } from '@hedystia/better-auth-typeorm';
import { DataSource } from 'typeorm';
import { ConfigService } from '@nestjs/config';

export function createBetterAuth(dataSource: DataSource, configService: ConfigService) {
  return betterAuth({
    database: typeormAdapter(dataSource),
    
    emailAndPassword: {
      enabled: true,
      requireEmailVerification: false, // Set to true in production
    },
    
    socialProviders: {
      google: {
        clientId: configService.get<string>('GOOGLE_CLIENT_ID') || '',
        clientSecret: configService.get<string>('GOOGLE_CLIENT_SECRET') || '',
      },
      github: {
        clientId: configService.get<string>('GITHUB_CLIENT_ID') || '',
        clientSecret: configService.get<string>('GITHUB_CLIENT_SECRET') || '',
      },
      // Add more providers as needed
    },
    
    session: {
      expiresIn: 60 * 60 * 24 * 7, // 7 days
      updateAge: 60 * 60 * 24, // 1 day
    },
    
    advanced: {
      useSecureCookies: process.env.NODE_ENV === 'production',
      cookiePrefix: 'pangea',
    },
    
    // Base URL for callbacks
    baseURL: configService.get<string>('BASE_URL') || 'http://localhost:3000',
    basePath: '/api/auth',
    secret: configService.get<string>('BETTER_AUTH_SECRET') || 'change-me-in-production',
  });
}

