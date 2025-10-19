using Microsoft.AspNetCore.Mvc;
using CodeGenerator.API.Models;
using CodeGenerator.API.Services;

namespace CodeGenerator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodeGeneratorController : ControllerBase
    {
        private readonly ICodeGenerationService _codeGenerationService;
        private readonly ILogger<CodeGeneratorController> _logger;

        public CodeGeneratorController(
            ICodeGenerationService codeGenerationService,
            ILogger<CodeGeneratorController> logger)
        {
            _codeGenerationService = codeGenerationService;
            _logger = logger;
        }

        /// <summary>
        /// Generate a complete project based on the provided specifications
        /// </summary>
        [HttpPost("generate-project")]
        public async Task<ActionResult<CodeGenerationResponse>> GenerateProject([FromBody] CodeGenerationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProjectName))
                {
                    return BadRequest("Project name is required");
                }

                if (string.IsNullOrEmpty(request.Language))
                {
                    return BadRequest("Language is required");
                }

                var result = await _codeGenerationService.GenerateProjectAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate code from a specific template
        /// </summary>
        [HttpPost("generate-from-template")]
        public async Task<ActionResult<string>> GenerateFromTemplate([FromBody] TemplateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TemplateName))
                {
                    return BadRequest("Template name is required");
                }

                var result = await _codeGenerationService.GenerateFromTemplateAsync(request);
                return Ok(new { content = result });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating from template");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all available templates
        /// </summary>
        [HttpGet("templates")]
        public async Task<ActionResult<List<Template>>> GetTemplates()
        {
            try
            {
                var templates = await _codeGenerationService.GetAvailableTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific template by ID
        /// </summary>
        [HttpGet("templates/{id}")]
        public async Task<ActionResult<Template>> GetTemplate(int id)
        {
            try
            {
                var template = await _codeGenerationService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    return NotFound(new { message = $"Template with ID {id} not found" });
                }
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get supported languages and frameworks
        /// </summary>
        [HttpGet("supported-technologies")]
        public ActionResult GetSupportedTechnologies()
        {
            var technologies = new
            {
                Languages = new[]
                {
                    new { Name = "C#", Key = "csharp", Frameworks = new[] { ".NET Core", "ASP.NET Core", ".NET Framework" } },
                    new { Name = "JavaScript", Key = "javascript", Frameworks = new[] { "Node.js", "Express", "React", "Vue", "Angular" } },
                    new { Name = "Python", Key = "python", Frameworks = new[] { "Flask", "Django", "FastAPI" } },
                    new { Name = "TypeScript", Key = "typescript", Frameworks = new[] { "Node.js", "Express", "NestJS" } }
                },
                ProjectTypes = new[]
                {
                    "Web API",
                    "Web Application",
                    "Console Application",
                    "Class Library",
                    "Microservice",
                    "Desktop Application"
                }
            };

            return Ok(technologies);
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }
    }
}