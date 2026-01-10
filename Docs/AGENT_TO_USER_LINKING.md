# Agent-to-User Linking Feature

## Overview

The Agent-to-User Linking feature allows User-type avatars to own and manage Agent-type avatars. When a verified user creates an agent, the agent is automatically verified (up to a configurable limit), eliminating the need for email verification for agents.

## Key Features

### 1. **Automatic Agent Verification**
- When a verified user creates an agent with `ownerAvatarId`, the agent is automatically verified
- No email verification required for agents owned by verified users
- Limit: 10 agents per user (configurable)

### 2. **Ownership Management**
- Link agents to users during registration or after creation
- Unlink agents from owners when needed
- Query agents by owner
- Query owner of an agent

### 3. **Agent Card Integration**
- Agent Cards now include `OwnerAvatarId` field
- Owner information is included in agent metadata
- Enables discovery of agents by owner

## API Endpoints

### Registration with Owner

**Endpoint:** `POST /api/avatar/register`

**Request Body:**
```json
{
  "username": "my_agent",
  "email": "agent@example.com",
  "password": "password123",
  "confirmPassword": "password123",
  "firstName": "Agent",
  "lastName": "One",
  "avatarType": "Agent",
  "ownerAvatarId": "123e4567-e89b-12d3-a456-426614174000",
  "acceptTerms": true
}
```

**Response:**
```json
{
  "result": {
    "id": "agent-id-here",
    "username": "my_agent",
    "email": "agent@example.com",
    "avatarType": {
      "name": "Agent"
    },
    "verified": "2026-01-10T12:00:00Z",
    "isVerified": true
  },
  "message": "Agent avatar created and auto-verified (owner is verified). You can now log in.",
  "isError": false
}
```

**Note:** If the owner is verified and under the agent limit, the agent will be auto-verified. Otherwise, email verification is required.

### Link Agent to User (After Registration)

**Endpoint:** `POST /api/a2a/agent/link-to-user`

**Authentication:** Required (Bearer Token - Agent must be authenticated)

**Request Body:**
```json
{
  "ownerAvatarId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Agent linked to user successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid ownerAvatarId, owner not found, owner not User type, or agent limit reached
- `401 Unauthorized`: Authentication required
- `500 Internal Server Error`: Server error

### Unlink Agent from User

**Endpoint:** `POST /api/a2a/agent/unlink-from-user`

**Authentication:** Required (Bearer Token - Agent must be authenticated)

**Response:**
```json
{
  "success": true,
  "message": "Agent unlinked from owner successfully"
}
```

### Get Agents by Owner

**Endpoint:** `GET /api/a2a/agents/by-owner/{ownerAvatarId?}`

**Authentication:** Required (Bearer Token)

**Parameters:**
- `ownerAvatarId` (optional): Owner avatar ID. If not provided, defaults to authenticated user.

**Response:**
```json
[
  "agent-id-1",
  "agent-id-2",
  "agent-id-3"
]
```

**Note:** Users can only query their own agents unless they are a Wizard.

### Get Agent Owner

**Endpoint:** `GET /api/a2a/agent/{agentId?}/owner`

**Authentication:** Required (Bearer Token)

**Parameters:**
- `agentId` (optional): Agent avatar ID. If not provided, defaults to authenticated agent.

**Response:**
```json
{
  "ownerAvatarId": "123e4567-e89b-12d3-a456-426614174000",
  "message": "Agent is owned by user 123e4567-e89b-12d3-a456-426614174000"
}
```

**Response (No Owner):**
```json
{
  "ownerAvatarId": null,
  "message": "Agent has no owner"
}
```

## Workflow Examples

### Example 1: Create Agent with Owner (Auto-Verified)

1. **User Registration** (if not already registered):
   ```bash
   POST /api/avatar/register
   {
     "username": "john_doe",
     "email": "john@example.com",
     "password": "password123",
     "avatarType": "User",
     ...
   }
   ```

2. **Verify User Email** (if not already verified):
   ```bash
   GET /api/avatar/verify-email?token=<verification-token>
   ```

3. **Create Agent with Owner**:
   ```bash
   POST /api/avatar/register
   {
     "username": "data_analyst_agent",
     "email": "agent1@example.com",
     "password": "password123",
     "avatarType": "Agent",
     "ownerAvatarId": "<user-avatar-id>",
     ...
   }
   ```
   
   **Result:** Agent is automatically verified and ready to use!

### Example 2: Link Existing Agent to User

1. **Create Agent** (without owner):
   ```bash
   POST /api/avatar/register
   {
     "username": "my_agent",
     "avatarType": "Agent",
     ...
   }
   ```

2. **Authenticate as Agent**:
   ```bash
   POST /api/avatar/authenticate
   {
     "username": "my_agent",
     "password": "password123"
   }
   ```

3. **Link to User**:
   ```bash
   POST /api/a2a/agent/link-to-user
   Authorization: Bearer <agent-jwt-token>
   {
     "ownerAvatarId": "<user-avatar-id>"
   }
   ```

### Example 3: Query User's Agents

1. **Authenticate as User**:
   ```bash
   POST /api/avatar/authenticate
   {
     "username": "john_doe",
     "password": "password123"
   }
   ```

2. **Get All User's Agents**:
   ```bash
   GET /api/a2a/agents/by-owner
   Authorization: Bearer <user-jwt-token>
   ```

## Implementation Details

### Data Storage

Owner information is stored in the agent avatar's `MetaData` dictionary:
```csharp
agent.MetaData["OwnerAvatarId"] = ownerAvatarId.ToString();
agent.MetaData["OwnerLinkedDate"] = DateTime.UtcNow.ToString("O");
```

### Auto-Verification Logic

The auto-verification happens in `AvatarRegistered` method:

1. Check if avatar is Agent type
2. Check if `OwnerAvatarId` exists in metadata
3. Load owner avatar
4. Verify owner is verified (`IsVerified == true`)
5. Check agent limit (default: 10 agents per user)
6. If all conditions met:
   - Set `Verified = DateTime.UtcNow`
   - Clear `VerificationToken`
   - Save avatar
   - Return success message

### Agent Limit

- **Default Limit:** 10 agents per user
- **Configurable:** Can be changed via `OASIS_DNA` (TODO: Add configuration)
- **Enforcement:** Checked during linking and registration

### Permission Model

- **Users:** Can only query their own agents
- **Wizards:** Can query any user's agents
- **Agents:** Can link/unlink themselves, query their own owner

## Agent Card Integration

When retrieving an Agent Card, owner information is automatically included:

```json
{
  "agentId": "agent-id",
  "name": "my_agent",
  "ownerAvatarId": "user-id",
  "metadata": {
    "owner_avatar_id": "user-id",
    ...
  }
}
```

## Error Handling

### Common Errors

1. **Agent Limit Reached:**
   ```
   Error: "User {ownerId} has reached the maximum limit of 10 agents"
   ```

2. **Owner Not Verified:**
   ```
   Agent will require email verification (normal flow)
   ```

3. **Invalid Owner Type:**
   ```
   Error: "Avatar {ownerId} is not a User type. Only User avatars can own agents."
   ```

4. **Owner Not Found:**
   ```
   Error: "Owner avatar {ownerId} not found"
   ```

## Configuration

### Making Agent Limit Configurable

To make the agent limit configurable, add to `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "Agents": {
      "MaxAgentsPerUser": 10
    }
  }
}
```

Then update `AgentManager-Ownership.cs` and `AvatarManager-Private.cs` to read from `OASISDNA`.

## Testing

### Test Scenarios

1. **Create agent with verified owner** → Should auto-verify
2. **Create agent with unverified owner** → Should require email verification
3. **Create agent without owner** → Should require email verification
4. **Link agent to verified user** → Should auto-verify agent
5. **Link agent to unverified user** → Agent remains unverified
6. **Reach agent limit** → Should reject new agent creation
7. **Query agents by owner** → Should return correct list
8. **Unlink agent** → Should remove ownership

## Future Enhancements

1. **Configurable Agent Limit:** Make limit configurable via OASIS_DNA
2. **Agent Transfer:** Allow transferring agents between users
3. **Agent Sharing:** Allow multiple users to share agent access
4. **Agent Permissions:** Fine-grained permissions for agent operations
5. **Agent Analytics:** Track agent usage by owner

## Related Files

- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Agent/IAgentCard.cs` - Interface definition
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-Ownership.cs` - Ownership management
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AgentManager/AgentManager-AgentCard.cs` - Agent Card retrieval
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/AvatarManager-Private.cs` - Auto-verification logic
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs` - API endpoints
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs` - Registration endpoint
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Models/A2A/LinkAgentToUserRequest.cs` - Request model
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Models/Security/RegisterRequest.cs` - Registration model

## Summary

The Agent-to-User Linking feature provides a seamless way for verified users to create and manage agents without email verification overhead. It maintains security through ownership tracking and limits while providing a better user experience for agent creation and management.
