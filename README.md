# 🐾 PawfectMatch

**A comprehensive pet adoption platform connecting shelters with loving families**

PawfectMatch is a modern, scalable pet adoption ecosystem built with Angular, .NET, and AWS infrastructure. The platform facilitates intelligent pet-adopter matching while providing comprehensive tools for shelter management and adopter engagement.

## ✨ Features

### 🏠 **For Pet Adopters**
- **Smart Matching Algorithm**: AI-powered pet recommendations based on lifestyle and preferences
- **Interactive Pet Profiles**: Comprehensive pet information with photos and personality traits
- **Application Management**: Streamlined adoption application process
- **Real-time Notifications**: Updates on application status and new pet arrivals

### 🏢 **For Shelter Administrators**
- **Comprehensive Shelter Hub**: Complete shelter management dashboard
- **Pet Profile Management**: Easy pet listing with detailed information and media
- **Application Processing**: Review and manage adoption applications
- **Analytics & Reporting**: Insights into adoption success rates and trends

### 🔐 **Secure Identity Management**
- **Multi-tenant Authentication**: Separate login flows for adopters and shelter staff
- **JWT-based Security**: Secure, stateless authentication with refresh tokens
- **Role-based Access Control**: Different permissions for adopters and shelter administrators

## 🏗️ Architecture

PawfectMatch follows a microservices architecture with clear separation of concerns:

```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   Identity      │  │     Matcher     │  │   ShelterHub    │
│   Service       │  │    Service      │  │    Service      │
│                 │  │                 │  │                 │
│ • Authentication│  │ • Pet Matching  │  │ • Shelter Mgmt  │
│ • User Profiles │  │ • Applications  │  │ • Pet Profiles  │
│ • JWT Tokens    │  │ • Notifications │  │ • Admin Tools   │
└─────────────────┘  └─────────────────┘  └─────────────────┘
        │                      │                      │
        └──────────────────────┼──────────────────────┘
                               │
                    ┌─────────────────┐
                    │   AWS Cloud     │
                    │  Infrastructure │
                    │                 │
                    │ • Route 53      │
                    │ • CloudFront    │
                    │ • ECS Fargate   │
                    │ • DynamoDB      │
                    │ • Cognito       │
                    │ • S3 Storage    │
                    └─────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- **Node.js** 18+ with npm
- **.NET 8.0** SDK
- **AWS CLI** v2 configured
- **Docker** (for local development)
- **AWS CDK** CLI (`npm install -g aws-cdk`)

### 1. Clone the Repository

```bash
git clone https://github.com/longhl104/PawfectMatch.git
cd PawfectMatch
```

### 2. Infrastructure Setup

Deploy the AWS infrastructure using CDK:

```bash
cd cdk
npm install
npm run build

# Deploy to development environment
../scripts/deploy.sh

# Or deploy manually
export CDK_STAGE=development
cdk deploy --all
```

### 3. Backend Services

#### Identity Service
```bash
cd Identity/Longhl104.Identity
dotnet restore
dotnet run
```

#### Matcher Service
```bash
cd Matcher/Longhl104.Matcher
dotnet restore
dotnet run
```

#### ShelterHub Service
```bash
cd ShelterHub/Longhl104.ShelterHub
dotnet restore
dotnet run
```

### 4. Frontend Applications

#### Adopter Web App (Matcher Client)
```bash
cd Matcher/client
npm install
npm start
# Visit http://localhost:4200
```

#### Shelter Admin App (ShelterHub Client)
```bash
cd ShelterHub/client
npm install
npm start
# Visit http://localhost:4201
```

#### Identity Management App
```bash
cd Identity/client
npm install
npm start
# Visit http://localhost:4202
```

## 📁 Project Structure

```
PawfectMatch/
├── 🏗️ cdk/                          # AWS CDK Infrastructure
│   ├── lib/                         # CDK Stack Definitions  
│   └── bin/                         # CDK App Entry Point
├── 🔐 Identity/                     # Authentication Service
│   ├── client/                      # Angular Identity App
│   ├── Lambdas/                     # AWS Lambda Functions
│   └── Longhl104.Identity/          # .NET Identity API
├── 🎯 Matcher/                      # Pet Matching Service
│   ├── client/                      # Angular Adopter App
│   └── Longhl104.Matcher/           # .NET Matcher API
├── 🏢 ShelterHub/                   # Shelter Management Service
│   ├── client/                      # Angular Shelter Admin App
│   └── Longhl104.ShelterHub/        # .NET ShelterHub API
├── 📦 Shared/                       # Shared Libraries & Components
│   ├── Longhl104.PawfectMatch/      # .NET Shared Library
│   └── pawfect-match-angular/       # Angular Component Library
├── 🚀 scripts/                      # Deployment & Utility Scripts
└── 📊 Environment/                  # Lambda Functions & Utilities
```

## 🌐 Live Applications

### Production Environment
- **Adopter Portal**: [adopter.pawfectmatchnow.com](https://adopter.pawfectmatchnow.com)
- **Shelter Hub**: [shelter.pawfectmatchnow.com](https://shelter.pawfectmatchnow.com)
- **Identity Service**: [id.pawfectmatchnow.com](https://id.pawfectmatchnow.com)

### Development Environment
- **Adopter Portal**: [adopter.development.pawfectmatchnow.com](https://adopter.development.pawfectmatchnow.com)
- **Shelter Hub**: [shelter.development.pawfectmatchnow.com](https://shelter.development.pawfectmatchnow.com)
- **Identity Service**: [id.development.pawfectmatchnow.com](https://id.development.pawfectmatchnow.com)

## 🛠️ Technology Stack

### Frontend
- **Angular 20**: Modern web framework with standalone components
- **TypeScript**: Type-safe JavaScript development
- **PrimeNG**: UI component library
- **SCSS**: Advanced CSS preprocessing
- **RxJS**: Reactive programming with Observables

### Backend
- **.NET 8**: High-performance web APIs
- **Entity Framework Core**: Object-relational mapping
- **AWS SDK**: Cloud service integration
- **JWT Authentication**: Secure token-based auth
- **Swagger/OpenAPI**: API documentation

### Cloud Infrastructure
- **AWS ECS Fargate**: Serverless containerized hosting
- **AWS Cognito**: User authentication and management
- **DynamoDB**: NoSQL database for high performance
- **S3**: Object storage for images and files
- **Route 53**: DNS management and routing
- **CloudFront**: Global content delivery network

### Development Tools
- **AWS CDK**: Infrastructure as Code in TypeScript
- **Docker**: Containerization for consistent environments
- **GitHub Actions**: CI/CD pipelines
- **ESLint/Prettier**: Code formatting and linting

## 🔧 Development

### Building the Shared Library

The project includes a shared Angular component library:

```bash
cd Shared/pawfect-match-angular
npm install
ng build pawfect-match-ng

# Publish to npm (if you have permissions)
cd dist/pawfect-match-ng
npm publish
```

### Running Tests

```bash
# Frontend tests
cd Matcher/client
npm test

# Backend tests  
cd Matcher/Longhl104.Matcher
dotnet test
```

### Environment Configuration

Create environment-specific configuration files:

```bash
# Development
export CDK_STAGE=development
export CDK_DEFAULT_REGION=ap-southeast-2

# Production
export CDK_STAGE=production
export CDK_DEFAULT_REGION=ap-southeast-2
```

## 📚 API Documentation

Each service provides comprehensive API documentation:

- **Identity API**: `/swagger` endpoint on Identity service
- **Matcher API**: `/swagger` endpoint on Matcher service  
- **ShelterHub API**: `/swagger` endpoint on ShelterHub service

## 🔐 Security Features

- **Multi-factor Authentication**: Optional 2FA for enhanced security
- **Role-based Access Control**: Granular permissions system
- **Data Encryption**: All data encrypted at rest and in transit
- **API Rate Limiting**: Protection against abuse and DDoS
- **CORS Configuration**: Secure cross-origin resource sharing
- **Input Validation**: Comprehensive request validation and sanitization

## 🎨 UI/UX Features

- **Responsive Design**: Optimized for mobile, tablet, and desktop
- **Dark/Light Mode**: Theme switching for user preference
- **Accessibility**: WCAG 2.1 AA compliance
- **Progressive Web App**: Offline capabilities and app-like experience
- **Real-time Updates**: WebSocket integration for live notifications
- **Internationalization**: Multi-language support ready

## 🚦 Deployment

### Using Deployment Scripts

```bash
# Interactive deployment with profile selection
cd scripts
./deploy.sh

# Automated deployment for CI/CD
./deploy.sh --profile longhl104 --stage development --auto-approve
```

### Manual Deployment

```bash
# Build and deploy infrastructure
cd cdk
npm run build
cdk deploy --all

# Deploy applications to ECS
# (Handled automatically by CI/CD pipeline)
```

## 📖 Documentation

Detailed documentation is available in individual service directories:

- [CDK Infrastructure](./cdk/README.md)
- [Identity Service](./Identity/README.md)
- [Matcher Service](./Matcher/README.md)
- [ShelterHub Service](./ShelterHub/README.md)
- [Shared Libraries](./Shared/README.md)
- [Deployment Scripts](./scripts/README.md)

## 🤝 Contributing

We welcome contributions! Please see our contributing guidelines:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Commit your changes**: `git commit -m 'Add amazing feature'`
4. **Push to the branch**: `git push origin feature/amazing-feature`
5. **Open a Pull Request**

### Development Guidelines

- Follow Angular and .NET coding conventions
- Write tests for new features
- Update documentation for API changes
- Use semantic commit messages
- Ensure all CI checks pass

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/longhl104/PawfectMatch/issues)
- **Discussions**: [GitHub Discussions](https://github.com/longhl104/PawfectMatch/discussions)
- **Email**: support@pawfectmatchnow.com

## 🙏 Acknowledgments

- **PrimeNG Team**: For the excellent Angular UI components
- **AWS**: For providing robust cloud infrastructure
- **Angular Team**: For the amazing web framework
- **Microsoft**: For the powerful .NET ecosystem
- **Open Source Community**: For the countless libraries and tools

---

**Made with ❤️ for pets and the families who love them**

*PawfectMatch - Where every pet finds their perfect home* 🏡🐕🐱
