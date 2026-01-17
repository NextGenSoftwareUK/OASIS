const express = require('express');
const { uploadImage } = require('../controllers/uploadController');
const {
  validate,
  uploadImageValidation,
} = require('../validators/uploadValidation');

const router = express.Router();

router.post('/upload-image', uploadImageValidation, validate, uploadImage);

module.exports = router;
