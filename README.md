# Code Generator Solution

A comprehensive .NET Core Web API solution for generating code projects and templates across multiple programming languages.

## 🚀 Features

- **Multi-Language Support**: Generate code for C#, JavaScript, TypeScript, and Python
- **Template System**: Pre-built templates for classes, controllers, and common patterns  
- **Project Generation**: Create complete project structures with proper folder organization
- **RESTful API**: Clean API endpoints for all code generation operations
- **Swagger Documentation**: Interactive API documentation available at startup
- **Extensible Architecture**: Easy to add new languages and templates

## 📁 Project Structure

```
CodeGenerator/
├── CodeGenerator.sln                    # Solution file
├── CodeGenerator.API/                   # Main Web API project
│   ├── Controllers/                     # API Controllers
│   │   ├── CodeGeneratorController.cs   # Main code generation endpoints
│   │   └── WeatherForecastController.cs # Example controller
│   ├── Models/                          # Data models
│   │   ├── CodeGenerationRequest.cs     # Request models
│   │   ├── CodeGenerationResponse.cs    # Response models
│   │   └── Template.cs                  # Template models
│   ├── Services/                        # Business logic
│   │   └── CodeGenerationService.cs     # Core generation service
│   ├── Program.cs                       # Application startup
│   ├── appsettings.json                # Configuration
│   └── CodeGenerator.API.csproj        # Project file
└── README.md                           # This file
```

## 🛠️ Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio Code or Visual Studio 2022

### Running the Application

1. **Clone and navigate to the project:**
   ```bash
   cd CodeGenerator
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   cd CodeGenerator.API
   dotnet run
   ```

4. **Access the API:**
   - API Base URL: `https://localhost:7071` or `http://localhost:5241`
   - Swagger UI: `https://localhost:7071` (available in development mode)

## 📚 API Endpoints

### Code Generation
- `POST /api/codegenerator/generate-project` - Generate a complete project
- `POST /api/codegenerator/generate-from-template` - Generate code from template
- `GET /api/codegenerator/templates` - Get available templates
- `GET /api/codegenerator/templates/{id}` - Get specific template
- `GET /api/codegenerator/supported-technologies` - Get supported languages/frameworks

### Utility
- `GET /` - API information and available endpoints
- `GET /api/codegenerator/health` - Health check

## 🧪 Example Usage

### Generate a C# Web API Project

```bash
POST /api/codegenerator/generate-project
Content-Type: application/json

{
  "projectName": "MyWebAPI",
  "language": "csharp",
  "framework": "ASP.NET Core",
  "description": "A sample Web API project",
  "features": ["swagger", "controllers", "models"]
}
```

### Generate Code from Template

```bash
POST /api/codegenerator/generate-from-template
Content-Type: application/json

{
  "templateName": "CSharpClass",
  "parameters": {
    "ClassName": "Product",
    "Namespace": "MyApp.Models"
  }
}
```

## 🔧 Configuration

The application can be configured via `appsettings.json`:

```json
{
  "CodeGenerator": {
    "DefaultOutputPath": "./generated",
    "SupportedLanguages": ["C#", "JavaScript", "TypeScript", "Python"],
    "MaxFilesPerProject": 100,
    "EnableTemplateCache": true
  }
}
```

## 🎯 Supported Technologies

### Languages
- **C#**: .NET Core, ASP.NET Core, Console Apps
- **JavaScript**: Node.js, Express, React components
- **TypeScript**: Node.js, NestJS applications  
- **Python**: Flask, Django, FastAPI

### Project Types
- Web APIs
- Web Applications
- Console Applications
- Class Libraries
- Microservices

## 🔨 Development

### Adding New Templates

1. Update the `GetPredefinedTemplates()` method in `CodeGenerationService.cs`
2. Add the template content with placeholder syntax: `{{ParameterName}}`
3. Specify required parameters in the template definition

### Adding New Languages

1. Add language handling in `GenerateProjectFilesAsync()` method
2. Create specific generation methods (e.g., `GenerateGoProject()`)
3. Update the supported technologies endpoint

## 📋 Features Roadmap

- [ ] Database storage for custom templates
- [ ] File upload/download for generated projects
- [ ] Template marketplace integration
- [ ] Git repository integration
- [ ] Real-time generation progress tracking
- [ ] Custom template editor UI
- [ ] Batch project generation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For questions and support:
- Create an issue in the repository
- Check the API documentation at `/swagger`
- Review the example requests in the Swagger UI