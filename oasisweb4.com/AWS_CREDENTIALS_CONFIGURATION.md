# AWS Credentials Configuration Guide for Asset Rail

## Summary

**AWS credentials are NOT stored in the repository** (as expected for security best practices). The deployment scripts use AWS CLI commands that require credentials to be configured externally.

---

## AWS Deployment Files Found

### 1. Frontend Deployment Script
**Location:** `deployment/frontend-build/deploy.sh`

This script deploys the frontend to AWS ECS and uses:
- **ECR (Elastic Container Registry)**: `881490134703.dkr.ecr.us-east-1.amazonaws.com/assetrail-frontend`
- **ECS Cluster**: `oasis-api-cluster`
- **ECS Service**: `assetrail-frontend-service`
- **AWS Region**: `us-east-1`
- **AWS Account ID**: `881490134703`

**Key AWS CLI commands used:**
```bash
# Line 34: ECR login (requires AWS credentials)
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Lines 41-44: ECS service update (requires AWS credentials)
aws ecs update-service \
    --cluster $ECS_CLUSTER \
    --service $ECS_SERVICE \
    --force-new-deployment
```

### 2. Smart Contract Generator API Task Definition
**Location:** `smart-contract-generator/scgen-api-task-definition.json`

This ECS task definition references:
- **ECR Image**: `881490134703.dkr.ecr.us-east-1.amazonaws.com/scgen-api:latest`
- **IAM Execution Role**: `arn:aws:iam::881490134703:role/ecsTaskExecutionRole`
- **IAM Task Role**: `arn:aws:iam::881490134703:role/ecsTaskRole`
- **CloudWatch Logs Group**: `/ecs/scgen-api`

### 3. Docker Build Guide
**Location:** `smart-contract-generator/DOCKER_BUILD_GUIDE.md`

Contains AWS ECS deployment instructions (lines 290-299) but does not detail credential setup.

---

## How to Configure AWS Credentials

The deployment scripts rely on AWS CLI being configured. You have **three options**:

### Option 1: AWS CLI Configure (Recommended for Local Development)

```bash
# Install AWS CLI if not already installed
# macOS: brew install awscli
# Linux: sudo apt-get install awscli
# Or download from: https://aws.amazon.com/cli/

# Configure credentials
aws configure

# You'll be prompted for:
# - AWS Access Key ID: [Your access key]
# - AWS Secret Access Key: [Your secret key]
# - Default region name: us-east-1
# - Default output format: json
```

**Where to get credentials:**
1. Log into AWS Console: https://console.aws.amazon.com
2. Navigate to IAM → Users → [Your User] → Security Credentials
3. Create Access Key (if you don't have one)
4. Download or copy the Access Key ID and Secret Access Key

### Option 2: Environment Variables

Set these environment variables before running deployment scripts:

```bash
export AWS_ACCESS_KEY_ID="your-access-key-id"
export AWS_SECRET_ACCESS_KEY="your-secret-access-key"
export AWS_DEFAULT_REGION="us-east-1"

# Then run the deployment script
./deployment/frontend-build/deploy.sh
```

### Option 3: AWS IAM Roles (For EC2/ECS Instances)

If running from an EC2 instance or ECS task, use IAM instance roles instead of storing credentials.

---

## Required AWS Permissions

To deploy using the scripts, your AWS user/role needs these permissions:

### For ECR (Elastic Container Registry):
- `ecr:GetAuthorizationToken`
- `ecr:BatchCheckLayerAvailability`
- `ecr:GetDownloadUrlForLayer`
- `ecr:BatchGetImage`
- `ecr:PutImage`
- `ecr:InitiateLayerUpload`
- `ecr:UploadLayerPart`
- `ecr:CompleteLayerUpload`

### For ECS (Elastic Container Service):
- `ecs:UpdateService`
- `ecs:DescribeServices`
- `ecs:DescribeTasks`
- `ecs:ListTasks`

### For CloudWatch Logs (ECS tasks):
- `logs:CreateLogGroup` (if log group doesn't exist)
- `logs:CreateLogStream`
- `logs:PutLogEvents`

**Recommended IAM Policy:**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:PutImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:UpdateService",
        "ecs:DescribeServices",
        "ecs:DescribeTasks",
        "ecs:ListTasks"
      ],
      "Resource": [
        "arn:aws:ecs:us-east-1:881490134703:cluster/oasis-api-cluster",
        "arn:aws:ecs:us-east-1:881490134703:service/oasis-api-cluster/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:us-east-1:881490134703:*"
    }
  ]
}
```

---

## IAM Roles Referenced in Task Definitions

The ECS task definition references these IAM roles that must exist in AWS:

1. **Execution Role**: `arn:aws:iam::881490134703:role/ecsTaskExecutionRole`
   - Allows ECS to pull images from ECR and write logs to CloudWatch
   - Needs: `AmazonECSTaskExecutionRolePolicy` + ECR read permissions

2. **Task Role**: `arn:aws:iam::881490134703:role/ecsTaskRole`
   - Allows the containerized application to access AWS services
   - Permissions depend on what your application needs

**To verify/create these roles:**
1. AWS Console → IAM → Roles
2. Search for `ecsTaskExecutionRole` and `ecsTaskRole`
3. If missing, create them with appropriate policies

---

## Testing AWS Configuration

Before running deployment scripts, verify your AWS credentials are configured:

```bash
# Test AWS CLI access
aws sts get-caller-identity

# Should return something like:
# {
#     "UserId": "AIDA...",
#     "Account": "881490134703",
#     "Arn": "arn:aws:iam::881490134703:user/your-username"
# }

# Test ECR access
aws ecr describe-repositories --region us-east-1

# Test ECS access
aws ecs describe-clusters --clusters oasis-api-cluster --region us-east-1
```

---

## Regenerating AWS Credentials

If you need to regenerate or reconfigure credentials:

1. **For IAM User Access Keys:**
   - AWS Console → IAM → Users → [Your User] → Security Credentials
   - Delete old access keys (if compromised)
   - Create new access key
   - Update your local configuration:
     ```bash
     aws configure
     # Enter new credentials when prompted
     ```

2. **For IAM Roles (ECS Tasks):**
   - AWS Console → IAM → Roles → [Role Name] → Trust Relationships
   - Review and update trust policies if needed
   - Update permissions in the Permissions tab

3. **For ECR Access:**
   - Ensure your IAM user/role has ECR permissions (see above)
   - ECR repositories are account-scoped, so access is controlled via IAM

---

## Files That Reference AWS Configuration

1. **`deployment/frontend-build/deploy.sh`** - Main deployment script
2. **`smart-contract-generator/scgen-api-task-definition.json`** - ECS task definition
3. **`smart-contract-generator/DOCKER_BUILD_GUIDE.md`** - Deployment documentation (lines 290-299)

---

## Security Best Practices

✅ **DO:**
- Use IAM roles instead of access keys when possible (e.g., on EC2/ECS)
- Rotate access keys regularly
- Use least-privilege IAM policies
- Store secrets in AWS Secrets Manager or Parameter Store (not in code)
- Use AWS profiles for multiple accounts

❌ **DON'T:**
- Commit AWS credentials to git
- Share access keys via email/chat
- Use root account credentials for deployments
- Hardcode credentials in scripts

---

## Next Steps

1. **Verify AWS Account Access**: Ensure you have access to AWS account `881490134703`
2. **Configure AWS CLI**: Run `aws configure` with your credentials
3. **Test Access**: Run the test commands above
4. **Deploy**: Run `./deployment/frontend-build/deploy.sh` when ready

---

## Troubleshooting

**Error: "Unable to locate credentials"**
- Run `aws configure` to set up credentials
- Or set `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` environment variables

**Error: "Access Denied"**
- Check IAM permissions (see Required AWS Permissions above)
- Verify you're using the correct AWS account (881490134703)

**Error: "Repository does not exist"**
- Create the ECR repository: `aws ecr create-repository --repository-name assetrail-frontend --region us-east-1`
- Or verify the repository name matches in the script

**Error: "Cluster not found"**
- Verify cluster exists: `aws ecs describe-clusters --clusters oasis-api-cluster --region us-east-1`
- Create cluster if needed: `aws ecs create-cluster --cluster-name oasis-api-cluster --region us-east-1`

