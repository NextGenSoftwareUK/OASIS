import { Injectable, Inject } from '@nestjs/common';
import { InjectDataSource } from '@nestjs/typeorm';
import { DataSource } from 'typeorm';
import Redis from 'ioredis';

@Injectable()
export class AppService {
  constructor(
    @InjectDataSource() private dataSource: DataSource,
    @Inject('REDIS_CLIENT') private redis: Redis,
  ) {}

  async getHealth() {
    const checks = {
      status: 'ok',
      timestamp: new Date().toISOString(),
      service: 'Pangea Markets Backend',
      version: '1.0.0',
      checks: {
        database: await this.checkDatabase(),
        redis: await this.checkRedis(),
      },
    };

    // If any check fails, set status to degraded
    if (
      checks.checks.database.status !== 'ok' ||
      checks.checks.redis.status !== 'ok'
    ) {
      checks.status = 'degraded';
    }

    return checks;
  }

  private async checkDatabase() {
    try {
      await this.dataSource.query('SELECT 1');
      return {
        status: 'ok',
        message: 'Database connection successful',
      };
    } catch (error) {
      return {
        status: 'error',
        message: 'Database connection failed',
        error: error.message,
      };
    }
  }

  private async checkRedis() {
    try {
      await this.redis.ping();
      return {
        status: 'ok',
        message: 'Redis connection successful',
      };
    } catch (error) {
      return {
        status: 'error',
        message: 'Redis connection failed',
        error: error.message,
      };
    }
  }
}












