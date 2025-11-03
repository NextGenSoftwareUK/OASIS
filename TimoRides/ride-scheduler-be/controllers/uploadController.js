require('dotenv').config({ path: '../config/.env' });

const cloudinary = require('cloudinary').v2;

cloudinary.config({
  cloud_name: process.env.CLOUDINARY_NAME,
  api_key: process.env.CLOUDINARY_KEY,
  api_secret: process.env.CLOUDINARY_secret,
});

async function uploadImage(req, res) {
  const { blob, filename } = req.body;

  try {
    await cloudinary.uploader.upload(
      `${blob}`,
      { public_id: filename },
      function (error, result) {
        if (error) {
          res.json({ error: error.message });
        } else {
          res.json({ imgUrl: result.url });
        }
      }
    );
  } catch (error) {
    res.json({ message: error.message });
  }
}

module.exports = {
  uploadImage,
};
