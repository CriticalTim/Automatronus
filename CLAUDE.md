# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

### Database Management
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create database migrations
dotnet ef migrations add <MigrationName>

# Update database
dotnet ef database update

# Drop database (if needed)
dotnet ef database drop
```

### Testing
The project uses .NET 8.0 testing framework. Run tests with:
```bash
dotnet test
```

## Architecture Overview

Automatronus is a cross-platform application that combines Windows Forms with Blazor WebView for the UI on Windows, and runs in console mode on Linux/macOS. The application is designed for freelance project management and web scraping.

### Technology Stack
- **Framework**: .NET 8.0 cross-platform application 
  - Windows: Windows Forms with Blazor WebView
  - Linux/macOS: Console application
- **Database**: SQLite with Entity Framework Core
- **Web Scraping**: Selenium WebDriver with Chrome driver
- **UI**: Blazor components (Windows Forms WebView on Windows, console on other platforms)
- **Services**: Dependency injection using Microsoft.Extensions.Hosting

### Platform-Specific Features
- **Windows**: Full GUI with Windows Forms hosting Blazor WebView
- **Linux/macOS**: Console mode with core functionality (database, web scraping, services)
- **Cross-platform**: All business logic, services, and data access work on all platforms

### Key Components

#### Data Layer (`Data/`)
- `AutomatronusContext`: Entity Framework DbContext managing all database operations
- SQLite database with tables for Profiles, Skills, Projects, ProjectSkills, and ScrapeSessions

#### Models (`Models/`)
- `Profile`: User profile information with skills
- `Project`: Freelance project data scraped from job boards
- `Skill`: Individual skills with years of experience and proficiency levels (+ to ++++)
- `ProjectSkill`: Many-to-many relationship between projects and skills
- `ScrapeSession`: Tracks web scraping operations
- `SkillProficiency`: Enum for skill levels (Beginner=1, Intermediate=2, Advanced=3, Expert=4)

#### Services (`Services/`)
- `ProfileService`: Manages user profiles and skills
- `ProjectService`: Handles project CRUD operations
- `WebScrapingService`: Automated web scraping using Selenium (targets FreelancerMap.de)
- `PdfService`: PDF text extraction and skill parsing using iText7
- `ExtractedSkill`: Model for skills with years of experience and proficiency levels

#### UI (`Pages/`)
- Blazor components for different application views
- `Index.razor`: Main dashboard
- `Projects.razor`: Project management interface
- `Skills.razor`: Skills management
- `History.razor`: Scraping history

### Application Flow
1. Main entry point (`Program.cs`) sets up dependency injection and creates the database
2. `MainForm.cs` hosts the Blazor WebView component
3. Blazor routing (`App.razor`) handles navigation between pages
4. Services are injected into Blazor components for data operations

### Web Scraping Architecture
The application uses headless Chrome to scrape freelance job boards:
- Configurable skill-based searches
- Automatic data extraction with CSS selectors
- Session tracking for scraping operations
- Project deduplication and status management

### Database Schema
- Profiles have many Skills (1:N)
- Projects belong to ScrapeSession (N:1)
- Projects have many ProjectSkills (N:M via join table)
- Skills include YearsOfExperience (nullable int) and Proficiency (enum)
- All entities use standard EF Core conventions with custom precision for decimal Budget field

### PDF Processing
- **Library**: iText7 (version 8.0.3) for cross-platform PDF text extraction
- **Skill Extraction**: Regex patterns to identify skills, years of experience, and proficiency levels
- **Supported Patterns**:
  - Years: "C# (5 years)", "JavaScript for 3 years", "5 years of Python"
  - Proficiency: "C# +++", "Expert in Python", "Advanced JavaScript"
  - Skills Section: Automatically detects skills/technologies/expertise sections
- **Proficiency Levels**: + (Beginner), ++ (Intermediate), +++ (Advanced), ++++ (Expert)

### UI Styling
- **Theme**: Dark theme with orange accent color (#ff8c00)
- **Buttons**: Orange primary buttons with hover effects
- **Enhanced Skill Cards**: Display skill name, years of experience, and proficiency symbols
- **Cross-platform**: Responsive design works in both Windows Forms WebView and browsers

### Known Issues & Solutions
- **Windows Build Errors**: Use `AddWindowsFormsBlazorWebView()` instead of `AddBlazorWebView()`
- **RootComponent API**: Use constructor `new RootComponent("#app", typeof(App), null)`
- **Database Migration**: Run `dotnet ef database update` after model changes