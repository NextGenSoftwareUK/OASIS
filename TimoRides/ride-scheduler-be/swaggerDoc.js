// Define security definitions for bearer token authentication
/**
 * @swagger
 * components:
 *   securitySchemes:
 *     bearerAuth:
 *       type: http
 *       scheme: bearer
 *       bearerFormat: JWT
 *   parameters:
 *     AuthorizationHeader:
 *       in: header
 *       name: Authorization
 *       schema:
 *         type: string
 *       required: true
 *       description: token
 */

/**
 * @swagger
 * tags:
 *   name: Auths
 *   description: Authentication management endpoints
 */

/**
 * @swagger
 * tags:
 *   name: Users
 *   description: User management endpoints
 */

/**
 * @swagger
 * tags:
 *   name: Cars
 *   description: Car management endpoints
 */

/**
 * @swagger
 * tags:
 *   name: Booking
 *   description: Schedule a Ride
 */

/**
 * @swagger
 * tags:
 *   name: Notification
 *   description: Notificatino notification
 */

/**
 * @swagger
 * tags:
 *   name: Upload
 *   description: Notificatino notification
 */

/**
 * @swagger
 * tags:
 *   name: Distance
 *   description: Distance management endpoints
 */

/**
 * @swagger
 * tags:
 *   name: Trip
 *   description: Trip start and end details
 */

/**
 * @swagger
 * tags:
 *   name: Admin
 *   description: Admin endpoints
 */

/* ====================
       USER ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/users:
 *   get:
 *     tags: [Users]
 *     summary: Get all users | driver role | user role (admin role only)
 *     description: Retrieve a list of all users.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: role
 *         in: query
 *         description: Role of the users to filter by
 *         required: false
 *         example: driver
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: A list of users.
 */

/**
 * @swagger
 * /api/users/{id}:
 *   get:
 *     tags: [Users]
 *     summary: Get user by ID
 *     description: Retrieve a user by their ID.
 *     parameters:
 *       - name: id
 *         in: path
 *         description: ID of the user to retrieve
 *         required: true
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: User found
 *       '404':
 *         description: User not found
 */

/**
 * @swagger
 * /api/users/{id}:
 *   delete:
 *     tags: [Users]
 *     summary: Delete user
 *     description: Disables user account for 30 days before delete
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: id
 *         in: path
 *         description: ID of the user to delete
 *         required: true
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: User found
 *       '404':
 *         description: User not found
 */

/**
 * @swagger
 * /api/users/{id}:
 *   put:
 *     tags: [Users]
 *     summary: Update a user
 *     description: Update a user with the provided data.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: id
 *         in: path
 *         description: ID of the user to delete
 *         required: true
 *         schema:
 *           type: string
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               fullName:
 *                 type: string
 *               phone:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 */

/**
 * @swagger
 * /api/users/wallet-transaction:
 *   get:
 *     tags: [Users]
 *     summary: Get uuser wallet transaction
 *     description: Retrieve a user wallet transaction.
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       '200':
 *         description: User found
 *       '404':
 *         description: User not found
 */

/**
 * @swagger
 * /api/users/request-payment:
 *   post:
 *     tags: [Users]
 *     summary: User request payment
 *     description: User request for payment
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               amount:
 *                 type: number
 *                 example: 0
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 */

/**
 * @swagger
 * /api/users/wallet-topup:
 *   post:
 *     tags: [Users]
 *     summary: User request payment
 *     description: User request for payment
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               trxId:
 *                 type: string
 *               trxRef:
 *                 type: string
 *               amount:
 *                 type: number
 *                 example: 0
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 */

/* ====================
       CAR ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/cars:
 *   get:
 *     tags: [Cars]
 *     summary: Get all cars
 *     description: Retrieve a list of all car.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: driverId
 *         in: query
 *         description: To get all cars associated to a driverId provided
 *         required: false
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: A list of cars.
 */

/**
 * @swagger
 * /api/cars/current-car/{driverId}:
 *   get:
 *     tags: [Cars]
 *     summary: Get currenct driver car
 *     description: Retrieve a  car.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: driverId
 *         in: path
 *         description: Driver id, to get a car current active owned by the driver
 *         required: true
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: A list of cars.
 */

/**
 * @swagger
 * /api/cars/{carId}:
 *   get:
 *     tags: [Cars]
 *     summary: Get  car
 *     description: Retrieve a  car.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: carId
 *         in: path
 *         description: Car id to get a particular car
 *         required: true
 *         schema:
 *           type: string
 *       - name: bookingId
 *         in: query
 *         description: Booking id to get a particular booking associated to car to get ride amount
 *         required: false
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: A list of cars.
 */

/**
 * @swagger
 * /api/cars:
 *   post:
 *     tags: [Cars]
 *     summary: Create Car entity
 *     description: Enter car details to db
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               vehicleRegNumber:
 *                 type: string
 *               vehicleModelYear:
 *                 type: string
 *               vehicleMake:
 *                 type: string
 *               vehicleModel:
 *                 type: string
 *               engineNumber:
 *                 type: string
 *               vehicleColor:
 *                 type: string
 *               insuranceBroker:
 *                 type: string
 *               insurancePolicyNumber:
 *                 type: string
 *               imagePath:
 *                 type: string
 *               altImagePath:
 *                 type: string
 *               interiorImagePath:
 *                 type: string
 *               vehicleAddress:
 *                 type: string
 *               state:
 *                 type: string
 *               location:
 *                 type: object
 *                 properties:
 *                   latitude:
 *                     type: number
 *                     example: 23
 *                   longitude:
 *                     type: number
 *                 required:
 *                   - latitude:
 *                   - longitude:
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/cars/{driverId}:
 *   put:
 *     tags: [Cars]
 *     summary: Update Car Entity
 *     description: Enter car details to db
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: driverId
 *         in: path
 *         description: Driver id to update driver car details car
 *         required: true
 *         schema:
 *           type: string
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               vehicleRegNumber:
 *                 type: string
 *               vehicleModelYear:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Car updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/cars/proximity:
 *   get:
 *     tags: [Cars]
 *     summary: Get all cars
 *     description: Retrieve a list of cars based on specified criteria.
 *     parameters:
 *       - name: page
 *         in: query
 *         description: "Page number for pagination (optional, default: 1)."
 *         schema:
 *           type: integer
 *           minimum: 1
 *           default: 1
 *       - name: pageSize
 *         in: query
 *         description: "Number of items per page (optional, default: 10, max: 100)."
 *         schema:
 *           type: integer
 *           minimum: 1
 *           maximum: 100
 *           default: 10
 *       - name: state
 *         in: query
 *         description: State of user booking ride from location.
 *         schema:
 *           type: string
 *         required: true
 *       - name: scheduledDate
 *         in: query
 *         description: The departureTime of  the ride.
 *         schema:
 *           type: string
 *         required: true
 *       - name: sourceLatitude
 *         in: query
 *         description: Source Latitude coordinate for location filtering (required).
 *         schema:
 *           type: number
 *           format: double
 *         required: true
 *       - name: sourceLongitude
 *         in: query
 *         description: Source Longitude coordinate for location filtering (required).
 *         schema:
 *           type: number
 *           format: double
 *         required: true
 *       - name: destinationLatitude
 *         in: query
 *         description: Destination Latitude coordinate for location filtering (required).
 *         schema:
 *           type: number
 *           format: double
 *         required: true
 *       - name: destinationLongitude
 *         in: query
 *         description: Destination Longitude coordinate for location filtering (required).
 *         schema:
 *           type: number
 *           format: double
 *         required: true
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/* ====================
    Booking ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/bookings:
 *   post:
 *     tags: [Booking]
 *     summary: Create Booking entity
 *     description: Enter booking details to db
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               car:
 *                 type: string
 *               fullName:
 *                 type: string
 *               phoneNumber:
 *                 type: string
 *                 example: "+234892781627"
 *               bookingType:
 *                 type: string
 *                 example: "goods"
 *               email:
 *                 type: string
 *                 example: "example#gmail.com"
 *               tripAmount:
 *                 type: string
 *                 example: "230"
 *               tripDuration:
 *                 type: string
 *                 example: "12 mins"
 *               tripDistance:
 *                 type: string
 *                 example: "2 km"
 *               state:
 *                 type: string
 *                 example: "lagos"
 *               isCash:
 *                 type: boolean
 *                 example: false
 *               passengers:
 *                 type: number
 *                 example: 0
 *               departureTime:
 *                 type: string
 *                 example: "2024-04-16T13:12:46.032Z"
 *               sourceLocation:
 *                 type: object
 *                 properties:
 *                   address:
 *                     type: string
 *                     example: "52 Linko drive"
 *                   latitude:
 *                     type: number
 *                     example: 6.4397641
 *                   longitude:
 *                     type: number
 *                     example: 3.4294794
 *                 required:
 *                   - latitude:
 *                   - longitude:
 *               destinationLocation:
 *                 type: object
 *                 properties:
 *                   address:
 *                     type: string
 *                     example: "52 Linko drive"
 *                   latitude:
 *                     type: number
 *                     example: 6.5095442
 *                   longitude:
 *                     type: number
 *                     example: 3.3710936
 *                 required:
 *                   - latitude:
 *                   - longitude:
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/bookings/re-book:
 *   post:
 *     tags: [Booking]
 *     summary: Create Booking entity
 *     description: Enter booking details to db
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               car:
 *                 type: string
 *               bookingId:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/bookings:
 *   get:
 *     tags: [Booking]
 *     summary: Get all booking
 *     description: Retrieve a list of all booking.
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       '200':
 *         description: A list of schedule.
 */

/**
 * @swagger
 * /api/bookings/{id}:
 *   get:
 *     tags: [Booking]
 *     summary: Get booking by ID
 *     description: Retrieve a booking by their ID.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: id
 *         in: path
 *         description: ID of the booking to retrieve
 *         required: true
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: booking found
 *       '404':
 *         description: booking not found
 */

/**
 * @swagger
 * /api/bookings/verify-acceptance:
 *   post:
 *     tags: [Booking]
 *     summary: Driver booking verify status
 *     description: This hold driver status of accepting the booking or not
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               token:
 *                 type: string
 *               isAccepted:
 *                 type: boolean
 *                 example: false
 *     responses:
 *       '200':
 *         description:  acceptance status updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/bookings/confirm-acceptance-status:
 *   post:
 *     tags: [Booking]
 *     summary: Driver booking acceptance status
 *     description: This hold driver status of accepting the booking or not
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               bookingId:
 *                 type: string
 *               isAccepted:
 *                 type: boolean
 *                 example: false
 *     responses:
 *       '200':
 *         description:  acceptance status updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/bookings/cancel-acceptance:
 *   post:
 *     tags: [Booking]
 *     summary: Cancel Booking
 *     description: This Cancels a previously accepted booking
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               bookingId:
 *                 type: string
 *     responses:
 *       '200':
 *         description:  acceptance status updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/* ====================
      AUTH END POINTS
   ====================
*/

/**
 * @swagger
 * /api/auth/login:
 *   post:
 *     tags: [Auths]
 *     summary: Authenticate a user
 *     description: Log user into the server
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               email:
 *                 type: string
 *               password:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/auth/signup:
 *   post:
 *     tags: [Auths]
 *     summary: Register a user
 *     description:  Save User information to Database
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               email:
 *                 type: string
 *               password:
 *                 type: string
 *               userType:
 *                 type: string
 *               fullName:
 *                 type: string
 *               phone:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/auth/refresh-token:
 *   post:
 *     tags: [Auths]
 *     summary: Request new token
 *     description:  Send user a new token
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               refreshToken:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/auth/verify-token:
 *   post:
 *     tags: [Auths]
 *     summary: Verify User email to verify user
 *     description:  Confirm email is valid
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               token:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User verified successfully.
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/auth/send-email-verify-token:
 *   post:
 *     tags: [Auths]
 *     summary: Resend verification email
 *     description: Resend email verification
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               email:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/auth/update-password:
 *   post:
 *     tags: [Auths]
 *     summary: Update user password
 *     description: User can change thier password
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               prevPassword:
 *                 type: string
 *               newPassword:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/auth/generate-change-password-link:
 *   post:
 *     tags: [Auths]
 *     summary: Generate password change verification  link
 *     description: Send link to user email for password change
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               email:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/**
 * @swagger
 * /api/auth/change-password:
 *   post:
 *     tags: [Auths]
 *     summary: Change user password
 *     description: Change user password to the new one
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               token:
 *                 type: string
 *               newPassword:
 *                 type: string
 *     responses:
 *       '200':
 *         description: User updated successfully.
 *       '400':
 *         description: Bad request.
 *       '500':
 *         description: Server error.
 */

/* ====================
       Trip ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/trips:
 *   get:
 *     tags: [Trip]
 *     summary: Get all trip or email (admin role only)
 *     description: Retrieve a list of all trip details.
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: email
 *         in: query
 *         description: email of the users to filter by
 *         required: false
 *         example: email@gmail.com
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: A list of users.
 */

/**
 * @swagger
 * /api/trips/confirm-otp:
 *   post:
 *     tags: [Trip]
 *     summary: Confirm otp for trip mode
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               bookingId:
 *                 type: string
 *               otpCode:
 *                 type: string
 *               tripMode:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Successfully calculated
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/* ====================
       Admin ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/admin/settings:
 *   get:
 *     tags: [Admin]
 *     summary: Get admin settings
 *     description: Retrieve a admin setting
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       '200':
 *         description: Successfully retirved admin setting.
 */

/**
 * @swagger
 * /api/admin/settings:
 *   put:
 *     tags: [Admin]
 *     summary: Update Admin settings
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               pricePerKm:
 *                 type: number
 *               driverWalletPercentage:
 *                 type: number
 *                 example: 0.15
 *               userPenalizeRate:
 *                 type: number
 *                 example: 0.15
 *               businessCommission:
 *                 type: number
 *                 example: 0.15
 *     responses:
 *       '200':
 *         description: Successfully updated
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/admin/car-status:
 *   put:
 *     tags: [Admin]
 *     summary: Update Car isVerify or isActive status
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               isVerify:
 *                 type: boolean
 *               isActive:
 *                 type: boolean
 *               carId:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Successfully updated
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/admin/request-payments:
 *   get:
 *     tags: [Admin]
 *     summary: Get all requested payments
 *     description: Retrieve a admin requested payments
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       '200':
 *         description: Successfully retirved admin setting.
 */

/**
 * @swagger
 * /api/admin/confirm-payment/{id}:
 *   put:
 *     tags: [Admin]
 *     summary: Update request payment status to completed
 *     description: Update confirmation of payment request
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - name: id
 *         in: path
 *         description: Request payment id
 *         required: true
 *         schema:
 *           type: string
 *     responses:
 *       '200':
 *         description: booking found
 *       '404':
 *         description: booking not found
 */

/* ==========================
    NOTIFICATION END POINTS
   ===========================
*/

/**
 * @swagger
 * /api/notification/email:
 *   post:
 *     tags: [Notification]
 *     summary: Sending user notification (email/sms)
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               subject:
 *                 type: string
 *               recipient:
 *                 type:string
 *               templateName:
 *                 type: string
 *               data:
 *                 type: object
 *
 *     responses:
 *       '200':
 *         description: Email sent successfully.
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/notification/sendUserOtp:
 *   post:
 *     tags: [Notification]
 *     summary: Sending user notification (sms)
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               phoneNumber:
 *                 type:string
 *               channel:
 *                 type: string
 *                 example: "sms"
 *     responses:
 *       '200':
 *         description: Otp sent successfully.
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/**
 * @swagger
 * /api/notification/verifyUserOtp:
 *   post:
 *     tags: [Notification]
 *     summary: Verify user notification (sms)
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               phoneNumber:
 *                 type:string
 *               otpCode:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Otp sent successfully.
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/* ====================
     UPLOAD ENDPOINTS
   ====================
*/

/**
 * @swagger
 * /api/upload-image:
 *   post:
 *     tags: [Upload]
 *     summary: Upload image and  Get imgae link
 *     description: Upload image
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               blob:
 *                 type: string
 *               filename:
 *                 type: string
 *     responses:
 *       '200':
 *         description: image upload Successfully
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */

/* ==========================
      DISTANCE END POINTS
   ===========================
*/

/**
 * @swagger
 * /api/distance/calculate-distance-amount:
 *   post:
 *     tags: [Distance]
 *     summary: Calculate distance amount from source to destination
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               sourceCoordinate:
 *                 type: object
 *                 properties:
 *                   latitude:
 *                     type: number
 *                   longitude:
 *                     type: number
 *                 required:
 *                   - latitude:
 *                   - longitude:
 *               destinationCoordinate:
 *                 type: object
 *                 properties:
 *                   latitude:
 *                     type: number
 *                   longitude:
 *                     type: number
 *                 required:
 *                   - latitude:
 *                   - longitude:
 *               amountPerKilo:
 *                 type: number
 *     responses:
 *       '200':
 *         description: Successfully calculated
 *       '400':
 *         description: Bad request.
 *       '401':
 *         description: unauthorized.
 */
