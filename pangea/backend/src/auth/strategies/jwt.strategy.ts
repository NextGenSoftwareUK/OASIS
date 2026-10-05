import { Injectable, UnauthorizedException } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import { ConfigService } from '@nestjs/config';
import { AuthService } from '../services/auth.service';

/**
 * JWT Strategy for Passport
 * Validates JWT tokens and extracts user information
 */
@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(
    private configService: ConfigService,
    private authService: AuthService,
  ) {
    super({
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      ignoreExpiration: false,
      secretOrKey:
        configService.get<string>('JWT_SECRET') ||
        'YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters',
    });
  }

  /**
   * Validate JWT payload and return user
   * This method is called after JWT is verified
   */
  async validate(payload: any) {
    // Validate user still exists and is active
    const user = await this.authService.validateUser(payload.sub);
    if (!user || !user.isActive) {
      throw new UnauthorizedException('User not found or inactive');
    }

    // Return user object (attached to request.user)
    return {
      id: user.id,
      email: user.email,
      username: user.username,
      avatarId: user.avatarId,
      role: user.role,
    };
  }
}












