using CodeGenerator.API.Models;
using System.Text.RegularExpressions;

namespace CodeGenerator.API.Services;

public class NavigationIntegrationService
{
    private readonly ILogger<NavigationIntegrationService> _logger;

    public NavigationIntegrationService(ILogger<NavigationIntegrationService> logger)
    {
        _logger = logger;
    }

    public async Task UpdateAppRoutesAsync(DatabaseTable table, string angularPath)
    {
        var className = ToPascalCase(table.TableName);
        var camelCaseName = ToCamelCase(table.TableName);
        var routesFilePath = Path.Combine(angularPath, "app.routes.ts");

        try
        {
            if (!File.Exists(routesFilePath))
            {
                _logger.LogWarning("Routes file not found at {FilePath}", routesFilePath);
                return;
            }

            var content = await File.ReadAllTextAsync(routesFilePath);

            // Check if imports already exist
            var listImportPattern = $@"import\s*{{\s*{className}List\s*}}\s*from\s*'[^']*'";
            var formImportPattern = $@"import\s*{{\s*{className}Form\s*}}\s*from\s*'[^']*'";

            if (!Regex.IsMatch(content, listImportPattern, RegexOptions.IgnoreCase))
            {
                // Add imports after existing imports
                var importInsertPoint = content.LastIndexOf("import { authGuard }");
                if (importInsertPoint != -1)
                {
                    var endOfLine = content.IndexOf('\n', importInsertPoint);
                    var newImports = $"\nimport {{ {className}List }} from './components/{camelCaseName}-list/{camelCaseName}-list';\nimport {{ {className}Form }} from './components/{camelCaseName}-form/{camelCaseName}-form';";
                    content = content.Insert(endOfLine, newImports);
                }
            }

            // Check if routes already exist
            var routePattern = $@"path:\s*'{camelCaseName}'";
            if (!Regex.IsMatch(content, routePattern, RegexOptions.IgnoreCase))
            {
                // Add routes before the wildcard route
                var wildcardIndex = content.IndexOf("{ path: '**'");
                if (wildcardIndex != -1)
                {
                    var newRoutes = $"  {{ path: '{camelCaseName}', component: {className}List, canActivate: [authGuard] }},\n" +
                                  $"  {{ path: '{camelCaseName}/new', component: {className}Form, canActivate: [authGuard] }},\n" +
                                  $"  {{ path: '{camelCaseName}/edit/:id', component: {className}Form, canActivate: [authGuard] }},\n  ";
                    content = content.Insert(wildcardIndex, newRoutes);
                }
            }

            await File.WriteAllTextAsync(routesFilePath, content);
            _logger.LogInformation("Updated routes for {TableName}", table.TableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating routes for {TableName}", table.TableName);
        }
    }

    public async Task UpdateSidebarAsync(DatabaseTable table, string angularPath)
    {
        var className = ToPascalCase(table.TableName);
        var camelCaseName = ToCamelCase(table.TableName);
        var sidebarFilePath = Path.Combine(angularPath, "components", "sidebar", "sidebar.ts");

        try
        {
            if (!File.Exists(sidebarFilePath))
            {
                _logger.LogWarning("Sidebar file not found at {FilePath}", sidebarFilePath);
                return;
            }

            var content = await File.ReadAllTextAsync(sidebarFilePath);

            // Check if menu item already exists
            var menuItemPattern = $@"title:\s*'{className}'";
            if (!Regex.IsMatch(content, menuItemPattern, RegexOptions.IgnoreCase))
            {
                // Find the menuItems array and add the new item before Settings
                var settingsPattern = @"{\s*title:\s*'Settings'[^}]*}";
                var settingsMatch = Regex.Match(content, settingsPattern);

                if (settingsMatch.Success)
                {
                    var newMenuItem = $"    {{ title: '{className}', icon: '📋', route: '/{camelCaseName}' }},\n    ";
                    content = content.Insert(settingsMatch.Index, newMenuItem);
                }
                else
                {
                    // If Settings not found, add before the closing bracket of menuItems
                    var menuItemsPattern = @"menuItems:\s*MenuItem\[\]\s*=\s*\[[^\]]*\]";
                    var menuItemsMatch = Regex.Match(content, menuItemsPattern, RegexOptions.Singleline);
                    
                    if (menuItemsMatch.Success)
                    {
                        var closingBracketIndex = menuItemsMatch.Value.LastIndexOf(']');
                        if (closingBracketIndex != -1)
                        {
                            var insertPosition = menuItemsMatch.Index + closingBracketIndex;
                            var newMenuItem = $",\n    {{ title: '{className}', icon: '📋', route: '/{camelCaseName}' }}";
                            content = content.Insert(insertPosition, newMenuItem);
                        }
                    }
                }

                await File.WriteAllTextAsync(sidebarFilePath, content);
                _logger.LogInformation("Updated sidebar for {TableName}", table.TableName);
            }
            else
            {
                _logger.LogInformation("Menu item for {TableName} already exists in sidebar", table.TableName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sidebar for {TableName}", table.TableName);
        }
    }

    public async Task RemoveFromAppRoutesAsync(string tableName, string angularPath)
    {
        var className = ToPascalCase(tableName);
        var camelCaseName = ToCamelCase(tableName);
        var routesFilePath = Path.Combine(angularPath, "app.routes.ts");

        try
        {
            if (!File.Exists(routesFilePath))
                return;

            var content = await File.ReadAllTextAsync(routesFilePath);

            // Remove imports
            var listImportPattern = $@"import\s*{{\s*{className}List\s*}}\s*from\s*'[^']*';\s*\n?";
            var formImportPattern = $@"import\s*{{\s*{className}Form\s*}}\s*from\s*'[^']*';\s*\n?";
            content = Regex.Replace(content, listImportPattern, "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, formImportPattern, "", RegexOptions.IgnoreCase);

            // Remove routes
            var routePatterns = new[]
            {
                $@"\s*{{\s*path:\s*'{camelCaseName}',\s*component:\s*{className}List[^}}]*}},?\s*\n?",
                $@"\s*{{\s*path:\s*'{camelCaseName}/new',\s*component:\s*{className}Form[^}}]*}},?\s*\n?",
                $@"\s*{{\s*path:\s*'{camelCaseName}/edit/:id',\s*component:\s*{className}Form[^}}]*}},?\s*\n?"
            };

            foreach (var pattern in routePatterns)
            {
                content = Regex.Replace(content, pattern, "", RegexOptions.IgnoreCase);
            }

            await File.WriteAllTextAsync(routesFilePath, content);
            _logger.LogInformation("Removed routes for {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing routes for {TableName}", tableName);
        }
    }

    public async Task RemoveFromSidebarAsync(string tableName, string angularPath)
    {
        var className = ToPascalCase(tableName);
        var sidebarFilePath = Path.Combine(angularPath, "components", "sidebar", "sidebar.ts");

        try
        {
            if (!File.Exists(sidebarFilePath))
                return;

            var content = await File.ReadAllTextAsync(sidebarFilePath);

            // Remove menu item
            var menuItemPattern = $@"\s*{{\s*title:\s*'{className}'[^}}]*}},?\s*\n?";
            content = Regex.Replace(content, menuItemPattern, "", RegexOptions.IgnoreCase);

            await File.WriteAllTextAsync(sidebarFilePath, content);
            _logger.LogInformation("Removed menu item for {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing menu item for {TableName}", tableName);
        }
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove schema prefix if present (e.g., "dbo.Authors" -> "Authors")
        if (input.Contains('.'))
            input = input.Substring(input.LastIndexOf('.') + 1);

        return string.Join("", input.Split('_')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }

    private static string ToCamelCase(string input)
    {
        var pascalCase = ToPascalCase(input);
        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }
}