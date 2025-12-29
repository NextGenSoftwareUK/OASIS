import { Module } from '@nestjs/common';
import { JwtModule } from '@nestjs/jwt';
import { PassportModule } from '@nestjs/passport';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { User } from '../users/entities/user.entity';
import { AuthService } from './services/auth.service';
import { OasisAuthService } from './services/oasis-auth.service';
import { UserSyncService } from './services/user-sync.service';
import { JwtStrategy } from './strategies/jwt.strategy';
import { AuthController } from './controllers/auth.controller';
import { UserController } from './controllers/user.controller';
import { BetterAuthService } from './services/better-auth.service';
import { BetterAuthController } from './controllers/better-auth.controller';
import { OasisLinkService } from './services/oasis-link.service';
import { OasisModule } from '../services/oasis.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([User]),
    PassportModule.register({ defaultStrategy: 'jwt' }),
    JwtModule.registerAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        secret:
          configService.get<string>('JWT_SECRET') ||
          'YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters',
        signOptions: { expiresIn: '7d' },
      }),
    }),
    OasisModule, // For OASIS wallet services
  ],
  controllers: [AuthController, UserController, BetterAuthController],
  providers: [
    AuthService,
    OasisAuthService,
    UserSyncService,
    JwtStrategy,
    BetterAuthService,
    OasisLinkService,
  ],
  exports: [
    AuthService,
    JwtStrategy,
    PassportModule,
    BetterAuthService,
    OasisLinkService,
  ],
})
export class AuthModule {}












