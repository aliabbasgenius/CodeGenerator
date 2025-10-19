using CodeGenerator.API.Models;
using CodeGenerator.API.Services;

namespace CodeGenerator.API.Services
{
    public interface IDatabaseCodeGenerationService
    {
        Task<DatabaseCodeGenerationResult> GenerateCodeAsync(DatabaseCodeGenerationRequest request);
    }

    public class DatabaseCodeGenerationService : IDatabaseCodeGenerationService
    {
        private readonly IDatabaseDiscoveryService _databaseService;
        private readonly IAngularCodeGenerationService _angularService;
        private readonly ILogger<DatabaseCodeGenerationService> _logger;

        public DatabaseCodeGenerationService(
            IDatabaseDiscoveryService databaseService,
            IAngularCodeGenerationService angularService,
            ILogger<DatabaseCodeGenerationService> logger)
        {
            _databaseService = databaseService;
            _angularService = angularService;
            _logger = logger;
        }

        public async Task<DatabaseCodeGenerationResult> GenerateCodeAsync(DatabaseCodeGenerationRequest request)
        {
            var result = new DatabaseCodeGenerationResult
            {
                Success = false,
                GeneratedFiles = new List<GeneratedFile>(),
                Errors = new List<string>()
            };

            try
            {
                _logger.LogInformation("Starting code generation for {TableCount} tables", request.SelectedTables.Count);

                var generatedFiles = new List<GeneratedFile>();

                foreach (var tableFullName in request.SelectedTables)
                {
                    try
                    {
                        var parts = tableFullName.Split('.');
                        var schema = parts.Length > 1 ? parts[0] : "dbo";
                        var tableName = parts.Length > 1 ? parts[1] : parts[0];

                        _logger.LogInformation("Generating code for table: {Schema}.{TableName}", schema, tableName);

                        // Get table schema
                        var table = await _databaseService.GetTableSchemaAsync(tableName, schema);

                        if (table.Columns == null || !table.Columns.Any())
                        {
                            result.Errors.Add($"No columns found for table {schema}.{tableName}");
                            continue;
                        }

                        // Generate Angular code if requested
                        if (request.GenerateAngularCode)
                        {
                            var angularFiles = GenerateAngularFiles(table, request.AngularPath);
                            generatedFiles.AddRange(angularFiles);
                        }

                        // Generate API code if requested
                        if (request.GenerateApiCode)
                        {
                            // TODO: Implement API code generation
                            _logger.LogInformation("API code generation not yet implemented");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating code for table {TableName}", tableFullName);
                        result.Errors.Add($"Error generating code for table {tableFullName}: {ex.Message}");
                    }
                }

                result.GeneratedFiles = generatedFiles;
                result.Success = result.Errors.Count == 0 || generatedFiles.Any();
                result.Message = result.Success 
                    ? $"Successfully generated {generatedFiles.Count} files for {request.SelectedTables.Count} table(s)"
                    : "Code generation completed with errors";

                _logger.LogInformation("Code generation completed. Generated {FileCount} files with {ErrorCount} errors", 
                    generatedFiles.Count, result.Errors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during code generation");
                result.Errors.Add($"Unexpected error: {ex.Message}");
                result.Message = "Code generation failed";
            }

            return result;
        }

        private List<GeneratedFile> GenerateAngularFiles(DatabaseTable table, string basePath)
        {
            var files = new List<GeneratedFile>();
            var className = ToPascalCase(table.TableName);
            var camelCaseName = ToCamelCase(table.TableName);

            try
            {
                // Generate Model
                var modelContent = _angularService.GenerateModel(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}.model.ts",
                    FilePath = $"{basePath}/models/{camelCaseName}.model.ts",
                    FileType = "model",
                    Content = modelContent
                });

                // Generate Service
                var serviceContent = _angularService.GenerateService(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}.service.ts",
                    FilePath = $"{basePath}/services/{camelCaseName}.service.ts",
                    FileType = "service",
                    Content = serviceContent
                });

                // Generate List Component TypeScript
                var listComponentContent = _angularService.GenerateListComponent(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-list.ts",
                    FilePath = $"{basePath}/components/{camelCaseName}-list/{camelCaseName}-list.ts",
                    FileType = "component",
                    Content = listComponentContent
                });

                // Generate List Component HTML
                var listHtmlContent = _angularService.GenerateListHtml(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-list.html",
                    FilePath = $"{basePath}/components/{camelCaseName}-list/{camelCaseName}-list.html",
                    FileType = "template",
                    Content = listHtmlContent
                });

                // Generate List Component CSS
                var listCssContent = _angularService.GenerateListCss(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-list.css",
                    FilePath = $"{basePath}/components/{camelCaseName}-list/{camelCaseName}-list.css",
                    FileType = "stylesheet",
                    Content = listCssContent
                });

                // Generate Form Component TypeScript
                var formComponentContent = _angularService.GenerateFormComponent(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-form.ts",
                    FilePath = $"{basePath}/components/{camelCaseName}-form/{camelCaseName}-form.ts",
                    FileType = "component",
                    Content = formComponentContent
                });

                // Generate Form Component HTML
                var formHtmlContent = _angularService.GenerateFormHtml(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-form.html",
                    FilePath = $"{basePath}/components/{camelCaseName}-form/{camelCaseName}-form.html",
                    FileType = "template",
                    Content = formHtmlContent
                });

                // Generate Form Component CSS
                var formCssContent = _angularService.GenerateFormCss(table);
                files.Add(new GeneratedFile
                {
                    FileName = $"{camelCaseName}-form.css",
                    FilePath = $"{basePath}/components/{camelCaseName}-form/{camelCaseName}-form.css",
                    FileType = "stylesheet",
                    Content = formCssContent
                });

                _logger.LogInformation("Generated {FileCount} Angular files for table {TableName}", files.Count, table.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Angular files for table {TableName}", table.TableName);
                throw;
            }

            return files;
        }

        private static string ToPascalCase(string input)
        {
            return string.Join("", input.Split('_', '-', ' ')
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
        }

        private static string ToCamelCase(string input)
        {
            var pascalCase = ToPascalCase(input);
            return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
        }
    }
}