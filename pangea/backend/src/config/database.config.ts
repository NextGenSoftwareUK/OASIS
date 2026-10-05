import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { TypeOrmModuleOptions, TypeOrmOptionsFactory } from '@nestjs/typeorm';

@Injectable()
export class DatabaseConfig implements TypeOrmOptionsFactory {
  constructor(private configService: ConfigService) {}

  createTypeOrmOptions(): TypeOrmModuleOptions {
    const databaseUrl = this.configService.get<string>('DATABASE_URL');
    const isProduction = this.configService.get<string>('NODE_ENV') === 'production';
    
    // In production, migrations are compiled to dist/migrations (relative to dist/config)
    // In development, use source migrations from project root
    const migrationsPath = isProduction
      ? __dirname + '/../migrations/*.js'
      : __dirname + '/../../migrations/*{.ts,.js}';

    // If DATABASE_URL is provided, use it directly
    if (databaseUrl) {
      return {
        type: 'postgres',
        url: databaseUrl,
        entities: [__dirname + '/../**/*.entity{.ts,.js}'],
        migrations: [migrationsPath],
        synchronize: !isProduction && this.configService.get<string>('NODE_ENV') === 'development',
        logging: !isProduction && this.configService.get<string>('NODE_ENV') === 'development',
      };
    }

    // Otherwise, use individual connection parameters
    return {
      type: 'postgres',
      host: this.configService.get<string>('DB_HOST', 'localhost'),
      port: this.configService.get<number>('DB_PORT', 5432),
      username: this.configService.get<string>('DB_USERNAME', 'user'),
      password: this.configService.get<string>('DB_PASSWORD', 'password'),
      database: this.configService.get<string>('DB_DATABASE', 'pangea'),
      entities: [__dirname + '/../**/*.entity{.ts,.js}'],
      migrations: [migrationsPath],
      synchronize: !isProduction && this.configService.get<string>('NODE_ENV') === 'development',
      logging: !isProduction && this.configService.get<string>('NODE_ENV') === 'development',
    };
  }
}












