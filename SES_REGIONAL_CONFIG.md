# PawfectMatch SES Regional Configuration

## Important: AWS Region Configuration

Your PawfectMatch application is configured for the **ap-southeast-2** (Asia Pacific - Sydney) region.

### Key Points:

1. **SES Service Region**: ap-southeast-2
   - All SES resources (domain identity, configuration sets) will be created in ap-southeast-2
   - This is different from many tutorials that use us-east-1

2. **CDK Deployment**: Your CDK stacks will deploy SES resources to ap-southeast-2
   - No changes needed to CDK code
   - Region is automatically determined by your AWS CLI/CDK configuration

3. **DNS Records**: The MX record for mail-from domain uses the regional SMTP endpoint:
   - `10 feedback-smtp.ap-southeast-2.amazonses.com`

4. **AWS Console**: Access SES console for ap-southeast-2:
   - https://console.aws.amazon.com/ses/home?region=ap-southeast-2

## Quick Setup for Production

```bash
# Check your current region
aws configure get region

# Should return: ap-southeast-2

# Deploy SES infrastructure
cd cdk
cdk deploy PawfectMatch-production-Shared

# Get DNS setup instructions
./scripts/setup-ses.sh setup production

# Check verification status
./scripts/setup-ses.sh check production

# Test email (after verification)
./scripts/setup-ses.sh test production
```

## Region-Specific DNS Records

When you run `./scripts/setup-ses.sh setup production`, you'll get DNS records that include:

### Mail-From Domain MX Record:
```
Type: MX
Name: mail.pawfectmatchnow.com
Value: 10 feedback-smtp.ap-southeast-2.amazonses.com
```

### SPF Record:
```
Type: TXT
Name: mail.pawfectmatchnow.com
Value: v=spf1 include:amazonses.com ~all
```

## Production Checklist

- [x] Region configured: ap-southeast-2 ✓
- [ ] CDK deployed to ap-southeast-2
- [ ] Domain verified in SES (ap-southeast-2)
- [ ] DNS records added to Route 53
- [ ] Production access requested
- [ ] Email sending tested

The setup script has been updated to use ap-southeast-2 for all AWS CLI commands.
