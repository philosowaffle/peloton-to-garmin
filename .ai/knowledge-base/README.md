# P2G Knowledge Base

## Overview
This knowledge base contains comprehensive documentation for maintaining, fixing, and enhancing the Peloton to Garmin (P2G) application. It serves as a reference guide for developers, maintainers, and contributors.

## Knowledge Base Structure

### 📋 [01-system-architecture.md](01-system-architecture.md)
**System Architecture Documentation**
- High-level system overview and component interactions
- Data flow diagrams and sync workflow
- Technology stack and deployment models
- Security and scalability considerations
- File system organization and error handling

### 🔌 [02-api-reference.md](02-api-reference.md)
**REST API Reference**
- Complete API endpoint documentation
- Request/response schemas and examples
- Authentication and security considerations
- Error handling and status codes
- Integration examples for multiple languages

### 🛠️ [03-development-setup.md](03-development-setup.md)
**Development Environment Setup**
- Prerequisites and required software
- Step-by-step setup instructions
- IDE configuration (Visual Studio, VS Code)
- Docker development environment
- Testing and debugging setup

### 🌐 [05-external-api-integration.md](05-external-api-integration.md)
**External API Integration Guide**
- Peloton API authentication and endpoints
- Garmin Connect API integration details
- Rate limiting and error handling strategies
- Performance optimization techniques
- Security and monitoring considerations

## Quick Reference

### Key Project Information
- **Version**: 5.0.1
- **Framework**: .NET 9.0
- **Primary Languages**: C#, Blazor, HTML/CSS
- **Database**: SQLite
- **Testing**: xUnit, Moq, FluentAssertions

### Core Components
- **ConsoleClient**: Headless sync application
- **WebUI**: Blazor web interface
- **API**: REST API service
- **ClientUI**: MAUI desktop application
- **Sync Service**: Core synchronization logic
- **Conversion**: File format converters (FIT, TCX, JSON)

### External Dependencies
- **Peloton API**: `https://api.onepeloton.com`
- **Garmin Connect**: `https://connect.garmin.com`
- **Key Libraries**: Flurl.Http, Serilog, Prometheus, Dynastream.Fit

## Common Tasks

### Development Workflow
1. **Setup Environment**: Follow [03-development-setup.md](03-development-setup.md)
2. **Understand Architecture**: Review [01-system-architecture.md](01-system-architecture.md)
3. **Run Tests**: Use testing guidelines in [06-testing-strategy.md](06-testing-strategy.md)
4. **Debug Issues**: Consult [04-troubleshooting-guide.md](04-troubleshooting-guide.md)

### Maintenance Tasks
1. **Monitor APIs**: Check external API status and changes
2. **Update Dependencies**: Keep NuGet packages current
3. **Review Logs**: Monitor application logs for issues
4. **Test Functionality**: Verify sync operations regularly

### Enhancement Process
1. **Plan Changes**: Document requirements and design
2. **Implement Features**: Follow existing patterns and conventions
3. **Add Tests**: Ensure comprehensive test coverage
4. **Update Documentation**: Keep knowledge base current

## File Locations

### Configuration Files
- `configuration.example.json` - Example configuration
- `configuration.local.json` - Local development config
- `src/Common/Configuration.cs` - Configuration loading logic

### Key Source Files
- `src/Sync/SyncService.cs` - Main synchronization service
- `src/Peloton/ApiClient.cs` - Peloton API integration
- `src/Garmin/ApiClient.cs` - Garmin API integration
- `src/Conversion/` - File format converters

### Test Files
- `src/UnitTests/` - Unit test suite
- `src/UnitTests/Data/` - Test data and samples

### Documentation
- `mkdocs/docs/` - User documentation
- `README.md` - Project overview
- `vNextReleaseNotes.md` - Release notes

## Support and Resources

### Internal Resources
- **GitHub Repository**: https://github.com/philosowaffle/peloton-to-garmin
- **Issues**: https://github.com/philosowaffle/peloton-to-garmin/issues
- **Discussions**: https://github.com/philosowaffle/peloton-to-garmin/discussions
- **Wiki**: https://philosowaffle.github.io/peloton-to-garmin

### External Resources
- **.NET Documentation**: https://docs.microsoft.com/en-us/dotnet/
- **ASP.NET Core**: https://docs.microsoft.com/en-us/aspnet/core/
- **Blazor**: https://docs.microsoft.com/en-us/aspnet/core/blazor/
- **MAUI**: https://docs.microsoft.com/en-us/dotnet/maui/

## Contributing to Knowledge Base

### Adding New Documentation
1. Create new markdown files following the naming convention
2. Update this README.md with links to new documentation
3. Ensure cross-references between related documents
4. Use consistent formatting and structure

### Updating Existing Documentation
1. Keep information current with code changes
2. Add new troubleshooting scenarios as they arise
3. Update API documentation when endpoints change
4. Maintain accuracy of configuration examples

### Documentation Standards
- Use clear, descriptive headings
- Include code examples where appropriate
- Provide step-by-step instructions
- Cross-reference related information
- Keep language concise but comprehensive

## Version History

### Current Version (5.0.1)
- Comprehensive knowledge base creation
- System architecture documentation
- API reference and troubleshooting guides
- Development setup and testing strategies

### Planned Updates
- Enhanced API monitoring documentation
- Performance optimization guides
- Security best practices expansion
- Automated testing improvements

---

**Note**: This knowledge base is maintained alongside the P2G codebase. When making significant changes to the application, please update the relevant documentation to keep it current and accurate. 