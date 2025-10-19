namespace CodeGenerator.API.Models
{
    public class CodeGenerationRequest
    {
        public string Language { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }
}