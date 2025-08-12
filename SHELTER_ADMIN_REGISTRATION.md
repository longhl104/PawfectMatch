# Shelter Admin Registration Feature

This implementation adds a complete shelter admin registration system to PawfectMatch with the required minimal fields and optional fields as specified.

## What was implemented:

### Backend (Identity API)

1. **New Registration Endpoint**: `/api/registration/shelter-admin`

   - Added `ShelterAdminRegistrationRequest` model with required and optional fields
   - Added `ShelterAdminRegistrationResponse` model
   - Validation for all required fields
   - Cognito user creation with `shelter_admin` user type
   - DynamoDB storage for shelter admin profiles

2. **Database Storage**:
   - Creates shelter admin profiles in `pawfect-match-shelter-admins-{environment}` DynamoDB table
   - Stores all registration data with proper type classification

### Frontend (Identity Client)

1. **New Service**: `ShelterAdminService`

   - Handles shelter admin registration API calls
   - Type-safe interfaces for request/response

2. **New Registration Page**: `/auth/shelter-admin/register`

   - Complete form with all required and optional fields
   - Form validation matching backend requirements
   - Consistent UI/UX with existing adopter registration
   - Password strength validation
   - International phone number validation (client-side)
   - URL validation for website
   - ABN validation (11 digits)

3. **Updated Routing**:
   - Added `shelter-admin.routes.ts`
   - Updated main auth routes to include shelter admin paths
   - Updated choice component to navigate correctly

## Required Fields Implemented:

- ✅ Email address (required)
- ✅ Password + confirm password (with strong password validation)
- ✅ Shelter name (required)
- ✅ Shelter contact number (required, international format)
- ✅ Shelter address (required)

## Optional Fields Implemented:

- ✅ Website URL (optional, with URL validation)
- ✅ ABN (optional, with 11-digit validation)
- ✅ Shelter description (optional, max 1000 characters)

## Key Features:

1. **Separation of Concerns**: Shelter-admin and shelter are properly separated as different entity types
2. **Validation**: Comprehensive client and server-side validation
3. **Security**: Same authentication flow as adopters with proper user type distinction
4. **User Experience**: Consistent with existing registration flows
5. **Error Handling**: Proper error handling and user feedback

## How to test:

1. Start the Identity service: `cd Identity/Longhl104.Identity && dotnet run`
2. Start the Identity client: `cd Identity/client && npm start`
3. Navigate to `https://localhost:4200/auth/choice`
4. Click "Register as Shelter Admin"
5. Fill out the registration form with the required fields
6. Submit to create a new shelter admin account

## Next steps:

- The shelter admin will be automatically logged in and redirected to the main application
- The user will have `shelter_admin` user type in Cognito
- Profile data will be stored in DynamoDB for future shelter management features
