const Dotenv = require("dotenv-webpack");
const path = require("path");

module.exports = {
  plugins: [
    new Dotenv({
      path: path.resolve(
        __dirname,
        `./.env.${process.env.NODE_ENV || "development"}`
      ),
    }),
  ],
  devServer: {
    proxy: [
      {
        context: ["/v3/banks/*"],
        target: "https://api.flutterwave.com",
        secure: false,
        logLevel: "debug",
        changeOrigin: true,
      },
    ],
  },
};
