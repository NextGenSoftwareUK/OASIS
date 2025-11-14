/* eslint-disable no-console */
require('dotenv').config({ path: './config/.env' });
const mongoose = require('mongoose');
const bcrypt = require('bcrypt');

const User = require('../models/userModel');
const Driver = require('../models/driverModel');
const Car = require('../models/carModal');
const GlobalSettings = require('../models/globalSettingsModal');

const {
  Database_Url,
  SEED_ADMIN_EMAIL = 'admin@timorides.com',
  SEED_ADMIN_PASSWORD = 'ChangeMe123!',
  SEED_DRIVER_EMAIL = 'driver@timorides.com',
  SEED_DRIVER_PASSWORD = 'DriverDemo123!',
  SEED_DRIVER_PHONE = '+27700000000',
  SEED_DRIVER_NAME = 'Demo Driver',
  DEFAULT_STATE = 'kwaZuluNatal',
} = process.env;

if (!Database_Url) {
  console.error('Database_Url is missing in config/.env');
  process.exit(1);
}

async function connect() {
  await mongoose.connect(Database_Url);
  console.log('✅ Connected to MongoDB');
}

async function ensureGlobalSettings() {
  const existing = await GlobalSettings.findOne();

  if (existing) {
    return existing;
  }

  const defaults = await GlobalSettings.create({
    pricePerKm: 12.5,
    driverWalletPercentage: 0.8,
    userPenalizeRate: 0.15,
    businessCommission: 0.2,
  });

  console.log('✅ Global pricing defaults created');
  return defaults;
}

async function ensureAdmin() {
  const passwordHash = await bcrypt.hash(SEED_ADMIN_PASSWORD, 10);

  const admin = await User.findOneAndUpdate(
    { email: SEED_ADMIN_EMAIL },
    {
      fullName: 'Timo Admin',
      phone: '+27000000000',
      password: passwordHash,
      role: 'admin',
      isVerified: true,
    },
    { upsert: true, new: true, setDefaultsOnInsert: true }
  );

  console.log(`✅ Admin ready (${admin.email})`);
  return admin;
}

async function ensureDriver() {
  const passwordHash = await bcrypt.hash(SEED_DRIVER_PASSWORD, 10);

  const driver = await Driver.findOneAndUpdate(
    { email: SEED_DRIVER_EMAIL },
    {
      fullName: SEED_DRIVER_NAME,
      phone: SEED_DRIVER_PHONE,
      password: passwordHash,
      role: 'driver',
      isVerified: true,
      location: {
        latitude: -29.8587,
        longitude: 31.0218,
      },
    },
    { upsert: true, new: true, setDefaultsOnInsert: true }
  );

  console.log(`✅ Driver ready (${driver.email})`);
  return driver;
}

async function ensureCar(driverId) {
  const car = await Car.findOneAndUpdate(
    { driver: driverId },
    {
      driver: driverId,
      vehicleRegNumber: 'TIM-001-ZA',
      vehicleModelYear: '2022',
      vehicleMake: 'Mercedes-Benz',
      vehicleModel: 'E200',
      vehicleColor: 'Obsidian Black',
      engineNumber: 'TIMOENGINE001',
      vehicleAddress: 'Durban, South Africa',
      state: DEFAULT_STATE.toLowerCase(),
      isActive: true,
      isVerify: true,
      isOffline: false,
      location: {
        latitude: -29.8587,
        longitude: 31.0218,
      },
    },
    { upsert: true, new: true, setDefaultsOnInsert: true }
  );

  console.log('✅ Demo car verified and active');
  return car;
}

async function run() {
  try {
    await connect();
    await ensureGlobalSettings();
    await ensureAdmin();
    const driver = await ensureDriver();
    await ensureCar(driver.id);
    console.log('\n🎉 Seed complete. You can now log in with the seeded accounts.\n');
  } catch (error) {
    console.error('❌ Seed failed', error);
  } finally {
    await mongoose.disconnect();
    process.exit(0);
  }
}

run();


