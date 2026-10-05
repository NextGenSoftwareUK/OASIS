import { DataSource, DataSourceOptions } from 'typeorm';
import { ConfigService } from '@nestjs/config';
import { config } from 'dotenv';

// Load environment variables
config();

const configService = new ConfigService();

const databaseUrl = configService.get<string>('DATABASE_URL');

const isProduction = configService.get<string>('NODE_ENV') === 'production';
// In production, migrations are compiled to dist/migrations (relative to dist/config)
// In development, use source migrations from project root
const migrationsPath = isProduction
  ? __dirname + '/../migrations/*.js'
  : __dirname + '/../../migrations/*{.ts,.js}';

const dataSourceOptions: DataSourceOptions = databaseUrl
  ? {
      type: 'postgres',
      url: databaseUrl,
      entities: [__dirname + '/../**/*.entity{.ts,.js}'],
      migrations: [migrationsPath],
    }
  : {
      type: 'postgres',
      host: configService.get<string>('DB_HOST', 'localhost'),
      port: configService.get<number>('DB_PORT', 5432),
      username: configService.get<string>('DB_USERNAME', 'user'),
      password: configService.get<string>('DB_PASSWORD', 'password'),
      database: configService.get<string>('DB_DATABASE', 'pangea'),
      entities: [__dirname + '/../**/*.entity{.ts,.js}'],
      migrations: [migrationsPath],
    };

export const AppDataSource = new DataSource(dataSourceOptions);












