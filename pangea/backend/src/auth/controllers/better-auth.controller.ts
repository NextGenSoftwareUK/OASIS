import { Controller, All, Req, Res } from '@nestjs/common';
import { Request, Response } from 'express';
import { BetterAuthService } from '../services/better-auth.service';

@Controller('auth')
export class BetterAuthController {
  constructor(private betterAuthService: BetterAuthService) {}

  @All('*')
  async handleAuth(@Req() req: Request, @Res() res: Response) {
    const handler = this.betterAuthService.getHandler();
    // Better-Auth handler expects a single request object
    return handler(req as any);
  }
}

