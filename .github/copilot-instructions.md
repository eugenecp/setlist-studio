# Copilot Instructions for Setlist Studio

## Project Description

**Setlist Studio** is a comprehensive music management application designed to help musicians organize and plan their performances. The app enables users to:

- **Manage Artists and Songs**: Add and organize musical artists and their songs with detailed metadata
- **Build Dynamic Setlists**: Create performance setlists with song order, transitions, BPM, and musical keys
- **Schedule Performances**: Plan and manage upcoming shows and events

### Target Audience

This application serves two primary audiences:

- **Developers**: Software engineers building, maintaining, and enhancing the Setlist Studio application
- **Musicians**: Artists, bands, and performers who need a reliable tool to organize their music and plan their shows

## Tools and Setup

Setlist Studio is built using modern .NET technologies and follows industry best practices:

### Technology Stack
- **.NET 8**: The latest long-term support version of .NET for robust application development
- **RESTful APIs**: Well-designed endpoints for seamless data interaction
- **Authentication & Authorization**: Secure user management and access control
- **Comprehensive Logging**: Detailed application monitoring and debugging capabilities
- **Database Integration**: Persistent storage for artists, songs, setlists, and performance data
- **Automated Testing**: Unit, integration, and end-to-end testing suites

### Development Workflow
- **GitHub Actions**: Automated CI/CD pipelines for building, testing, and deployment
- **Version Control**: Git-based workflow with feature branches and pull request reviews

## Key Principles

When working with Setlist Studio, please adhere to these core principles:

### 1. Reliability 🛡️
Every feature must work consistently and predictably. All functionality should be thoroughly tested and handle edge cases gracefully.

**Guidelines:**
- Write comprehensive unit and integration tests for all new features
- Implement proper error handling and user-friendly error messages
- Ensure database transactions are atomic and consistent
- Test boundary conditions (empty setlists, maximum song limits, etc.)

### 2. Scalability 📈
The application must handle growth in songs, setlists, users, and performance data as the user base expands.

**Guidelines:**
- Design efficient database queries and indexing strategies
- Implement pagination for large data sets
- Use caching where appropriate to reduce database load
- Structure code to support horizontal scaling when needed

### 3. Security 🔒
Protect user data and maintain system integrity through robust security practices.

**Guidelines:**
- Validate all user inputs on both client and server sides
- Never store secrets, API keys, or sensitive data in source code
- Use environment variables and secure configuration management
- Implement proper authentication and authorization checks
- Sanitize data to prevent injection attacks

### 4. Maintainability 🔧
Keep the codebase organized, well-documented, and easy to understand for current and future developers.

**Guidelines:**
- Follow consistent naming conventions and coding standards
- Write clear, self-documenting code with meaningful variable and method names
- Maintain up-to-date documentation for APIs and complex business logic
- Organize code into logical modules and maintain separation of concerns
- Keep dependencies up to date and minimize technical debt

### 5. Delight ✨
Create an enjoyable user experience with realistic, relatable content and smooth interactions.

**Guidelines:**
- Use realistic music examples in documentation, tests, and sample data
- Include diverse genres, artists, and musical styles in examples
- Provide helpful default values (e.g., common BPM ranges, popular keys)
- Create intuitive user interfaces and clear user feedback
- Use authentic musical terminology and metadata

## Example Prompts for GitHub Copilot

Use these example prompts to get the most out of GitHub Copilot while maintaining our key principles:

### Reliability Examples
```
"Write comprehensive unit tests for the setlist creation endpoint, including validation edge cases"

"Create integration tests for the artist and song relationship management"

"Add error handling for database connection failures in the performance scheduling service"

"Write tests that verify setlist ordering is maintained correctly when songs are added or removed"
```

### Scalability Examples
```
"Optimize the query for fetching large setlists with song metadata using Entity Framework"

"Implement pagination for the artists endpoint to handle thousands of artists efficiently"

"Redesign the setlist storage to support better performance with 10,000+ songs per user"

"Add caching layer for frequently accessed song and artist data"
```

### Security Examples
```
"Add input validation for BPM values to ensure they're between 40 and 250"

"Implement authorization checks to ensure users can only access their own setlists"

"Add data sanitization for artist names and song titles to prevent XSS attacks"

"Create validation rules for musical keys to only accept valid key signatures (C, C#, Db, etc.)"
```

### Maintainability Examples
```
"Refactor the Song and Setlist classes with clearer property names and comprehensive XML documentation"

"Organize the API controllers into logical folders and add consistent routing patterns"

"Create a comprehensive README with setup instructions and API documentation"

"Add inline comments explaining the complex setlist transition logic"
```

### Delight Examples
```
"Generate Swagger API examples using realistic song data like 'Bohemian Rhapsody' by Queen (BPM: 72, Key: Bb)"

"Create seed data with a diverse mix of musical genres including rock, jazz, classical, and electronic music"

"Add sample setlists for different types of performances (wedding, concert, practice session)"

"Design user-friendly error messages that use musical terminology musicians will understand"
```

## Sample Data Guidelines

When creating examples, tests, or documentation, use realistic musical data:

### Song Examples
- **Classic Rock**: "Sweet Child O' Mine" by Guns N' Roses (BPM: 125, Key: D)
- **Pop**: "Billie Jean" by Michael Jackson (BPM: 117, Key: F#m)
- **Jazz**: "Take Five" by Dave Brubeck (BPM: 176, Key: Bb)
- **Blues**: "The Thrill Is Gone" by B.B. King (BPM: 98, Key: Bm)

### BPM Ranges
- **Ballads**: 60-80 BPM
- **Medium Tempo**: 90-120 BPM  
- **Up-tempo**: 130-160 BPM
- **Fast Songs**: 170+ BPM

### Common Keys
- **Guitar-friendly**: E, A, D, G, C
- **Vocal-friendly**: F, Bb, Eb, Ab
- **Minor keys**: Am, Em, Bm, F#m, Cm

---

## Getting Started

When contributing to Setlist Studio:

1. **Read the codebase**: Familiarize yourself with existing patterns and conventions
2. **Follow the principles**: Keep reliability, scalability, security, maintainability, and delight in mind
3. **Use realistic examples**: When creating tests or documentation, use authentic musical data
4. **Test thoroughly**: Ensure your code works correctly and handles edge cases
5. **Document your work**: Add clear comments and update documentation as needed

Remember: We're building a tool that musicians will rely on for their performances. Every line of code should contribute to creating a reliable, secure, and delightful experience for artists sharing their music with the world.