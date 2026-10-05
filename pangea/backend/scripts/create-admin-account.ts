/**
 * Script to create an admin account
 * 
 * Usage:
 *  1. Set ADMIN_EMAIL and ADMIN_PASSWORD environment variables
 *  2. Run: npx ts-node scripts/create-admin-account.ts
 * 
 * Or with environment variables inline:
 *   ADMIN_EMAIL=user@example.com ADMIN_PASSWORD=password npx ts-node scripts/create-admin-account.ts
 */

import { DataSource } from 'typeorm';
import { User } from '../src/users/entities/user.entity';
import { config } from 'dotenv';
import * as path from 'path';

// Load environment variables
config({ path: path.resolve(__dirname, '../.env') });

const ADMIN_EMAIL = process.env.ADMIN_EMAIL;

if (!ADMIN_EMAIL) {
  console.error('❌ Error: ADMIN_EMAIL environment variable is required');
  console.log('\nUsage:');
  console.log('  ADMIN_EMAIL=user@example.com npx ts-node scripts/create-admin-account.ts');
  console.log('\nOr set DATABASE_URL if running from different directory:');
  console.log('  DATABASE_URL=postgresql://... ADMIN_EMAIL=user@example.com npx ts-node scripts/create-admin-account.ts');
  process.exit(1);
}

async function createAdminAccount() {
  console.log('🔐 Creating admin account...\n');
  console.log(`Target email: ${ADMIN_EMAIL}\n`);

  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    console.error('❌ Error: DATABASE_URL environment variable is required');
    console.log('\nGet it from Railway dashboard → PostgreSQL service → Variables → DATABASE_URL');
    process.exit(1);
  }

  // Create DataSource
  const dataSource = new DataSource({
    type: 'postgres',
    url: databaseUrl,
    entities: [User],
    synchronize: false,
    logging: false,
  });

  try {
    // Connect to database
    await dataSource.initialize();
    console.log('✅ Connected to database\n');

    // Find user by email
    const userRepository = dataSource.getRepository(User);
    const user = await userRepository.findOne({
      where: { email: ADMIN_EMAIL },
    });

    if (!user) {
      console.error(`❌ Error: User with email "${ADMIN_EMAIL}" not found`);
      console.log('\n💡 Make sure the user has registered first via:');
      console.log('   POST /api/auth/register');
      process.exit(1);
    }

    // Check if already admin
    const previousRole = user.role;
    if (user.role === 'admin') {
      console.log(`✅ User "${ADMIN_EMAIL}" is already an admin`);
      console.log(`   User ID: ${user.id}`);
      console.log(`   Role: ${user.role}`);
      process.exit(0);
    }

    // Update role to admin
    user.role = 'admin';
    await userRepository.save(user);

    console.log(`✅ Successfully promoted user to admin!`);
    console.log(`   Email: ${user.email}`);
    console.log(`   User ID: ${user.id}`);
    console.log(`   Role: ${previousRole} → ${user.role}`);
    console.log('\n📝 Next steps:');
    console.log('   1. Re-login to get a new JWT token with admin role');
    console.log('   2. Test admin access: GET /api/admin/users');
  } catch (error) {
    console.error('❌ Error creating admin account:', error.message);
    if (error.stack) {
      console.error(error.stack);
    }
    process.exit(1);
  } finally {
    await dataSource.destroy();
  }
}

createAdminAccount();



