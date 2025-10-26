using CodeGenerator.API.Models;
using CodeGenerator.API.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeGenerator.API.Services
{
    public interface IDatabaseCodeGenerationService
    {
        Task<DatabaseCodeGenerationResult> GenerateCodeAsync(DatabaseCodeGenerationRequest request);
        Task<CleanupResult> CleanupGeneratedFilesAsync(CleanupRequest request);
    }

    public class DatabaseCodeGenerationService : IDatabaseCodeGenerationService
    {
        private readonly IDatabaseDiscoveryService _databaseService;
        private readonly IAngularCodeGenerationService _angularService;
        private readonly IApiCodeGenerationService _apiService;
        private readonly NavigationIntegrationService _navigationService;
        private readonly ILogger<DatabaseCodeGenerationService> _logger;
        private readonly string _projectRootPath;

        public DatabaseCodeGenerationService(
            IDatabaseDiscoveryService databaseService,
            IAngularCodeGenerationService angularService,
            IApiCodeGenerationService apiService,
            NavigationIntegrationService navigationService,
            ILogger<DatabaseCodeGenerationService> logger)
        {
            _databaseService = databaseService;
            _angularService = angularService;
            _apiService = apiService;
            _navigationService = navigationService;
            _logger = logger;
            _projectRootPath = ResolveProjectRootPath();
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
                    await GenerateCodeForTableAsync(tableFullName, request, generatedFiles, result.Errors);
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

        private async Task GenerateCodeForTableAsync(
            string tableFullName,
            DatabaseCodeGenerationRequest request,
            List<GeneratedFile> generatedFiles,
            List<string> errors)
        {
            try
            {
                var (schema, tableName) = ParseTableIdentifier(tableFullName);

                _logger.LogInformation("Generating code for table: {Schema}.{TableName}", schema, tableName);

                var table = await _databaseService.GetTableSchemaAsync(tableName, schema);

                if (table.Columns == null || !table.Columns.Any())
                {
                    errors.Add($"No columns found for table {schema}.{tableName}");
                    return;
                }

                if (request.GenerateAngularCode)
                {
                    var angularFiles = GenerateAngularFiles(table, request.AngularPath);

                    foreach (var file in angularFiles)
                    {
                        await WriteFileToDiskAsync(file);
                    }

                    await _navigationService.UpdateAppRoutesAsync(table, request.AngularPath);
                    await _navigationService.UpdateSidebarAsync(table, request.AngularPath);

                    generatedFiles.AddRange(angularFiles);
                }

                if (request.GenerateApiCode)
                {
                    var apiFiles = GenerateApiArtifacts(table, request.ApiControllersPath);

                    foreach (var file in apiFiles)
                    {
                        await WriteFileToDiskAsync(file);
                    }

                    generatedFiles.AddRange(apiFiles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code for table {TableName}", tableFullName);
                errors.Add($"Error generating code for table {tableFullName}: {ex.Message}");
            }
        }

        private List<GeneratedFile> GenerateAngularFiles(DatabaseTable table, string basePath)
        {
            var files = new List<GeneratedFile>();
            var camelCaseName = ToCamelCase(table.TableName);

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

            return files;
        }

        private List<GeneratedFile> GenerateApiArtifacts(DatabaseTable table, string? controllersPathOverride)
        {
            var controllersDirectory = GetApiControllersDirectory(controllersPathOverride);
            var modelsDirectory = GetApiModelsDirectory();
            var configurationsDirectory = GetApiConfigurationsDirectory();

            var options = new ApiGenerationOptions(controllersDirectory, modelsDirectory, configurationsDirectory);
            var artifacts = _apiService.GenerateApiArtifacts(table, options).ToList();

            _logger.LogInformation("Generated API artifacts for table {TableName}", table.TableName);

            return artifacts;
        }

        private string GetApiControllersDirectory(string? overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var candidate = Path.IsPathRooted(overridePath)
                    ? overridePath
                    : Path.Combine(_projectRootPath, overridePath);

                return Path.GetFullPath(candidate);
            }

            return Path.Combine(_projectRootPath, "Controllers", "Generated");
        }

        private string GetApiModelsDirectory()
        {
            return Path.Combine(_projectRootPath, "Models", "Generated");
        }

        private string GetApiConfigurationsDirectory()
        {
            return Path.Combine(_projectRootPath, "Data", "Generated");
        }

        private async Task WriteFileToDiskAsync(GeneratedFile file)
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(file.FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }

                // Write the file content
                await File.WriteAllTextAsync(file.FilePath, file.Content);
                _logger.LogInformation("Successfully wrote file: {FilePath}", file.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write file: {FilePath}", file.FilePath);
                throw new IOException($"Failed to write generated file '{file.FilePath}'", ex);
            }
        }

        public async Task<CleanupResult> CleanupGeneratedFilesAsync(CleanupRequest request)
        {
            var result = new CleanupResult
            {
                Success = false,
                DeletedFiles = new List<string>(),
                Errors = new List<string>()
            };

            try
            {
                _logger.LogInformation("Starting cleanup of generated files in {AngularPath}", request.AngularPath);

                var deletedFiles = new List<string>();
                var errors = new List<string>();
                var generatedTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Define the directories to clean
                var directoriesToClean = new[]
                {
                    Path.Combine(request.AngularPath, "models"),
                    Path.Combine(request.AngularPath, "services"), 
                    Path.Combine(request.AngularPath, "components")
                };

                foreach (var directory in directoriesToClean.Where(Directory.Exists))
                {
                    try
                    {
                        if (directory.EndsWith("components", StringComparison.OrdinalIgnoreCase))
                        {
                            // For components directory, only delete generated component folders (not existing ones like product-list, etc.)
                            await CleanupGeneratedComponentsAsync(directory, generatedTableNames, deletedFiles, errors);
                        }
                        else
                        {
                            // For models and services, delete generated files but keep existing ones
                            await CleanupGeneratedFilesInDirectoryAsync(directory, deletedFiles, errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error cleaning directory {directory}: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(ex, "Error cleaning directory {Directory}", directory);
                    }
                }

                await CleanupGeneratedApiArtifactsAsync(request.ApiControllersPath, deletedFiles, errors);

                // Cleanup navigation entries for generated components
                await CleanupNavigationEntriesAsync(generatedTableNames, request.AngularPath, errors);

                result.Success = errors.Count == 0;
                result.DeletedFiles = deletedFiles;
                result.Errors = errors;
                result.Message = result.Success 
                    ? $"Successfully deleted {deletedFiles.Count} files"
                    : $"Cleanup completed with {errors.Count} errors";

                _logger.LogInformation("Cleanup completed. Deleted {FileCount} files with {ErrorCount} errors", 
                    deletedFiles.Count, errors.Count);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Cleanup failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "Critical error during cleanup");
            }

            return result;
        }

        private Task CleanupGeneratedApiArtifactsAsync(string? controllersPathOverride, List<string> deletedFiles, List<string> errors)
        {
            try
            {
                var controllersDirectory = GetApiControllersDirectory(controllersPathOverride);
                DeleteGeneratedFilesInDirectory(controllersDirectory, "*Controller.cs", deletedFiles, errors);

                var modelsDirectory = GetApiModelsDirectory();
                DeleteGeneratedFilesInDirectory(modelsDirectory, "*.cs", deletedFiles, errors);

                var configurationsDirectory = GetApiConfigurationsDirectory();
                DeleteGeneratedFilesInDirectory(configurationsDirectory, "*Configuration.cs", deletedFiles, errors);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to cleanup API artifacts: {ex.Message}";
                errors.Add(errorMessage);
                _logger.LogError(ex, "Error while cleaning generated API artifacts");
            }

            return Task.CompletedTask;
        }

        private void DeleteGeneratedFilesInDirectory(string directory, string searchPattern, List<string> deletedFiles, List<string> errors)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    deletedFiles.Add(file);
                    _logger.LogInformation("Deleted generated API artifact: {File}", file);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to delete file {file}: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to delete generated API artifact {File}", file);
                }
            }

            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    _logger.LogInformation("Deleted empty directory: {Directory}", directory);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to delete directory {directory}: {ex.Message}";
                errors.Add(errorMessage);
                _logger.LogError(ex, "Failed to delete generated directory {Directory}", directory);
            }
        }

        private async Task CleanupNavigationEntriesAsync(HashSet<string> tableNames, string angularPath, List<string> errors)
        {
            try
            {
                // Remove navigation entries for each generated table
                foreach (var tableName in tableNames)
                {
                    try
                    {
                        await _navigationService.RemoveFromAppRoutesAsync(tableName, angularPath);
                        await _navigationService.RemoveFromSidebarAsync(tableName, angularPath);
                        _logger.LogInformation("Removed navigation entries for {TableName}", tableName);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to remove navigation entries for {tableName}: {ex.Message}");
                        _logger.LogError(ex, "Failed to remove navigation entries for {TableName}", tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to cleanup navigation entries: {ex.Message}");
                _logger.LogError(ex, "Error during navigation cleanup");
            }
        }

        private static string ExtractTableNameFromComponentDirectory(string directoryName)
        {
            // Convert component directory name back to table name
            // e.g., "authors-list" -> "Authors", "book-categories-form" -> "BookCategories"
            if (directoryName.EndsWith("-list") || directoryName.EndsWith("-form"))
            {
                var baseName = directoryName.Substring(0, directoryName.Length - 5);
                return ToPascalCase(baseName.Replace("-", ""));
            }

            return string.Empty;
        }

        private async Task CleanupGeneratedComponentsAsync(string componentsDirectory, HashSet<string> tableNames, List<string> deletedFiles, List<string> errors)
        {
            var directories = Directory.GetDirectories(componentsDirectory);
            
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                
                // Only delete directories that match generated patterns (exclude existing ones like product-list, product-form, etc.)
                if (IsGeneratedComponentDirectory(dirName))
                {
                    try
                    {
                        var tableName = ExtractTableNameFromComponentDirectory(dirName);
                        if (!string.IsNullOrEmpty(tableName))
                        {
                            tableNames.Add(tableName);
                        }
                        
                        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            File.Delete(file);
                            deletedFiles.Add(file);
                        }
                        Directory.Delete(dir, true);
                        _logger.LogInformation("Deleted generated component directory: {Directory}", dir);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to delete component directory {dir}: {ex.Message}");
                        _logger.LogError(ex, "Failed to delete generated component directory {Directory}", dir);
                    }
                }
            }
        }

        private async Task CleanupGeneratedFilesInDirectoryAsync(string directory, List<string> deletedFiles, List<string> errors)
        {
            var files = Directory.GetFiles(directory);
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                
                // Only delete files that match generated patterns (exclude existing ones like product.model.ts, product.service.ts, etc.)
                if (IsGeneratedFile(fileName))
                {
                    try
                    {
                        File.Delete(file);
                        deletedFiles.Add(file);
                        _logger.LogInformation("Deleted generated file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to delete file {file}: {ex.Message}");
                        _logger.LogError(ex, "Failed to delete generated file {File}", file);
                    }
                }
            }
        }

        private static bool IsGeneratedComponentDirectory(string dirName)
        {
            // Skip existing components (product-list, product-form, dashboard, login, etc.)
            var existingComponents = new[] { "product-list", "product-form", "dashboard", "login", "header", "sidebar", "footer", "code-generator" };
            
            if (existingComponents.Contains(dirName.ToLowerInvariant()))
            {
                return false;
            }

            // Consider it generated if it follows the pattern: tablename-list or tablename-form
            return dirName.EndsWith("-list") || dirName.EndsWith("-form");
        }

        private static bool IsGeneratedFile(string fileName)
        {
            // Skip existing files (product.model.ts, product.service.ts, database.service.ts, etc.)
            var existingFiles = new[] { "product.model.ts", "product.service.ts", "database.service.ts" };
            
            if (existingFiles.Contains(fileName.ToLowerInvariant()))
            {
                return false;
            }

            // Consider it generated if it matches typical generated patterns but isn't in the existing files list
            return (fileName.EndsWith(".model.ts") || fileName.EndsWith(".service.ts")) && 
                   !existingFiles.Contains(fileName.ToLowerInvariant());
        }

        private static (string Schema, string TableName) ParseTableIdentifier(string tableFullName)
        {
            if (string.IsNullOrWhiteSpace(tableFullName))
            {
                return ("dbo", string.Empty);
            }

            var parts = tableFullName.Split('.', 2);
            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }

            return ("dbo", parts[0]);
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

        private static string ResolveProjectRootPath()
        {
            var directoryInfo = new DirectoryInfo(AppContext.BaseDirectory);

            while (directoryInfo != null)
            {
                var projectFilePath = Path.Combine(directoryInfo.FullName, "CodeGenerator.API.csproj");
                if (File.Exists(projectFilePath))
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the CodeGenerator.API project directory.");
        }
    }
}