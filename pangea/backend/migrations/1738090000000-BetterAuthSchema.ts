import { MigrationInterface, QueryRunner } from 'typeorm';

export class BetterAuthSchema1738090000000 implements MigrationInterface {
  name = 'BetterAuthSchema1738090000000';

  public async up(queryRunner: QueryRunner): Promise<void> {
    // Create Better-Auth user table
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "user" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "email" VARCHAR(255) NOT NULL,
        "email_verified" BOOLEAN NOT NULL DEFAULT false,
        "name" VARCHAR(255),
        "image" VARCHAR(255),
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_user_email" UNIQUE ("email"),
        CONSTRAINT "PK_user" PRIMARY KEY ("id")
      )
    `);

    // Create Better-Auth session table
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "session" (
        "id" VARCHAR(255) NOT NULL,
        "user_id" uuid NOT NULL,
        "expires_at" TIMESTAMP NOT NULL,
        "token" VARCHAR(255) NOT NULL,
        "ip_address" VARCHAR(45),
        "user_agent" TEXT,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_session_token" UNIQUE ("token"),
        CONSTRAINT "PK_session" PRIMARY KEY ("id"),
        CONSTRAINT "FK_session_user" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE
      )
    `);

    // Create Better-Auth account table (for OAuth)
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "account" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "user_id" uuid NOT NULL,
        "account_id" VARCHAR(255) NOT NULL,
        "provider" VARCHAR(50) NOT NULL,
        "access_token" TEXT,
        "refresh_token" TEXT,
        "expires_at" TIMESTAMP,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_account_provider_account_id" UNIQUE ("provider", "account_id"),
        CONSTRAINT "PK_account" PRIMARY KEY ("id"),
        CONSTRAINT "FK_account_user" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE
      )
    `);

    // Create Better-Auth verification table (for email verification, password reset, etc.)
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "verification" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "identifier" VARCHAR(255) NOT NULL,
        "value" VARCHAR(255) NOT NULL,
        "expires_at" TIMESTAMP NOT NULL,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "PK_verification" PRIMARY KEY ("id")
      )
    `);

    // Create indexes for Better-Auth tables
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_session_user_id" ON "session" ("user_id")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_session_expires_at" ON "session" ("expires_at")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_account_user_id" ON "account" ("user_id")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_verification_identifier" ON "verification" ("identifier")`);

    // Create OASIS mapping table
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "user_oasis_mapping" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "user_id" uuid NOT NULL,
        "avatar_id" VARCHAR(255) NOT NULL,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_user_oasis_mapping_user_id" UNIQUE ("user_id"),
        CONSTRAINT "UQ_user_oasis_mapping_avatar_id" UNIQUE ("avatar_id"),
        CONSTRAINT "PK_user_oasis_mapping" PRIMARY KEY ("id"),
        CONSTRAINT "FK_user_oasis_mapping_user" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE
      )
    `);

    // Create Pangea-specific user data table
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS "pangea_user_data" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "user_id" uuid NOT NULL,
        "role" VARCHAR(20) NOT NULL DEFAULT 'user',
        "kyc_status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        "is_active" BOOLEAN NOT NULL DEFAULT true,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_pangea_user_data_user_id" UNIQUE ("user_id"),
        CONSTRAINT "PK_pangea_user_data" PRIMARY KEY ("id"),
        CONSTRAINT "FK_pangea_user_data_user" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE
      )
    `);

    // Create indexes for mapping tables
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_user_oasis_mapping_user_id" ON "user_oasis_mapping" ("user_id")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_user_oasis_mapping_avatar_id" ON "user_oasis_mapping" ("avatar_id")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_pangea_user_data_user_id" ON "pangea_user_data" ("user_id")`);
    await queryRunner.query(`CREATE INDEX IF NOT EXISTS "idx_pangea_user_data_role" ON "pangea_user_data" ("role")`);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    // Drop indexes
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_pangea_user_data_role"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_pangea_user_data_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_user_oasis_mapping_avatar_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_user_oasis_mapping_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_verification_identifier"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_account_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_session_expires_at"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_session_user_id"`);

    // Drop tables (in reverse order of dependencies)
    await queryRunner.query(`DROP TABLE IF EXISTS "pangea_user_data"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "user_oasis_mapping"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "verification"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "account"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "session"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "user"`);
  }
}

