# OASIS API

This package is for client applications to connect and work with the OASIS API
Various oasis-api functions and services can be accessed with this API such as Authentication, Karma, Avatars.

Each function call you make returns a promise of an object which is in this form:
`{error: boolean, data: object}`.

*object values*
*error:* true when response from api fails
*data:* response gotten from server.

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
    - [update](#update)
    - [delete](#delete)
    - [addKarma](#addkarma)
    - [removeKarma](#removekarma)
  - [Data](#data)
    - [loadHolon](#loadholon)
    - [loadAllHolons](#loadallholons)
    - [saveHolon](#saveholon)
    - [deleteHolon](#deleteholon)
  - [Karma](#karma)
    - [getKarmaForAvatar](#getkarmaforavatar)
    - [removeKarmaForAvatar](#removekarmaforavatar)
    - [getKarmaAkashicRecordsForAvatar](#getkarmaakashicrecordsforavatar)
  - [Holochain](#holochain)
    - [getHolochainAgentIdForAvatar](#getholochainagentidforavatar)
    - [getHolochainAgentPrivateKeyForAvatar](#getholochainagentprivatekeyforavatar)
    - [getAvatarIdForHolochainAgentId](#getavataridforholochainagentid)
    - [getHoloFuelBalanceForAgentId](#getholofuelbalanceforagentid)
    - [getHoloFuelBalanceForAvatar](#getholofuelbalanceforavatar)
    - [getHoloFuelBalanceForAvatar](#getholofuelbalanceforavatar-1)
  - [NFT](#nft)
    - [createPurchase](#createpurchase)
    - [getOlandPrice](#getolandprice)
    - [purchaseOLAND](#purchaseoland)
  - [Solona](#solona)
    - [mint](#mint)
    - [exchange](#exchange)

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

oasisAuth.login({
  email: "email@test.com",
  password: "testpass",
}).then((res)=>{
  if(res.error){
    // Error
  }
  else // No error
}).catch((err)=>{
  console.log(err)
})
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
import { Avatar } from "oasis-api"
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

### update
This updates the avatar with the given ID. User must be logged in & authenticated for this method to work.

```js
avatar.update(data, id).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
the parameter **data** should be of this shape
```js{
  "title": "string",
  "firstName": "string",
  "lastName": "string",
  "avatarType": "string",
  "email": "user@example.com",
  "password": "string",
  "confirmPassword": "string"
}
```

### delete
This updates the avatar with the given ID. User must be logged in & authenticated for this method to work.
```js
avatar.delete(id).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```

### addKarma
Adds karma to avatar. User must be logged in & authenticated for this method to work.
```js
avatar.addKarma(id, data).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Avatar id
*data schema*
```js{
  "karmaType": "string",
  "karmaSourceType": "string",
  "karamSourceTitle": "string",
  "karmaSourceDesc": "string"
}
```

### removeKarma
Removes karma to avatar. User must be logged in & authenticated for this method to work.
```js
avatar.removeKarma(id, data).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Avatar id
*data schema*
```js{
  "karmaType": "string",
  "karmaSourceType": "string",
  "karamSourceTitle": "string",
  "karmaSourceDesc": "string"
}
```

## Data

### loadHolon
Load's a holon data object for the given id.

```js
const data = new oasis.Data()
data.loadHolon(id).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Holon id

### loadAllHolons
Load's all holon data object for the given id.

```js
const data = new oasis.Data()
data.loadAllHolons().then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```

### saveHolon
saves holon data object.

```js
const data = new oasis.Data()
data.saveHolon(data).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```

### deleteHolon
deletes a holon data object for the given id.

```js
const data = new oasis.Data()
data.deleteHolon(id).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Holon id

## Karma

### getKarmaForAvatar
gets karma value of an avatar

```js
const karma = new oasis.Karma()
karma.getKarmaForAvatar(id).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Avatar id

### removeKarmaForAvatar
removes karma value of an avatar

```js
const karma = new oasis.Karma()
karma.getKarmaForAvatar(id, data).then(()=>{
  //pass
}).catch((err)=>{
  // pass
})
```
**params**
*id*: Avatar id
*data schema*:
```js
const data={
      karmaType: string,
      karmaSourceType: string,
      karamSourceTitle: string,
      karmaSourceDesc: string,
}
```

### getKarmaAkashicRecordsForAvatar

`oasis.Karma.getKarmaAkashicRecordsForAvatar(id)`
**params**
*id*: Avatar id


## Holochain

### getHolochainAgentIdForAvatar

`oasis.Holochain.getHolochainAgentIdForAvatar(id)`
**params**
*id*: holochain id

### getHolochainAgentPrivateKeyForAvatar

`oasis.Holochain.getHolochainAgentPrivateKeyForAvatar(id)`
**params**
*id*: avatar id

### getAvatarIdForHolochainAgentId

`oasis.Holochain.getAvatarIdForHolochainAgentId(id)`
**params**
*id*: agent id

### getHoloFuelBalanceForAgentId

`oasis.Holochain.getHoloFuelBalanceForAgentId(id)`
**params**
*id*: agent id

### getHoloFuelBalanceForAvatar

`oasis.Holochain.getHoloFuelBalanceForAvatar(id)`
**params**
*id*: avatar id

### getHoloFuelBalanceForAvatar

`oasis.Holochain.getHoloFuelBalanceForAvatar(data)`
**params**
*data schema*: `{agentId: string, avatarId: string}`
 
 ## NFT

 ### createPurchase

 `oasis.NFT.createPurchase(data)`
 **params**
 *data schema*
 ```
 nftProvider: number,
      solanaExchange: {
        fromAccount: {
          publicKey: string,
        },
        toAccount: {
          publicKey: string,
        },
        memoText: string,
        amount: number,
        mintAccount: {
          publicKey: string,
        },
      },
      cargoExchange: {
        saleId: string,
      },
    }
```

### getOlandPrice
`oasis.NFT.createPurchase(count, couponCode)`

### purchaseOLAND
`oasis.NFT.purchaseOLAND(data)`
**params**
 *data schema*
 ```
 {
      olandId: "",
      avatarId: "",
      avatarUsername: "",
      tiles: "",
      walletAddress: "",
      cargoSaleId: "",
}
```

## Solona

### mint
`oasis.Solana.mint(data)`

### exchange
`oasis.Solana.exchange(data)`
**params**
*data schema* 
```
{
      fromAccount: {
        publicKey: "",
      },
      toAccount: {
        publicKey: "",
      },
      memoText: "",
      amount: 0,
      mintAccount: {
        publicKey: "",
      },
    }
```