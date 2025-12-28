import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import { DataSource } from 'typeorm';
import { InjectDataSource } from '@nestjs/typeorm';

/**
 * Service to automatically run pending migrations on startup
 * Only runs migrations if there are pending ones (safe to call multiple times)
 */
@Injectable()
export class MigrationRunnerService implements OnModuleInit {
  private readonly logger = new Logger(MigrationRunnerService.name);

  constructor(
    @InjectDataSource() private dataSource: DataSource,
  ) {}

  async onModuleInit() {
    // Run migrations in background, don't block startup
    setImmediate(async () => {
      try {
        this.logger.log('Running database migrations...');
        
        // TypeORM will automatically skip migrations that have already been run
        const executedMigrations = await this.dataSource.runMigrations();
        
        if (executedMigrations && executedMigrations.length > 0) {
          this.logger.log(`✅ Executed ${executedMigrations.length} migration(s):`);
          executedMigrations.forEach((migration) => {
            this.logger.log(`   - ${migration.name}`);
          });
        } else {
          this.logger.log('✅ No pending migrations. Database is up to date.');
        }
      } catch (error) {
        this.logger.error(
          `Failed to run migrations: ${error.message}`,
          error.stack,
        );
        // Don't throw - allow app to start even if migrations fail
        // This is a safety measure to prevent blocking startup
      }
    });
  }
}




