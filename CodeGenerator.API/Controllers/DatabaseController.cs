using Microsoft.AspNetCore.Mvc;
using CodeGenerator.API.Services;
using CodeGenerator.API.Models;

namespace CodeGenerator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly IDatabaseDiscoveryService _databaseService;
        private readonly IDatabaseCodeGenerationService _codeGenerationService;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(
            IDatabaseDiscoveryService databaseService, 
            IDatabaseCodeGenerationService codeGenerationService,
            ILogger<DatabaseController> logger)
        {
            _databaseService = databaseService;
            _codeGenerationService = codeGenerationService;
            _logger = logger;
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isConnected = await _databaseService.TestConnectionAsync();
                return Ok(new { Success = isConnected, Message = isConnected ? "Database connection successful" : "Database connection failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Get all database tables with their schema information
        /// </summary>
        [HttpGet("tables")]
        public async Task<ActionResult<List<DatabaseTable>>> GetTables()
        {
            try
            {
                var tables = await _databaseService.GetTablesAsync();
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database tables");
                return StatusCode(500, new { Message = "Failed to retrieve database tables", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get schema information for a specific table
        /// </summary>
        [HttpGet("tables/{tableName}/schema")]
        public async Task<ActionResult<DatabaseTable>> GetTableSchema(string tableName, [FromQuery] string schema = "dbo")
        {
            try
            {
                var table = await _databaseService.GetTableSchemaAsync(tableName, schema);
                return Ok(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for table {TableName}.{Schema}", tableName, schema);
                return StatusCode(500, new { Message = $"Failed to retrieve schema for table {tableName}", Error = ex.Message });
            }
        }

        /// <summary>
        /// Generate code for selected database tables
        /// </summary>
        [HttpPost("generate-code")]
        public async Task<ActionResult<DatabaseCodeGenerationResult>> GenerateCode([FromBody] DatabaseCodeGenerationRequest request)
        {
            try
            {
                if (request.SelectedTables == null || !request.SelectedTables.Any())
                {
                    return BadRequest(new { Message = "At least one table must be selected for code generation" });
                }

                _logger.LogInformation("Starting code generation for {TableCount} tables", request.SelectedTables.Count);

                var result = await _codeGenerationService.GenerateCodeAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code");
                return StatusCode(500, new DatabaseCodeGenerationResult 
                { 
                    Success = false, 
                    Message = "Code generation failed", 
                    Errors = new List<string> { ex.Message } 
                });
            }
        }

        /// <summary>
        /// Clean up generated files
        /// </summary>
        [HttpPost("cleanup-files")]
        public async Task<ActionResult<CleanupResult>> CleanupFiles([FromBody] CleanupRequest request)
        {
            try
            {
                _logger.LogInformation("Starting cleanup of generated files");

                var result = await _codeGenerationService.CleanupGeneratedFilesAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up generated files");
                return StatusCode(500, new CleanupResult
                { 
                    Success = false, 
                    Message = "Cleanup failed", 
                    Errors = new List<string> { ex.Message },
                    DeletedFiles = new List<string>()
                });
            }
        }
    }
}