require('dotenv').config({ path: './config/.env' });
const express = require('express');
const cors = require('cors');
const mongoose = require('mongoose');
const routes = require('./routes');
const specs = require('./swagger');
const swaggerUi = require('swagger-ui-express');
const requestContextMiddleware = require('./middleware/requestContextMiddleware');
const { errors: celebrateErrors } = require('celebrate');

const app = express();

// Trust proxy for rate limiting behind ngrok/reverse proxy
app.set('trust proxy', 1);

// Site Accessable Url
const allowedOrigins =
  process.env.NODE_ENV === 'development'
    ? [
        'http://localhost:4205',
        'http://localhost:4202',
        'https://timoridesolutions.space',
        'https://timorides-scheduler.onrender.com',
        'https://ride-scheduler-be.onrender.com',
        'https://ride-scheduler-fe.onrender.com',
        'https://ride-scheduler-be-live.onrender.com',
      ]
    : ['https://timorides.com', 'https://timoridesolutions.space'];

// Cors option logic
const corsOptions = {
  origin: (origin, callback) => {
    // Allow requests with no origin (like mobile apps or curl requests) or from allowed origins
    if (!origin || allowedOrigins.includes(origin)) {
      callback(null, true);
    } else {
      console.log(' Cors Error ---');
      callback(
        new Error(
          'The CORS policy for this site does not allow access from the specified Origin.',
          origin
        )
      );
    }
  },
};

app.use(cors(corsOptions));

// Swagger Doc call
// To prevent swagger from use in production
if (process.env.NODE_ENV !== 'production') {
  app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(specs));
}

app.use(
  express.json({
    limit: '50mb',
    verify: (req, res, buf) => {
      req.rawBody = buf?.toString() || '';
    },
  })
);
app.use(express.urlencoded({ limit: '50mb', extended: true }));
app.use(requestContextMiddleware);
app.use(routes);
app.use(celebrateErrors());

// mongo db connection string
const mongoDbUrl = process.env.Database_Url;

// Connecting to mongo db
mongoose
  .connect(mongoDbUrl)
  .then(() => console.log('Database connected well !!!'))
  .catch((error) => console.log('Error in Database connection ', error));

const port = process.env.PORT || 4205;

app.listen(port, () => console.log(`server connected !!! on port ${port}`));
