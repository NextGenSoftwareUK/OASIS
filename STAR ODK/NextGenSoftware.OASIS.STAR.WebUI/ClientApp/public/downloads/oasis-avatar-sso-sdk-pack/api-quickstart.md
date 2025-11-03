# Avatar API Quickstart

## Authentication
```http
POST /api/avatar/authenticate
{
  "username": "@starnet_dev",
  "password": "secret123"
}
```

## Fetch Avatar Profile
```http
GET /api/avatar/profile
Authorization: Bearer <token>
```

## List Active Sessions
```http
GET /api/avatar/sessions
Authorization: Bearer <token>
```

## Beam Out (Remote Logout)
```http
POST /api/avatar/sessions/beam-out
Authorization: Bearer <token>
{
  "sessionIds": ["all"]
}
```

## Provider Replication Policy
```http
POST /api/avatar/providers
Authorization: Bearer <token>
{
  "primary": "Auto",
  "replicas": ["IPFSOASIS", "EthereumOASIS"]
}
```

