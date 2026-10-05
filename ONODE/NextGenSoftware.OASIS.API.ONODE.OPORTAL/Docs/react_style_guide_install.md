# Installing the Style Guide

<br> 

## Create ReactJS App
<br> 

```
npx create-react-app react-project
```
<br>

## Install ESLint
<br>

Prerequisites: Node.js(`^10.12.0` or `>=12.0.0`) built with SSL support.  

*(If you are using an official Node.js distribution, SSL is always built in.)*

<br>

You can install ESLint using npm or yarn:

```
npm install eslint --save-dev

# or

yarn add eslint --dev
```
<br>

Then, set up a configuration file, using the `--init` flag.
<br>  

```
npx eslint --init
```
<br>
<br>

After running `eslint --init`, you'll have a `.eslintrc.{js,yml,json}` file in your directory.

```
{
  "env": {
    "browser": true,
    "es2021": true
  },
  "extends": [
    "eslint:recommended",
    "plugin:react/recommended"
  ],
  "parserOptions": {
    "ecmaFeatures": {
      "jsx": true
    },
    "ecmaVersion": 12,
    "sourceType": "module"
  },
  "plugins": [
      "react"
  ],
  "rules": {
  }
}
```

<br>

## Installing Prettier

<br>

```
npm i prettier eslint-config-prettier eslint-plugin-prettier -D
```
<br>

Then add Prettier to the "extends" array in your `.eslintrc` file
```
  "extends": [
    "eslint:recommended",
    "plugin:react/recommended",
    "airbnb",
    "plugin:prettier/recommended"
  ],
