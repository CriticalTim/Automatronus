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
- `Skill`: Individual skills linked to profiles
- `ProjectSkill`: Many-to-many relationship between projects and skills
- `ScrapeSession`: Tracks web scraping operations

#### Services (`Services/`)
- `ProfileService`: Manages user profiles and skills
- `ProjectService`: Handles project CRUD operations
- `WebScrapingService`: Automated web scraping using Selenium (targets FreelancerMap.de)
- `PdfService`: PDF generation and processing

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
- All entities use standard EF Core conventions with custom precision for decimal Budget field