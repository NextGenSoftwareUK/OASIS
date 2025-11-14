#### Timo Rides

### Set Up

- Install packages

```
npm i
```

- Start server

```
npm run start
```

- Base api Url

```
localhost:4205
```

- Swagger Docs

```
localhost:4205/api-docs
```

### Seed demo data (admin, driver, car)

Populate MongoDB with a verified admin account, a demo driver, and an active car:

```
npm run seed
```

Override defaults through the `.env` variables `SEED_ADMIN_*`, `SEED_DRIVER_*`, and `DEFAULT_STATE`. Rerunning the script is idempotent.

### Driver status endpoints

Drivers (or admins) can now update their location and availability through REST endpoints:

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/drivers/{driverId}/location` | `PATCH` | Update driver + active car GPS coordinates (body: `latitude`, `longitude`, optional `bearing`, `speed`). |
| `/api/drivers/{driverId}/status` | `PATCH` | Toggle `isOffline` / `isActive` flags for the driver’s car. |
| `/api/drivers/{driverId}/status` | `GET` | Retrieve driver profile plus current car snapshot. |

All routes require authentication; drivers may only mutate their own record while admins can manage anyone.

### Folder Structure

```
project-root/
|
├── config/
│ ├── database.js // Configuration for database connection
│ └── env.js // Environment variables
│
├── controllers/
│ ├── adminController.js // Controllers for handling admin-related logic
│ ├── authController.js // Controllers for handling auth-related logic
│ ├── bookingController.js // Controllers for handling booking-related logic
│ ├── carController.js // Controllers for handling car-related logic
│ ├── distanceController.js // Controllers for handling distance-related logic (test run)
│ ├── notificationController.js // Controllers for handling notification-related logic (test run)
│ ├── otpController.js // Controllers for handling otp-related logic
│ ├── uploadController.js // Controllers for handling upload-related logic
│ └── userController.js // Controllers for handling user-related logic
│
├── models/
│ ├── bookngModel.js // Model definition for bookings
│ ├── carModel.js // Model definition for cars
│ ├── driverModel.js // Model definition for drivers
| ├── globalSettingsModel.js // Model definition for admin settings
| ├── otpModal.js // Model definition for storing start and end trip otp
│ └── userModal.js // Modal defination for users
│
├── routes/
│ ├── adminRoutes.js // Routes for admin specific related endpoints
│ ├── bookingRoutes.js // Routes for booking-related endpoints
│ ├── carRoutes.js // Routes for user-related endpoints
│ ├── userRoutes.js // Routes for car-related endpoints
│ ├── distanceRoutes.js // Routes for distance-related endpoints (test run)
│ ├── index.js // holds all Routes initials
│ ├── notificationRoutes.js // Routes for notification-related endpoints (test run)
│ ├── otpRoutes.js // Routes for otp-related endpoints
│ ├── uploadRoutes.js // Routes for uploads-related endpoints
│ └── userRoutes.js // Main router to aggregate all routes
│
├── middleware/
│ ├── authMiddleware.js // Middleware for authentication
│ └── authorizationMiddleware.js // Middleware for authorization
│
├── services/
│ ├── userService.js // Business logic for users
│ ├── driverService.js // Business logic for drivers
│ └── ... // Other services
│
├── validators/
│ ├── adminValidation.js // Input validation for admin-related requests
│ ├── authValidation.js // Input validation for auth-related requests
│ ├── bookingValidation.js // Input validation for booking-related requests
│ ├── carValidation.js // Input validation for car-related requests
│ ├── distanceValidation.js // Input validation for distance-related requests (test run)
│ ├── notificationValidation.js // Input validation for notification-related requests (test run)
│ ├── uploadValidation.js // Input validation for upload-related requests
│ ├── userValidation.js // Input validation for user-related requests
│ └── validationResponse.js // Input validation for genaral validation respnse
│
├── tests/ // Unit and integration tests
│
├── server.js // Entry point of the application
├── package.json // Node.js dependencies and scripts
└── README.md // Project documentation
```
