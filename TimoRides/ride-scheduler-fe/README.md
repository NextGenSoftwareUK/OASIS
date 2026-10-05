# RideScheduler Frontend

Welcome to RideScheduler, the frontend project for our ride scheduling app! RideScheduler is built using Angular and is responsible for providing a user-friendly interface for scheduling rides.

## Features

- **Ride Booking Form**: Users can input their ride details, including pickup location, destination, and desired time for scheduling a ride.
- **Driver List**: Display a list of available drivers near the user's location, retrieved using Google Maps integration.
- **Ride Details**: Show detailed information about the scheduled ride, including driver details, pickup time, and estimated fare.
- **Authentication**: Users can log in to the app as passengers or drivers to access ride scheduling functionality.
- **Responsive Design**: RideScheduler is designed to be responsive, ensuring a seamless user experience across different devices and screen sizes.

## Folder Structure

- **src/app/components/**: Contains Angular components specific to the ride scheduling feature.
  - **ride-form/**: Ride booking form component.
  - **driver-list/**: Component for displaying available drivers.
  - **ride-details/**: Component for displaying ride details.
- **src/app/shared/**: Shared components that can be reused across different parts of the application.
- **src/app/services/**: Angular services for data retrieval, authentication, etc.
- **src/app/models/**: Data models or interfaces used in the frontend.
- **src/app/guards/**: Route guards for protecting routes (e.g., authentication guard).
- **src/app/interceptors/**: HTTP interceptors for handling HTTP requests and responses.
- **src/app/utils/**: Utility functions and helpers.

## Getting Started

To run the frontend project locally, follow these steps:

1. Clone this repository to your local machine.
2. Navigate to the `frontend` directory.
3. Run `npm install` to install dependencies.
4. Run `ng serve` to start the development server.
5. Open your web browser and navigate to `http://localhost:4205`.

## Dependencies

- **Angular**: The frontend is built using Angular, a powerful web application framework.
- **Angular Material**: UI components library following Google's Material Design guidelines.
- **Google Maps API**: Integration for locating nearby drivers and displaying maps.
- **Node.js and npm**: Node.js is required to run the Angular development server and manage project dependencies.

## Contributing

We welcome contributions from the community! If you'd like to contribute to RideScheduler, please follow these guidelines:

- Fork the repository and create a new branch for your feature or bug fix.
- Make your changes and submit a pull request.
- Ensure your code follows the project's coding standards and conventions.
- Write unit tests for your code if applicable.
- Provide a clear and descriptive explanation of your changes in the pull request description.

## License

This project is licensed under the [MIT License](LICENSE).

# RideSchedulerFe

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 15.2.10.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4205/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.io/cli) page.

## Deployment

### Dev deployment

- build the project with development configuration flag
- push to dev branch
- PR into staging. When PR is approved, render gets updated with the demo branch (staging).

### Production deployment (AWS)

- build the project with production configuration flag
- push to dev branch
- PR into main branch
- follow these links as a guide to setup AWS EC2 for Angular deployment
  - https://www.youtube.com/watch?v=wlmRcOi1kaU
  - https://codewithmuh.medium.com/deploy-angular-project-on-aws-ec2-linux-instance-927f7eece5ff
- for future builds, consider setting up CI/CD with GitHub Actions

### How to renew SSL certificate using Certbot

- Open terminal and cd into the directory containing the .pem file created at the point of setting up the SSL previously (in my case, it's downloads folder).
- Run the following command `chmod 400 "timowebapp.pem"` where `timowebapp.pem` is the name of the `pem` file
- Run `certbot --version` to check if Certbot is installed (in my case, yes).
- Run `sudo certbot renew` to renew the SSL automatically. A smooth renewal process looks like this below:

```
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Processing /etc/letsencrypt/renewal/timorides.com.conf
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Renewing an existing certificate for timorides.com and www.timorides.com
Reloading nginx server after certificate renewal

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Congratulations, all renewals succeeded:
  /etc/letsencrypt/live/timorides.com/fullchain.pem (success)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
```
