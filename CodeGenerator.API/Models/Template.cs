namespace CodeGenerator.API.Models
{
    public class TemplateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // class, interface, controller, etc.
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredParameters { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}