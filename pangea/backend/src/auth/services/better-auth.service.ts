import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { DataSource } from 'typeorm';
import { createBetterAuth } from '../better-auth.config';

@Injectable()
export class BetterAuthService {
  private auth: ReturnType<typeof createBetterAuth>;

  constructor(
    private dataSource: DataSource,
    private configService: ConfigService,
  ) {
    this.auth = createBetterAuth(this.dataSource, this.configService);
  }

  getAuth() {
    return this.auth;
  }

  getHandler() {
    return this.auth.handler;
  }
}

