using CodeGenerator.API.Models;

namespace CodeGenerator.API.Services
{
    public interface ICodeGenerationService
    {
        Task<CodeGenerationResponse> GenerateProjectAsync(CodeGenerationRequest request);
        Task<string> GenerateFromTemplateAsync(TemplateRequest request);
        Task<List<Template>> GetAvailableTemplatesAsync();
        Task<Template?> GetTemplateByIdAsync(int id);
    }

    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly ILogger<CodeGenerationService> _logger;

        public CodeGenerationService(ILogger<CodeGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<CodeGenerationResponse> GenerateProjectAsync(CodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating project: {request.ProjectName} for {request.Language}");

                var response = new CodeGenerationResponse
                {
                    Success = true,
                    Message = "Project generated successfully"
                };

                // Generate files based on the request
                response.GeneratedFiles = await GenerateProjectFilesAsync(request);
                response.ProjectStructure = GenerateProjectStructure(request);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project");
                return new CodeGenerationResponse
                {
                    Success = false,
                    Message = $"Error generating project: {ex.Message}"
                };
            }
        }

        public async Task<string> GenerateFromTemplateAsync(TemplateRequest request)
        {
            await Task.Delay(100); // Simulate async operation

            var templates = GetPredefinedTemplates();
            var template = templates.FirstOrDefault(t => t.Name.Equals(request.TemplateName, StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                throw new ArgumentException($"Template '{request.TemplateName}' not found");
            }

            var content = template.Content;

            // Replace parameters in the template
            foreach (var parameter in request.Parameters)
            {
                content = content.Replace($"{{{{{parameter.Key}}}}}", parameter.Value);
            }

            return content;
        }

        public async Task<List<Template>> GetAvailableTemplatesAsync()
        {
            await Task.Delay(50); // Simulate async operation
            return GetPredefinedTemplates();
        }

        public async Task<Template?> GetTemplateByIdAsync(int id)
        {
            await Task.Delay(50); // Simulate async operation
            var templates = GetPredefinedTemplates();
            return templates.FirstOrDefault(t => t.Id == id);
        }

        private async Task<List<GeneratedFile>> GenerateProjectFilesAsync(CodeGenerationRequest request)
        {
            await Task.Delay(100); // Simulate async operation

            var files = new List<GeneratedFile>();

            switch (request.Language.ToLower())
            {
                case "csharp":
                case "c#":
                    files.AddRange(GenerateCSharpProject(request));
                    break;
                case "javascript":
                case "js":
                    files.AddRange(GenerateJavaScriptProject(request));
                    break;
                case "python":
                    files.AddRange(GeneratePythonProject(request));
                    break;
                default:
                    files.Add(new GeneratedFile
                    {
                        FileName = "README.md",
                        FilePath = "README.md",
                        Content = $"# {request.ProjectName}\\n\\n{request.Description}",
                        FileType = "markdown"
                    });
                    break;
            }

            return files;
        }

        private List<GeneratedFile> GenerateCSharpProject(CodeGenerationRequest request)
        {
            var files = new List<GeneratedFile>();

            // Program.cs
            files.Add(new GeneratedFile
            {
                FileName = "Program.cs",
                FilePath = "Program.cs",
                Content = GenerateCSharpProgram(request),
                FileType = "csharp"
            });

            // Project file
            files.Add(new GeneratedFile
            {
                FileName = $"{request.ProjectName}.csproj",
                FilePath = $"{request.ProjectName}.csproj",
                Content = GenerateCSharpProjectFile(request),
                FileType = "xml"
            });

            // Model
            files.Add(new GeneratedFile
            {
                FileName = "Models/SampleModel.cs",
                FilePath = "Models/SampleModel.cs",
                Content = GenerateCSharpModel(request),
                FileType = "csharp"
            });

            return files;
        }

        private List<GeneratedFile> GenerateJavaScriptProject(CodeGenerationRequest request)
        {
            var files = new List<GeneratedFile>();

            files.Add(new GeneratedFile
            {
                FileName = "package.json",
                FilePath = "package.json",
                Content = GeneratePackageJson(request),
                FileType = "json"
            });

            files.Add(new GeneratedFile
            {
                FileName = "index.js",
                FilePath = "index.js",
                Content = GenerateJavaScriptIndex(request),
                FileType = "javascript"
            });

            return files;
        }

        private List<GeneratedFile> GeneratePythonProject(CodeGenerationRequest request)
        {
            var files = new List<GeneratedFile>();

            files.Add(new GeneratedFile
            {
                FileName = "main.py",
                FilePath = "main.py",
                Content = GeneratePythonMain(request),
                FileType = "python"
            });

            files.Add(new GeneratedFile
            {
                FileName = "requirements.txt",
                FilePath = "requirements.txt",
                Content = "flask>=2.0.0\\nrequests>=2.25.0",
                FileType = "text"
            });

            return files;
        }

        private string GenerateProjectStructure(CodeGenerationRequest request)
        {
            return request.Language.ToLower() switch
            {
                "csharp" or "c#" => $@"{request.ProjectName}/
├── Program.cs
├── {request.ProjectName}.csproj
├── Models/
│   └── SampleModel.cs
├── Controllers/
├── Services/
└── appsettings.json",
                "javascript" or "js" => $@"{request.ProjectName}/
├── package.json
├── index.js
├── src/
├── tests/
└── README.md",
                "python" => $@"{request.ProjectName}/
├── main.py
├── requirements.txt
├── src/
├── tests/
└── README.md",
                _ => $@"{request.ProjectName}/
├── README.md
└── src/"
            };
        }

        private List<Template> GetPredefinedTemplates()
        {
            return new List<Template>
            {
                new Template
                {
                    Id = 1,
                    Name = "CSharpClass",
                    Type = "class",
                    Language = "C#",
                    Description = "Basic C# class template",
                    RequiredParameters = new List<string> { "ClassName", "Namespace" },
                    Content = @"namespace {{Namespace}}
{
    public class {{ClassName}}
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public {{ClassName}}()
        {
        }
    }
}"
                },
                new Template
                {
                    Id = 2,
                    Name = "CSharpController",
                    Type = "controller",
                    Language = "C#",
                    Description = "ASP.NET Core API Controller template",
                    RequiredParameters = new List<string> { "ControllerName", "Namespace" },
                    Content = @"using Microsoft.AspNetCore.Mvc;

namespace {{Namespace}}.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    public class {{ControllerName}}Controller : ControllerBase
    {
        private readonly ILogger<{{ControllerName}}Controller> _logger;

        public {{ControllerName}}Controller(ILogger<{{ControllerName}}Controller> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(""Hello from {{ControllerName}}Controller"");
        }
    }
}"
                },
                new Template
                {
                    Id = 3,
                    Name = "JavaScriptClass",
                    Type = "class",
                    Language = "JavaScript",
                    Description = "JavaScript ES6 class template",
                    RequiredParameters = new List<string> { "ClassName" },
                    Content = @"class {{ClassName}} {
    constructor() {
        this.id = null;
        this.name = '';
    }

    // Add your methods here
    toString() {
        return `{{ClassName}} { id: ${this.id}, name: '${this.name}' }`;
    }
}

module.exports = {{ClassName}};"
                }
            };
        }

        private string GenerateCSharpProgram(CodeGenerationRequest request)
        {
            return $@"var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
";
        }

        private string GenerateCSharpProjectFile(CodeGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.4.0"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateCSharpModel(CodeGenerationRequest request)
        {
            return $@"namespace {request.ProjectName}.Models
{{
    public class SampleModel
    {{
        public int Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    }}
}}";
        }

        private string GeneratePackageJson(CodeGenerationRequest request)
        {
            return $@"{{
  ""name"": ""{request.ProjectName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""description"": ""{request.Description}"",
  ""main"": ""index.js"",
  ""scripts"": {{
    ""start"": ""node index.js"",
    ""dev"": ""nodemon index.js"",
    ""test"": ""jest""
  }},
  ""keywords"": [],
  ""author"": """",
  ""license"": ""ISC"",
  ""dependencies"": {{
    ""express"": ""^4.18.0""
  }},
  ""devDependencies"": {{
    ""nodemon"": ""^2.0.0"",
    ""jest"": ""^29.0.0""
  }}
}}";
        }

        private string GenerateJavaScriptIndex(CodeGenerationRequest request)
        {
            return $@"const express = require('express');
const app = express();
const port = process.env.PORT || 3000;

app.use(express.json());

app.get('/', (req, res) => {{
    res.json({{ message: 'Welcome to {request.ProjectName}!' }});
}});

app.listen(port, () => {{
    console.log(`{request.ProjectName} server running on port ${{port}}`);
}});";
        }

        private string GeneratePythonMain(CodeGenerationRequest request)
        {
            return $@"from flask import Flask, jsonify

app = Flask(__name__)

@app.route('/')
def hello():
    return jsonify({{
        'message': 'Welcome to {request.ProjectName}!',
        'description': '{request.Description}'
    }})

if __name__ == '__main__':
    app.run(debug=True, port=5000)";
        }
    }
}