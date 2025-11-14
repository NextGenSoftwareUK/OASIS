function handleCatchError(error, res) {
  console.log(error, ' Error Catg ');
  if (error.statusCode) {
    return res.status(error.statusCode).json({ message: error.message });
  }
  if (error.name === 'NotFoundError' || error.status === 404) {
    return res.status(404).json({ error: 'Resource not found' });
  } else if (error.name === 'TokenExpiredError') {
    return res.status(401).json({ message: 'Token expired' });
  } else if (
    error.name === 'MongoServerError' &&
    error?.code === 11000 &&
    error?.keyPattern?.phone
  ) {
    return res.status(409).json({
      error: `The phone number ${error.keyValue.phone} is already in use. Please use a different number.`,
    });
  } else if (error.name === 'MongoServerError') {
    return res.status(401).json({ ...error });
  } else if (error.name === 'ValidationError' || error.name === 'CastError') {
    return res.status(401).json({ error });
  } else if (error.name === 'JsonWebTokenError') {
    return res.status(401).json({ ...error });
  } else if (error.name === 'TypeError') {
    return res.status(400).json({ ...error });
  } else return res.status(500).json({ error: 'Internal Server Error' });
}

module.exports = { handleCatchError };
