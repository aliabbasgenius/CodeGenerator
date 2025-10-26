using System.ComponentModel.DataAnnotations;

namespace CodeGenerator.API.Models
{
    public class DatabaseTable
    {
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public List<DatabaseColumn> Columns { get; set; } = new List<DatabaseColumn>();
        public bool IsSelected { get; set; } = false;
    }

    public class DatabaseColumn
    {
        private const string StringTypeScript = "string";
        private const string NumberTypeScript = "number";
        private const string BooleanTypeScript = "boolean";
        private const string DateTypeScript = "Date";

        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public int? MaxLength { get; set; }
        public bool IsForeignKey { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }
        public string CSharpType => GetCSharpType();
        public string TypeScriptType => GetTypeScriptType();

        private string GetCSharpType()
        {
            var nullable = IsNullable && !IsPrimaryKey ? "?" : "";
            return DataType.ToLower() switch
            {
                "int" or "integer" => $"int{nullable}",
                "bigint" => $"long{nullable}",
                "smallint" => $"short{nullable}",
                "tinyint" => $"byte{nullable}",
                "bit" => $"bool{nullable}",
                "decimal" or "numeric" or "money" or "smallmoney" => $"decimal{nullable}",
                "float" or "real" => $"double{nullable}",
                "datetime" or "datetime2" or "smalldatetime" => $"DateTime{nullable}",
                "date" => $"DateOnly{nullable}",
                "time" => $"TimeOnly{nullable}",
                "datetimeoffset" => $"DateTimeOffset{nullable}",
                "uniqueidentifier" => $"Guid{nullable}",
                "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => "string",
                "varbinary" or "binary" or "image" => "byte[]",
                _ => "string"
            };
        }

        private string GetTypeScriptType()
        {
            return DataType.ToLower() switch
            {
                "int" or "integer" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric" 
                or "money" or "smallmoney" or "float" or "real" => NumberTypeScript,
                "bit" => BooleanTypeScript,
                "datetime" or "datetime2" or "smalldatetime" or "date" or "time" or "datetimeoffset" => DateTypeScript,
                "uniqueidentifier" => StringTypeScript,
                "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => StringTypeScript,
                "varbinary" or "binary" or "image" => StringTypeScript, // Base64 encoded
                _ => StringTypeScript
            };
        }
    }

    public class DatabaseCodeGenerationRequest
    {
        [Required]
        public List<string> SelectedTables { get; set; } = new List<string>();
        
        public string OutputPath { get; set; } = "./generated";
        public bool GenerateAngularCode { get; set; } = true;
        public bool GenerateApiCode { get; set; } = false;
        public string AngularPath { get; set; } = "../AngularApp/src/app";
        public string ApiControllersPath { get; set; } = string.Empty;
    }

    public class DatabaseCodeGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class CleanupRequest
    {
        [Required]
        public string AngularPath { get; set; } = string.Empty;
        public string ApiControllersPath { get; set; } = string.Empty;
    }

    public class CleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> DeletedFiles { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}