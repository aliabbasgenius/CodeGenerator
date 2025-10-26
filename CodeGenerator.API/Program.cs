using CodeGenerator.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Code Generator API",
        Version = "v1",
        Description = "A powerful API for generating code projects and templates",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Code Generator Team",
            Email = "support@codegenerator.com"
        }
    });
});

// Register services
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
builder.Services.AddScoped<IAngularCodeGenerationService, AngularCodeGenerationService>();
builder.Services.AddScoped<IApiCodeGenerationService, ApiCodeGenerationService>();
builder.Services.AddScoped<IDatabaseCodeGenerationService, DatabaseCodeGenerationService>();
builder.Services.AddScoped<NavigationIntegrationService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Code Generator API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Enable CORS first
app.UseCors("AllowAngularApp");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/", () => new
{
    service = "Code Generator API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    endpoints = new[]
    {
        "/api/codegenerator/generate-project",
        "/api/codegenerator/generate-from-template",
        "/api/codegenerator/templates",
        "/api/codegenerator/supported-technologies",
        "/api/database/test-connection",
        "/api/database/tables",
        "/swagger"
    }
});

await app.RunAsync();
