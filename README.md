# Setlist Studio 🎵

A comprehensive music management application designed to help musicians organize songs and create professional setlists for their performances.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-purple)](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
[![Material Design](https://img.shields.io/badge/UI-Material%20Design-red)](https://material.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)
[![WCAG 2.2 AA](https://img.shields.io/badge/Accessibility-WCAG%202.2%20AA-green)](https://www.w3.org/WAI/WCAG22/quickref/)

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

```bash
# OAuth Configuration
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

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Follow coding standards**: Use consistent naming and add tests
4. **Commit changes**: `git commit -m 'Add amazing feature'`
5. **Push to branch**: `git push origin feature/amazing-feature`
6. **Open a Pull Request**

### Development Guidelines
- Follow the existing code style and patterns
- Write unit tests for new functionality
- Ensure accessibility compliance
- Update documentation as needed
- Use realistic music data in examples

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🎵 About

Setlist Studio was created to solve a real need in the music community. Whether you're a solo artist, part of a band, or a DJ, organizing your music and planning performances shouldn't be complicated. 

The app focuses on:
- **Reliability**: Your setlists need to work when you're on stage
- **Simplicity**: Intuitive interface that musicians can learn quickly  
- **Flexibility**: Adapts to different musical styles and performance types
- **Accessibility**: Everyone should be able to use music technology
- **Privacy**: Your music data stays secure and private

## 🔗 Links

- **Live Demo**: [setlist-studio.demo.com](https://setlist-studio.demo.com) (Coming soon)
- **Documentation**: [docs.setlist-studio.com](https://docs.setlist-studio.com) (Coming soon)
- **Issues**: [GitHub Issues](https://github.com/your-username/setlist-studio/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-username/setlist-studio/discussions)

---

**Made with ❤️ for the music community**

*Ready to rock your next performance? Get started with Setlist Studio today!*