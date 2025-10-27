# Setlist Studio 🎵

A comprehensive music management application designed to help musicians organize songs and create professional setlists for their performances.

[![CI/CD Pipeline](https://github.com/eugenecp/setlist-studio/actions/workflows/ci.yml/badge.svg)](https://github.com/eugenecp/setlist-studio/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-purple)](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
[![Material Design](https://img.shields.io/badge/UI-Material%20Design-red)](https://material.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)
[![WCAG 2.2 AA](https://img.shields.io/badge/Accessibility-WCAG%202.2%20AA-green)](https://www.w3.org/WAI/WCAG22/quickref/)

## 🎤 For Musicians - Get Started Now!

**New to Setlist Studio?** These guides will get you performing in minutes:

### 🚀 Quick Start Options
| **Experience Level** | **Best Option** | **Time to Setup** | **Guide** |
|---------------------|-----------------|-------------------|-----------|
| **Just want to try it** | Local Docker | **5 minutes** | **[→ Quick Start](docs/musician-quick-start.md)** |
| **Solo artist / small band** | Cloud deployment | **15 minutes** | **[→ Full Onboarding](docs/musician-onboarding.md)** |
| **Professional band** | PostgreSQL setup | **30 minutes** | **[→ Deployment Guide](docs/musician-deployment.md)** |

### 🎯 Popular Resources
- **[🎼 Musician Features Overview](docs/musician-features.md)** - See what makes Setlist Studio perfect for performers
- **[🚀 5-Minute Quick Start](docs/musician-quick-start.md)** - Get running immediately
- **[☁️ Band Deployment Guide](docs/musician-deployment.md)** - Professional setups for collaborating bands
- **[📱 Performance Day Guide](docs/musician-onboarding.md#-performance-day-workflow)** - Using Setlist Studio during shows

### 🎸 Built for Real Musicians
*"Finally, a setlist app built by musicians who understand backstage chaos, poor venue WiFi, and the need for quick song changes during a show."* - Verified musician feedback

## ✨ Features

### 🎼 Song Management
- **Comprehensive Library**: Add songs with artist, album, BPM, musical key, duration, and genre
- **Smart Organization**: Tag songs and rate difficulty levels (1-5 scale)
- **Performance Notes**: Add custom notes for each song (chords, lyrics, special instructions)
- **Search & Filter**: Quickly find songs by title, artist, genre, or tags
- **Realistic Data**: Supports authentic musical metadata (BPM 40-250, standard keys, etc.)

### 📋 Setlist Creation
- **Drag & Drop Ordering**: Intuitive reordering with accessibility alternatives
- **Performance Planning**: Set venue, date, and expected duration
- **Transition Notes**: Add notes between songs for smooth performances
- **Custom Settings**: Override BPM and key per performance
- **Templates**: Create reusable setlist templates
- **Encore & Optional**: Mark songs as encore or optional pieces

### 🔐 Secure Authentication
- **OAuth Integration**: Sign in with Google, Microsoft, or Facebook
- **User Isolation**: All data is user-specific and secure
- **Session Management**: Secure login/logout with session handling

### 🎨 Modern UI/UX
- **Material Design**: Clean, professional interface using MudBlazor components
- **Responsive Layout**: Works perfectly on mobile and desktop
- **Dark/Light Themes**: Automatic theme detection with manual toggle
- **Accessibility First**: WCAG 2.2 AA compliant with screen reader support

## 🎵 Perfect for Every Musical Scenario

### 🎹 **Solo Artists & Acoustic Performers**
- Quick song lookup during intimate venues
- BPM tracking for smooth transitions
- Offline mode when venue WiFi fails
- **[→ Solo Artist Setup Guide](docs/musician-onboarding.md#for-individual-musicians)**

### 🎸 **Rock & Cover Bands** 
- Collaborative setlist planning for all members
- Difficulty ratings for skill-appropriate assignments  
- Professional exports for sound engineers
- **[→ Band Collaboration Features](docs/musician-features.md#-collaboration-features)**

### 💍 **Wedding & Event Musicians**
- Multi-hour performance planning (dinner, dancing, special moments)
- Genre organization for different event phases
- Client-friendly export formats
- **[→ Wedding Gig Setup Example](docs/musician-onboarding.md#wedding-gig-setup)**

### 🎺 **Jazz & Classical Ensembles**
- Complex time signature support (5/4, 7/8, etc.)
- Authentic musical key notation
- Professional repertoire management
- **[→ Jazz Club Workflow](docs/musician-onboarding.md#jazz-club-night)**

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)

### Run with Docker (Recommended)

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/setlist-studio.git
   cd setlist-studio
   ```

2. **Set up environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your OAuth credentials (optional for basic testing)
   ```

3. **Run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

4. **Access the application**
   - Open your browser to [http://localhost:5000](http://localhost:5000)
   - The app will create a SQLite database and sample data automatically

### Run Locally (Development)

1. **Clone and restore packages**
   ```bash
   git clone https://github.com/your-username/setlist-studio.git
   cd setlist-studio
   dotnet restore
   ```

2. **Run the application**
   ```bash
   cd src/SetlistStudio.Web
   dotnet run
   ```

3. **Access the application**
   - Navigate to [https://localhost:5001](https://localhost:5001) or [http://localhost:5000](http://localhost:5000)
   - Sample data will be created automatically in development mode

## 🔧 Configuration

### OAuth Setup (Optional)

To enable social login, configure OAuth providers:

#### Google OAuth
1. Go to [Google Cloud Console](https://console.developers.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add `http://localhost:5000/signin-google` to authorized redirect URIs

#### Microsoft OAuth
1. Go to [Azure Portal](https://portal.azure.com/)
2. Register a new application
3. Add `http://localhost:5000/signin-microsoft` to redirect URIs

#### Facebook OAuth
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create a new app
3. Set up Facebook Login product
4. Add `http://localhost:5000/signin-facebook` to Valid OAuth Redirect URIs

### Environment Variables

#### Development Configuration
```bash
# OAuth Configuration (Development)
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
MICROSOFT_CLIENT_ID=your_microsoft_client_id  
MICROSOFT_CLIENT_SECRET=your_microsoft_client_secret
FACEBOOK_APP_ID=your_facebook_app_id
FACEBOOK_APP_SECRET=your_facebook_app_secret

# Database Configuration
CONNECTION_STRING=Data Source=setliststudio.db

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

#### Production Configuration with Azure Key Vault

For production deployments, OAuth secrets are securely stored in Azure Key Vault:

```bash
# Azure Key Vault Configuration
KeyVault__VaultName=your_keyvault_name

# Azure Authentication (Managed Identity recommended)
AZURE_CLIENT_ID=your_managed_identity_client_id
```

**📖 Production OAuth Setup**: 
- **[Azure Key Vault Setup Guide](docs/azure-keyvault-setup.md)** - Complete step-by-step setup instructions
- **[OAuth Configuration Summary](docs/azure-keyvault-oauth-summary.md)** - Implementation overview and deployment workflow

Includes:
- Creating and configuring Azure Key Vault
- Deploying OAuth secrets securely with automated scripts
- Setting up managed identity authentication
- GitHub Actions CI/CD integration
- Security best practices and troubleshooting

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
cd tests/SetlistStudio.Tests
dotnet test --filter "Category=Integration"
```

## 🏗️ Project Structure

```
setlist-studio/
├── src/
│   ├── SetlistStudio.Core/          # Domain models and interfaces
│   ├── SetlistStudio.Infrastructure/ # Data access and services
│   └── SetlistStudio.Web/           # Blazor Server web application
├── tests/
│   └── SetlistStudio.Tests/         # Unit and integration tests
├── docker-compose.yml               # Container orchestration
├── Dockerfile                       # Container definition
└── README.md                        # This file
```

### Architecture

- **Clean Architecture**: Separation of concerns with distinct layers
- **Domain-Driven Design**: Rich domain models with business logic
- **Repository Pattern**: Abstracted data access via Entity Framework Core
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **CQRS Principles**: Command and query separation where appropriate

## 🎯 Sample Data

The application includes realistic sample music data for development and testing:

### Songs Include:
- **"Bohemian Rhapsody"** by Queen (BPM: 72, Key: Bb) - Epic, Opera, Classic Rock
- **"Billie Jean"** by Michael Jackson (BPM: 117, Key: F#m) - Dance, Pop, 80s  
- **"Sweet Child O' Mine"** by Guns N' Roses (BPM: 125, Key: D) - Guitar Solo, Rock
- **"Take Five"** by Dave Brubeck (BPM: 176, Key: Bb) - Jazz, Instrumental, 5/4 Time
- **"The Thrill Is Gone"** by B.B. King (BPM: 98, Key: Bm) - Blues, Guitar

### Setlists Include:
- **Wedding Reception Set** - Perfect mix for celebrations
- **Jazz Evening Template** - Sophisticated standards for intimate venues

## 🌐 Accessibility Features

Setlist Studio is built with accessibility as a core requirement:

- **WCAG 2.2 AA Compliance**: Meets accessibility guidelines
- **Keyboard Navigation**: Full app functionality via keyboard
- **Screen Reader Support**: Proper ARIA labels and semantic HTML
- **High Contrast**: Sufficient color contrast ratios
- **Focus Management**: Clear focus indicators and logical tab order
- **Reduced Motion**: Respects user's motion preferences
- **Touch Targets**: Minimum 44px touch targets for mobile
- **Error Prevention**: Clear validation and confirmation dialogs

### Accessibility Testing
- Use NVDA, JAWS, or VoiceOver to test screen reader compatibility
- Navigate the entire app using only keyboard
- Check color contrast with tools like WebAIM's contrast checker

## 🌍 Internationalization

The application is ready for internationalization:

- **Resource Files**: Text strings externalized for translation
- **Culture Support**: Built-in support for multiple cultures
- **RTL Support**: Right-to-left language support via Material Design
- **Date/Time Formatting**: Locale-specific formatting

## 🚢 Deployment

### Docker Deployment
```bash
# Build and run
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Manual Deployment
```bash
# Publish the application
dotnet publish src/SetlistStudio.Web -c Release -o publish

# Run the published app
cd publish
dotnet SetlistStudio.Web.dll
```

### Production Considerations
- Use SQL Server or PostgreSQL for production databases
- Configure HTTPS with proper certificates
- Set up OAuth redirect URIs for your domain
- Enable logging and monitoring
- Configure backup strategies for user data

## 🔄 CI/CD Pipeline

This project uses GitHub Actions for continuous integration and deployment. The pipeline automatically:

- ✅ **Builds** the solution using .NET 8
- � **Runs tests** with detailed reporting
- 📊 **Generates code coverage** reports
- 🔒 **Performs security scans** for vulnerabilities
- 🐳 **Builds Docker images** for deployment
- 🚀 **Deploys preview environments** for pull requests

### Viewing Results

1. **Status Badge**: The badge at the top shows current build status
2. **Actions Tab**: Visit [Actions](https://github.com/eugenecp/setlist-studio/actions) to see all workflow runs
3. **Pull Requests**: Each PR shows build status and includes test/coverage reports
4. **Artifacts**: Download test results and coverage reports from completed runs

### Manual Triggers

You can manually trigger the CI/CD pipeline:
1. Go to the [Actions tab](https://github.com/eugenecp/setlist-studio/actions)
2. Select "CI/CD Pipeline" from the left sidebar
3. Click "Run workflow" and choose your branch

### Pipeline Stages

- **Build & Test**: Compiles code, runs tests, generates reports
- **Security Scan**: Checks for vulnerable packages
- **Docker Build**: Creates containerized version
- **Deploy Preview**: Sets up preview environment for PRs

## 🤝 Contributing

We welcome contributions! Setlist Studio maintains high standards for security, maintainability, and user experience to ensure musicians can rely on our tool during performances.

### 🚀 Quick Start for Contributors

1. **Read our comprehensive guides**:
   - **[CONTRIBUTING.md](CONTRIBUTING.md)** - Complete development setup and guidelines
   - **[Code Review Standards](.github/CODE_REVIEW_STANDARDS.md)** - Quality requirements and review process
   - **[Copilot Instructions](.github/copilot-instructions.md)** - Detailed technical standards

2. **Security-First Development**:
   - **CodeQL Analysis**: Zero tolerance for high/critical security issues
   - **Input Validation**: All user inputs must be validated and sanitized
   - **Authorization**: Every data access must verify user ownership

3. **Quality Requirements**:
   - **100% Test Success**: Zero failing tests allowed
   - **80%+ Coverage**: Line AND branch coverage for new code
   - **Zero Build Warnings**: Clean builds required
   - **Performance Standards**: <500ms API responses, <100ms DB queries

### 🎼 Development Process

```bash
# 1. Setup and validation
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio
dotnet test  # Must achieve 100% success rate
.\scripts\run-codeql-security.ps1  # Must pass with zero security issues

# 2. Create feature branch
git checkout -b feature/[issue-number]-[description]

# 3. Security-first development with musician focus
# - Implement input validation first
# - Add authorization checks
# - Use realistic musical data (BPM: 40-250, standard keys)
# - Follow test naming: {SourceClass}Tests.cs

# 4. Quality validation before PR
dotnet test --collect:"XPlat Code Coverage"  # Verify 80%+ coverage
dotnet build --verbosity normal  # Check for zero warnings
.\scripts\run-codeql-security.ps1  # Final security validation
```

### 📋 Pull Request Requirements

Every PR must complete our [comprehensive quality checklist](.github/PULL_REQUEST_TEMPLATE.md):
- **Security & Testing**: CodeQL passes, all tests pass, adequate coverage
- **Maintainability**: Clear business purpose, team handover readiness
- **Musical Context**: Features serve real musician workflows
- **Performance**: Response times and scalability considered

**All contributions go through our [Code Review Standards](.github/CODE_REVIEW_STANDARDS.md) ensuring security, maintainability, and musician-focused excellence.**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## � Ready to Transform Your Performances?

### 🚀 **Get Started Right Now**

**For Individual Musicians:**
```bash
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio  
docker-compose up -d
# Visit http://localhost:5000 - You're ready to rock! 🎸
```

**For Bands & Professional Use:**
- **[📖 Full Setup Guide](docs/musician-onboarding.md)** - Complete walkthrough
- **[☁️ Cloud Deployment](docs/musician-deployment.md)** - Professional band setup
- **[🎼 Feature Overview](docs/musician-features.md)** - See what you can do

### 💬 **Join the Community**
- **[Share Your Setup](https://github.com/eugenecp/setlist-studio/discussions)** - Show us your creative setlist organization
- **[Get Help](https://github.com/eugenecp/setlist-studio/discussions)** - Community support from fellow musicians
- **[Request Features](https://github.com/eugenecp/setlist-studio/issues)** - Help us build what musicians need

## �🎵 About

Setlist Studio was created to solve a real need in the music community. Whether you're a solo artist, part of a band, or a DJ, organizing your music and planning performances shouldn't be complicated. 

The app focuses on:
- **Reliability**: Your setlists need to work when you're on stage
- **Simplicity**: Intuitive interface that musicians can learn quickly  
- **Flexibility**: Adapts to different musical styles and performance types
- **Accessibility**: Everyone should be able to use music technology
- **Privacy**: Your music data stays secure and private

## 🔗 Essential Links

### 🎵 **For Musicians**
- **[🚀 Get Started in 5 Minutes](docs/musician-quick-start.md)** - Try Setlist Studio right now
- **[📖 Complete Musician Guide](docs/musician-onboarding.md)** - From setup to your first gig
- **[🎼 Features for Performers](docs/musician-features.md)** - Why musicians love Setlist Studio
- **[☁️ Professional Deployment](docs/musician-deployment.md)** - Setup for bands & organizations

### 🛠️ **For Developers**  
- **[GitHub Repository](https://github.com/eugenecp/setlist-studio)** - Source code and contributions
- **[Issues & Bug Reports](https://github.com/eugenecp/setlist-studio/issues)** - Report problems or request features  
- **[Community Discussions](https://github.com/eugenecp/setlist-studio/discussions)** - Get help and share ideas
- **[Technical Documentation](docs/)** - Architecture, security, and deployment guides

### 🌐 **Live Resources**
- **Live Demo**: [setlist-studio.demo.com](https://setlist-studio.demo.com) (Coming soon)
- **Documentation Site**: [docs.setlist-studio.com](https://docs.setlist-studio.com) (Coming soon)

---

**Made with ❤️ for the music community**

*Ready to rock your next performance? Get started with Setlist Studio today!*