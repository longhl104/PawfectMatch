# GitHub Copilot Instructions for PawfectMatch

This document provides comprehensive guidance for AI coding agents working on the PawfectMatch pet adoption platform. PawfectMatch is a sophisticated microservices-based application built with ASP.NET Core, Angular, and AWS infrastructure.

## 🏗️ Architecture Overview

PawfectMatch follows a microservices architecture with three main services:

### Core Services
- **Identity Service** (`Identity/`): User authentication, registration, and profile management using AWS Cognito
- **Matcher Service** (`Matcher/`): Pet matching and adoption processes for adopters
- **ShelterHub Service** (`ShelterHub/`): Shelter administration and pet management for shelter staff

### Infrastructure
- **AWS CDK** (`cdk/`): Infrastructure as Code for multi-environment deployment
- **Shared Libraries** (`Shared/`): Common functionality, authentication middleware, and utilities
- **Client Applications**: Angular 17+ standalone applications with PrimeNG UI components

## 🔑 Critical Development Patterns

### 1. Microservices Communication

**Internal Service Communication:**
```csharp
// Use IInternalHttpClientFactory for service-to-service calls
public class UserService
{
    private readonly IInternalHttpClientFactory _httpClientFactory;
    
    public async Task<UserProfile?> GetUserAsync(string userId)
    {
        using var httpClient = _httpClientFactory.CreateClient(PawfectMatchServices.Identity);
        var response = await httpClient.GetAsync($"/api/internal/users/{userId}");
        // Handle response...
    }
}
```

**Internal Authorization:**
```csharp
// Internal-only endpoints use InternalOnly policy
[ApiController]
[Route("api/internal/[controller]")]
[Authorize(Policy = "InternalOnly")]
public class UsersController : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        // Only accessible by other services with X-Internal-API-Key header
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }
}
```

### 2. Authentication & Authorization

**Service Configuration:**
```csharp
// Program.cs - Standard authentication setup
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("adopter"); // or "shelter_admin"

// For internal communication
builder.Services.AddPawfectMatchInternalHttpClients([
    PawfectMatchServices.Identity,
    PawfectMatchServices.Matcher
]);
```

**User Context Access:**
```csharp
// Get current user in controllers
var user = HttpContext.GetCurrentUser();
var userId = HttpContext.GetCurrentUserId();
var userEmail = HttpContext.GetCurrentUserEmail();

// Check if request is internal
bool isInternal = HttpContext.IsInternalRequest();
```

### 3. Angular Client Architecture

**Standalone Components (Angular 17+):**
```typescript
@Component({
  selector: 'app-example',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,        // PrimeNG components
    CardModule,
    DataViewModule
  ],
  templateUrl: './example.component.html',
  styleUrl: './example.component.scss'
})
export class ExampleComponent {
  private api = inject(GeneratedApi);  // Use generated API clients
  private toastService = inject(ToastService);
  
  // Use signals for reactive state
  data = signal<DataType[]>([]);
}
```

**Generated API Integration:**
```typescript
// Always use the generated API clients from generated-apis.ts
export class DataService {
  constructor(private api: DataApi) {}
  
  async loadData(): Promise<DataResponse> {
    return firstValueFrom(this.api.getData());
  }
}
```

## 🛠️ Development Workflows

### Building and Running Services

**Local Development:**
```bash
# Build specific service
dotnet build Identity/Longhl104.Identity/

# Run service locally
cd Identity/Longhl104.Identity
dotnet run

# Angular client development
cd Identity/client
ng serve
```

**Using VS Code Tasks:**
```bash
# Use the configured build task
Ctrl+Shift+P → "Tasks: Run Task" → "build"
```

### Deployment Process

**Interactive Deployment:**
```bash
# Run the interactive deployment script
./scripts/deploy.sh

# Script provides menu for:
# 1. Project selection (Identity/Matcher/ShelterHub)
# 2. Environment selection (development/production)
# 3. Automatic Docker build and push
# 4. CDK stack deployment
```

**Manual CDK Operations:**
```bash
cd cdk
npm install
cdk deploy PawfectMatch-Identity-development
```

### Database and AWS Integration

**DynamoDB Access:**
```csharp
// Services use injected DynamoDB client
public class DataService
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    
    public DataService(AmazonDynamoDBClient dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }
}
```

**AWS Cognito Integration:**
```csharp
public class CognitoService : ICognitoService
{
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
    
    public async Task<(bool Success, string Message)> InitiatePasswordResetAsync(string email)
    {
        // AWS Cognito password reset implementation
    }
}
```

## 📁 File Organization Patterns

### Backend Services Structure
```
ServiceName/
├── ServiceName.sln
├── client/                    # Angular frontend
│   ├── src/app/
│   ├── src/shared/apis/generated-apis.ts  # NSwag generated APIs
│   └── environments/
├── Longhl104.ServiceName/     # ASP.NET Core project
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   └── Program.cs
└── Lambdas/                   # AWS Lambda functions
```

### Shared Library Usage
```csharp
// Always use shared library extensions
using Longhl104.PawfectMatch.Extensions;
using Longhl104.PawfectMatch.Middleware;
using Longhl104.PawfectMatch.HttpClient;
```

## 🚀 Deployment Architecture

### Multi-Environment Support
- **Development**: `.development.pawfectmatchnow.com` subdomains
- **Production**: `.pawfectmatchnow.com` subdomains

### Container Deployment
- Services run on **AWS ECS Fargate**
- Docker images built and pushed automatically via deployment scripts
- **Application Load Balancer** for HTTPS termination and routing

### Domain Configuration
- **Route 53** DNS management
- **ACM certificates** for HTTPS
- **CloudFront** distributions for Angular clients

## 🔧 Key Configuration Requirements

### Required Environment Variables
```bash
# All services need these
ASPNETCORE_ENVIRONMENT=Development|Production
InternalApiKey=your-secure-internal-api-key

# Identity service
AWS_COGNITO_USER_POOL_ID=your-pool-id
AWS_COGNITO_CLIENT_ID=your-client-id

# Email service
AWS_SES_REGION=us-east-1
```

### Angular Environment Configuration
```typescript
// environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001',  // Backend API
  identityUrl: 'https://localhost:4200'  // Identity service URL
};
```

## 🎨 UI/UX Patterns

### PrimeNG Integration
- Use **PrimeNG v18+** components consistently
- **Aura theme** preset configuration
- Standard component imports in standalone components

### Form Validation
```typescript
// Use reactive forms with custom validators
this.form = this.fb.group({
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(8)]],
  confirmPassword: ['', [Validators.required]]
}, { validators: passwordMatchValidator });
```

### State Management
- Use **Angular signals** for reactive state
- **BehaviorSubject** for service-level state management
- **firstValueFrom()** for converting Observables to Promises

## 🔍 Debugging and Testing

### Logging Patterns
```csharp
// Structured logging with context
_logger.LogInformation("User {UserId} performed action {Action} at {Timestamp}", 
    userId, action, DateTime.UtcNow);

_logger.LogError(ex, "Failed to process request for user {UserId}", userId);
```

### Error Handling
```typescript
// Angular global error handler configured
// Use ToastService for user notifications
try {
  const result = await this.api.performAction();
  this.toastService.showSuccess('Action completed successfully');
} catch (error) {
  this.toastService.showError('Action failed. Please try again.');
}
```

### Testing Endpoints
```bash
# Test with internal API key
curl -H "X-Internal-API-Key: your-api-key" \
     https://localhost:7001/api/internal/users/123

# Test without key (should return 401)
curl https://localhost:7001/api/internal/users/123
```

## 📚 Important Files to Understand

### Core Configuration Files
- `Program.cs` - Service configuration and middleware setup
- `generated-apis.ts` - TypeScript API clients (auto-generated)
- `app.config.ts` - Angular application configuration
- `environment.ts` - Environment-specific settings

### Key Shared Libraries
- `AuthenticationMiddleware.cs` - JWT authentication handling
- `InternalHttpClientFactory.cs` - Service-to-service communication
- `PawfectMatchServices.cs` - Service enumeration

### Deployment Scripts
- `scripts/deploy.sh` - Interactive deployment orchestration
- `scripts/publish-pawfect-match-ng.sh` - Shared Angular library publishing

## ⚠️ Critical Guidelines

### Security Best Practices
1. **Never expose internal API keys** in client-side code
2. **Always use HTTPS** for service communication
3. **Validate user permissions** before data access
4. **Use parameterized queries** for database operations

### Performance Considerations
1. **Use pagination** for large data sets (already implemented in APIs)
2. **Implement proper caching** for frequently accessed data
3. **Minimize database queries** in tight loops
4. **Use async/await patterns** consistently

### API Design Standards
1. **Follow REST conventions** for public APIs
2. **Use `/api/internal/` prefix** for service-to-service endpoints
3. **Include proper HTTP status codes** and error messages
4. **Document APIs with Swagger/OpenAPI**

## 🚨 Common Pitfalls to Avoid

1. **Don't mix authentication schemes** - Use either user auth OR internal auth per endpoint
2. **Don't forget CORS configuration** for cross-origin requests
3. **Don't hardcode environment-specific values** - Use configuration
4. **Don't skip input validation** on API endpoints
5. **Don't use `any` types in TypeScript** - Leverage generated API types

## 📖 Additional Resources

### Internal Documentation
- `AUTHORIZATION_README.md` - Detailed authorization patterns
- `INTERNAL_HTTP_CLIENT_README.md` - Service communication guide
- `ECS_DEPLOYMENT_GUIDE.md` - Infrastructure deployment
- `CERTIFICATE_SETUP.md` - SSL/TLS configuration

### Generated APIs
- API clients are auto-generated using **NSwag** from OpenAPI specifications
- Regenerate after backend API changes: `nswag run`
- Types are strongly-typed and include request/response models

This guide should help you understand the project architecture and maintain consistency with established patterns. When in doubt, refer to existing implementations in the codebase as examples.
