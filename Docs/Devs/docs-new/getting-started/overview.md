# Getting Started with OASIS

## 🚀 Quick Start

Get up and running with the OASIS API in 5 minutes.

### Step 1: Create Your Account

First, register a new avatar:

```http
POST http://api.oasisweb4.com/api/avatar/register
Content-Type: application/json

{
  "username": "myusername",
  "email": "myemail@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": false
  },
  "isError": false,
  "message": "Avatar registered successfully. Please check your email to verify your account."
}
```

### Step 2: Verify Your Email

Check your email for a verification token, then verify your account:

```http
GET http://api.oasisweb4.com/api/avatar/verify-email?token=YOUR_VERIFICATION_TOKEN
```

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Email verified successfully"
}
```

### Step 3: Authenticate

Login to get your JWT token:

```http
POST http://api.oasisweb4.com/api/avatar/authenticate
Content-Type: application/json

{
  "username": "myusername",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here"
  },
  "isError": false,
  "message": "Authentication successful"
}
```

### Step 4: Make Your First API Call

Use your JWT token to make authenticated requests:

```http
GET http://api.oasisweb4.com/api/avatar/{avatarId}
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📚 Next Steps

- [Complete Authentication Guide](authentication.md) - Detailed authentication documentation
- [Your First API Call](first-api-call.md) - Step-by-step API usage
- [WEB4 OASIS API Quick Start](../web4-oasis-api/overview.md) - Data aggregation layer
- [WEB5 STAR API Quick Start](../web5-star-api/overview.md) - Gamification layer

---

## 🔗 Resources

- **Swagger UI:** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)
- **Postman Collection:** [Download Postman Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)
- **Base URL:** `http://api.oasisweb4.com/api`

---

*Last Updated: January 24, 2026*
