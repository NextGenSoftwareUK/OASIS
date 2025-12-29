import { Injectable, CanActivate, ExecutionContext, UnauthorizedException } from '@nestjs/common';
import { BetterAuthService } from '../services/better-auth.service';

@Injectable()
export class BetterAuthGuard implements CanActivate {
  constructor(private betterAuthService: BetterAuthService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest();
    const auth = this.betterAuthService.getAuth();

    try {
      const session = await auth.api.getSession({ headers: request.headers });
      
      if (!session?.user) {
        throw new UnauthorizedException('Not authenticated');
      }

      // Attach user to request
      request.user = session.user;
      request.session = session;

      return true;
    } catch (error) {
      throw new UnauthorizedException('Authentication failed');
    }
  }
}

