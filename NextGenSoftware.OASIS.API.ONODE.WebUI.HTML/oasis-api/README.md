# OASIS API

This package is for client applications to connect and work with the OASIS API

- [OASIS API](#oasis-api)
  - [Getting started](#getting-started)
  - [Auth](#auth)
    - [Login](#login)
    - [Signup](#signup)
    - [Logout](#logout)
    - [Forgot password](#forgot-password)
    - [Get user](#get-user)
  - [Avatar](#avatar)
    - [get](#get)
    - [Get All](#get-all)

## Getting started

To get started with this package run

`npm install oasis-api`

This installs the package to your project.
To use it in the Application, it can be imported by

`import oasis from "oasis-api"`

## Auth

The Auth class handles the authentication of the oasis-api. It can be imported by:

`import { Auth } from "oasis-api"`

### Login

To login use this code:

```javascript
import { Auth } from "oasis-api";

const oasisAuth = new Auth();

const user = await oasisAuth.login({
  email: "email@test.com",
  password: "testpass",
});
```

### Signup

For signup, use this code snippet:

```javascript
import { Auth } from "oasis-api";

const oasisAuth = new Auth();

const data = await OasisAuth.signup({
  firstName: "test",
  lastName: "test",
  email: "test@email.com",
  password: "test",
  confirmPassword: "test",
  acceptTerms: true,
  avatarType: "User",
  title: "Mr",
});
```

### Logout

This logs a user out and revokes his token

```javascript
import { Auth } from "oasis-api";

const oasisAuth = new Auth();

const data = await OasisAuth.logout();
```

### Forgot password

```javascript
import { Auth } from "oasis-api";

const oasisAuth = new Auth();

const data = await OasisAuth.forgotPassword({ email: "test@test.com" });
```

### Get user

This function is used when trying to get a currently logged in user.
Code snippet:

```javascript
import { Auth } from "oasis-api";

const oasisAuth = new Auth();

const data = await OasisAuth.getUser();
```

## Avatar

This class manages a user's avatar from adding Karma, deleting and updating avatar, etc,

```js
import { Avatar } from "oasis-api";
const avatar = new Avatar();
```

### get

This function gets an avatar when its ID is provided

```js
import { Avatar } from "oasis-api";
const avatar = new Avatar();

const res = await avatar.get();
```

### Get All

This function returns all registered avatars

```js
import { Avatar } from "oasis-api";
const avatar = new Avatar();

const res = await avatar.getAll();
```
