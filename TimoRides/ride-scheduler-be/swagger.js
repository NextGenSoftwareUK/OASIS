require('dotenv').config({ path: './config/.env' });
const swaggerJsdoc = require('swagger-jsdoc');

const options = {
  definition: {
    openapi: '3.1.0',
    info: {
      title: 'LogRocket Express API with Swagger',
      version: '0.1.0',
      description:
        'This is a simple CRUD API application made with Express and documented with Swagger',
      license: {
        name: 'MIT',
        url: 'https://spdx.org/licenses/MIT.html',
      },
      contact: {
        name: 'LogRocket',
        url: 'https://logrocket.com',
        email: 'info@email.com',
      },
    },

    servers: [
      {
        url: process.env.BASE_SERVER_URL,
      },
    ],
  },
  apis: ['./swaggerDoc.js'],
};

const specs = swaggerJsdoc(options);

module.exports = specs;
