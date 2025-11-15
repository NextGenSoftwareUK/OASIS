/* eslint-disable no-console */
require('dotenv').config({ path: './config/.env' });
const mongoose = require('mongoose');
const bcrypt = require('bcrypt');

const User = require('../models/userModel');
const Driver = require('../models/driverModel');
const Car = require('../models/carModal');
const Booking = require('../models/bookingModal');
const GlobalSettings = require('../models/globalSettingsModal');

const {
  Database_Url,
  SEED_ADMIN_EMAIL = 'admin@timorides.com',
  SEED_ADMIN_PASSWORD = 'ChangeMe123!',
  SEED_DRIVER_EMAIL = 'driver@timorides.com',
  SEED_DRIVER_PASSWORD = 'DriverDemo123!',
  SEED_DRIVER_PHONE = '+27700000000',
  SEED_DRIVER_NAME = 'Demo Driver',
  SEED_RIDER_EMAIL = 'rider@timorides.com',
  SEED_RIDER_PASSWORD = 'RiderDemo123!',
  SEED_RIDER_PHONE = '+27710000000',
  SEED_RIDER_NAME = 'Demo Rider',
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

async function ensureRider() {
  const passwordHash = await bcrypt.hash(SEED_RIDER_PASSWORD, 10);

  const rider = await User.findOneAndUpdate(
    { email: SEED_RIDER_EMAIL },
    {
      fullName: SEED_RIDER_NAME,
      phone: SEED_RIDER_PHONE,
      password: passwordHash,
      role: 'user',
      isVerified: true,
    },
    { upsert: true, new: true, setDefaultsOnInsert: true }
  );

  console.log(`✅ Rider ready (${rider.email})`);
  return rider;
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

async function createSampleBookings({ rider, car }) {
  if (!rider || !car) {
    console.log('⚠️ Skipping booking seeding (missing rider or car)');
    return;
  }

  const baseSource = {
    address: 'King Shaka Intl Airport, Durban',
    latitude: -29.614444,
    longitude: 31.119722,
  };

  const baseDestination = {
    address: 'Umhlanga Arch, Durban',
    latitude: -29.725556,
    longitude: 31.068611,
  };

  const now = new Date();

  const scenarios = [
    {
      seedKey: 'pending-cash',
      status: 'pending',
      isCash: true,
      payment: { method: 'cash', status: 'pending' },
    },
    {
      seedKey: 'accepted-cash',
      status: 'accepted',
      isCash: true,
      payment: { method: 'cash', status: 'pending' },
      timeline: { acceptedAt: now },
    },
    {
      seedKey: 'started-wallet',
      status: 'started',
      isCash: false,
      payment: { method: 'wallet', status: 'pending' },
      timeline: {
        acceptedAt: now,
        startedAt: new Date(now.getTime() + 5 * 60 * 1000),
      },
    },
    {
      seedKey: 'completed-card',
      status: 'completed',
      isCash: false,
      payment: {
        method: 'card',
        status: 'paid',
        paidAt: new Date(now.getTime() + 15 * 60 * 1000),
        reference: 'SEED-CARD-PAID',
      },
      timeline: {
        acceptedAt: now,
        startedAt: new Date(now.getTime() + 5 * 60 * 1000),
        completedAt: new Date(now.getTime() + 25 * 60 * 1000),
      },
    },
  ];

  for (const scenario of scenarios) {
    // eslint-disable-next-line no-await-in-loop
    await Booking.findOneAndUpdate(
      { trxId: `seed-${scenario.seedKey}` },
      {
        car: car.id,
        user: rider.id,
        status: scenario.status,
        bookingType: 'passengers',
        tripAmount: 275.5,
        currency: {
          symbol: 'R',
          code: 'ZAR',
        },
        fullName: rider.fullName,
        phoneNumber: rider.phone,
        email: rider.email,
        passengers: 1,
        isCash: scenario.isCash,
        isDriverAccept: ['accepted', 'started', 'completed'].includes(
          scenario.status
        ),
        trxId: `seed-${scenario.seedKey}`,
        sourceLocation: baseSource,
        destinationLocation: baseDestination,
        departureTime: new Date(now.getTime() + 30 * 60 * 1000),
        payment: scenario.payment,
        timeline: scenario.timeline,
      },
      { upsert: true, new: true, setDefaultsOnInsert: true }
    );
  }

  console.log('✅ Sample bookings seeded (pending, accepted, started, completed)');
}

async function run() {
  try {
    await connect();
    await ensureGlobalSettings();
    await ensureAdmin();
    const driver = await ensureDriver();
    const car = await ensureCar(driver.id);
    const rider = await ensureRider();
    await createSampleBookings({ rider, car });
    console.log('\n🎉 Seed complete. You can now log in with the seeded accounts and sample bookings.\n');
  } catch (error) {
    console.error('❌ Seed failed', error);
  } finally {
    await mongoose.disconnect();
    process.exit(0);
  }
}

run();


