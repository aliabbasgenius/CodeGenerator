using CodeGenerator.API.Models;
using System.Linq;
using System.Text;
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
        var naming = GetEntityNaming(table.TableName);
        var className = naming.SingularPascal;
        var singularFolder = naming.SingularCamel;
        var componentFolder = naming.PluralCamel;
        var routeSegment = naming.PluralKebab;
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

            if (!Regex.IsMatch(content, listImportPattern, RegexOptions.IgnoreCase))
            {
                // Add imports after existing imports
                var importInsertPoint = content.LastIndexOf("import { authGuard }");
                if (importInsertPoint != -1)
                {
                    var endOfLine = content.IndexOf('\n', importInsertPoint);
                    var newImports = $"\nimport {{ {className}List }} from './components/{componentFolder}-list/{componentFolder}-list';\nimport {{ {className}Form }} from './components/{componentFolder}-form/{componentFolder}-form';";
                    content = content.Insert(endOfLine, newImports);
                }
            }
            else
            {
                // Normalize previously generated imports that used singular folder names
                var oldListPath = $"./components/{singularFolder}-list/{singularFolder}-list";
                var oldFormPath = $"./components/{singularFolder}-form/{singularFolder}-form";
                var newListPath = $"./components/{componentFolder}-list/{componentFolder}-list";
                var newFormPath = $"./components/{componentFolder}-form/{componentFolder}-form";

                content = content.Replace(oldListPath, newListPath);
                content = content.Replace(oldFormPath, newFormPath);
            }

            // Check if routes already exist
            var routePattern = $@"path:\s*'{routeSegment}'";
            if (!Regex.IsMatch(content, routePattern, RegexOptions.IgnoreCase))
            {
                // Add routes before the wildcard route
                var wildcardIndex = content.IndexOf("{ path: '**'");
                if (wildcardIndex != -1)
                {
                    var newRoutes = $"  {{ path: '{routeSegment}', component: {className}List, canActivate: [authGuard] }},\n" +
                                  $"  {{ path: '{routeSegment}/new', component: {className}Form, canActivate: [authGuard] }},\n" +
                                  $"  {{ path: '{routeSegment}/edit/:id', component: {className}Form, canActivate: [authGuard] }},\n  ";
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
    var naming = GetEntityNaming(table.TableName);
    var className = naming.SingularPascal;
    var displayName = ToDisplayName(className);
        var routeSegment = naming.PluralKebab;
        var sidebarFilePath = Path.Combine(angularPath, "components", "sidebar", "sidebar.ts");

        try
        {
            if (!File.Exists(sidebarFilePath))
            {
                _logger.LogWarning("Sidebar file not found at {FilePath}", sidebarFilePath);
                return;
            }

            var content = await File.ReadAllTextAsync(sidebarFilePath);

            // Normalize previously generated titles without spacing
            var rawTitlePattern = $"title:\\s*'{Regex.Escape(className)}'";
            if (Regex.IsMatch(content, rawTitlePattern, RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, rawTitlePattern, $"title: '{displayName}'", RegexOptions.IgnoreCase);
            }

            // Check if menu item already exists
            var menuItemPattern = $@"title:\s*'(?:{Regex.Escape(className)}|{Regex.Escape(displayName)})'";
            if (!Regex.IsMatch(content, menuItemPattern, RegexOptions.IgnoreCase))
            {
                // Find the menuItems array and add the new item before Settings
                var settingsPattern = @"{\s*title:\s*'Settings'[^}]*}";
                var settingsMatch = Regex.Match(content, settingsPattern);

                if (settingsMatch.Success)
                {
                    var newMenuItem = $"    {{ title: '{displayName}', icon: '📋', route: '/{routeSegment}' }},\n    ";
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
                            var newMenuItem = $",\n    {{ title: '{displayName}', icon: '📋', route: '/{routeSegment}' }}";
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
        var naming = GetEntityNaming(tableName);
        var className = naming.SingularPascal;
        var routeSegment = naming.PluralKebab;
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
                $@"\s*{{\s*path:\s*'{routeSegment}',\s*component:\s*{className}List[^}}]*}},?\s*\n?",
                $@"\s*{{\s*path:\s*'{routeSegment}/new',\s*component:\s*{className}Form[^}}]*}},?\s*\n?",
                $@"\s*{{\s*path:\s*'{routeSegment}/edit/:id',\s*component:\s*{className}Form[^}}]*}},?\s*\n?"
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
        var naming = GetEntityNaming(tableName);
        var className = naming.SingularPascal;
        var sidebarFilePath = Path.Combine(angularPath, "components", "sidebar", "sidebar.ts");

        try
        {
            if (!File.Exists(sidebarFilePath))
                return;

            var displayName = ToDisplayName(className);
            var content = await File.ReadAllTextAsync(sidebarFilePath);

            // Normalize previously generated titles without spacing
            var rawTitlePattern = $"title:\\s*'{Regex.Escape(className)}'";
            if (Regex.IsMatch(content, rawTitlePattern, RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, rawTitlePattern, $"title: '{displayName}'", RegexOptions.IgnoreCase);
            }

            // Remove menu item
            var menuItemPattern = $@"\s*{{\s*title:\s*'(?:{Regex.Escape(className)}|{Regex.Escape(displayName)})'[^}}]*}},?\s*\n?";
            content = Regex.Replace(content, menuItemPattern, "", RegexOptions.IgnoreCase);

            await File.WriteAllTextAsync(sidebarFilePath, content);
            _logger.LogInformation("Removed menu item for {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing menu item for {TableName}", tableName);
        }
    }

    private static EntityNaming GetEntityNaming(string tableName)
    {
        var baseName = ToPascalCase(tableName);
        var singular = Singularize(baseName);
        var plural = Pluralize(singular);

        return new EntityNaming(
            singular,
            ToCamelFromPascal(singular),
            ToCamelFromPascal(plural),
            ToKebabCase(plural));
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (input.Contains('.'))
            input = input[(input.LastIndexOf('.') + 1)..];

        var sanitized = input.Replace("_", " ").Replace("-", " ");
        var tokens = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var words = tokens
            .SelectMany(token => Regex.Matches(token, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\\d+").Select(m => m.Value))
            .ToList();

        if (words.Count == 0)
            return char.ToUpperInvariant(input[0]) + input.Substring(1);

        static string FormatWord(string word)
        {
            if (word.All(char.IsDigit))
                return word;

            if (word.Length <= 3 && word.All(char.IsUpper))
                return word.ToUpperInvariant();

            return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        }

        return string.Concat(words.Select(FormatWord));
    }

    private static string Singularize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
            return name[..^3] + "y";

        if (name.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("zes", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ches", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("shes", StringComparison.OrdinalIgnoreCase))
            return name[..^2];

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("us", StringComparison.OrdinalIgnoreCase))
            return name[..^1];

        return name;
    }

    private static string Pluralize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1 && !IsVowel(name[^2]))
            return name[..^1] + "ies";

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            return name + "es";

        return name + "s";
    }

    private static bool IsVowel(char c)
    {
        return "aeiou".Contains(char.ToLowerInvariant(c));
    }

    private static string ToCamelFromPascal(string pascal)
    {
        return string.IsNullOrEmpty(pascal)
            ? pascal
            : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var sb = new System.Text.StringBuilder(name.Length * 2);
        for (int i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (char.IsUpper(ch))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }

    private static string ToDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var matches = Regex.Matches(name, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\\d+");
        if (matches.Count == 0)
            return name;

        var words = matches
            .Select(match => match.Value)
            .Select(value => value.Length <= 2 && value.All(char.IsUpper)
                ? value.ToUpperInvariant()
                : char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant())
            .ToList();

        return string.Join(" ", words);
    }

    private sealed record EntityNaming(
        string SingularPascal,
        string SingularCamel,
        string PluralCamel,
        string PluralKebab);
}