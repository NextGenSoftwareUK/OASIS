const express = require('express');
const driverSignalMetrics = require('../utils/driverSignalMetrics');

const router = express.Router();

router.get('/driver-signal', (req, res) => {
  res.json({
    success: true,
    metrics: driverSignalMetrics.getSnapshot(),
  });
});

module.exports = router;

