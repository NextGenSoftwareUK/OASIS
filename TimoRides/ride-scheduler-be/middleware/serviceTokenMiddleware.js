function serviceTokenMiddleware(req, res, next) {
  const expectedToken = process.env.SERVICE_DRIVER_ACTION_TOKEN;

  if (!expectedToken) {
    return res
      .status(500)
      .json({ message: 'Service token not configured on server' });
  }

  const providedToken = req.headers['x-service-token'];

  if (!providedToken || providedToken !== expectedToken) {
    return res.status(401).json({ message: 'Invalid service token' });
  }

  return next();
}

module.exports = serviceTokenMiddleware;

