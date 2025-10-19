namespace CodeGenerator.API.Models
{
    public class CodeGenerationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
        public string ProjectStructure { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class GeneratedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }
}