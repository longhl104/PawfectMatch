# PawfectMatch Deployment Scripts

This directory contains scripts for deploying and managing the PawfectMatch infrastructure.

## Scripts

### `deploy.sh`

Interactive deployment script for the PawfectMatch CDK infrastructure.

**Usage:**

```bash
./scripts/deploy.sh
```

**Features:**

- Interactive AWS SSO profile selection
- Automatic AWS SSO login
- Credential verification
- CDK bootstrap check
- Build and deployment with confirmation steps
- Stack outputs display

**Prerequisites:**

- AWS CLI v2 installed
- Node.js and npm installed
- AWS SSO configured with profiles:
  - `longhl104` (Development)
  - `longhl104_production` (Production)

**Example:**

```bash
cd /Volumes/T7Shield/Projects/PawfectMatch
./scripts/deploy.sh
```

The script will:

1. Prompt you to select an AWS profile
2. Perform AWS SSO login
3. Build the CDK project
4. Show deployment diff
5. Deploy all stacks
6. Display stack outputs
