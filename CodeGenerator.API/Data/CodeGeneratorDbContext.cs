using Microsoft.EntityFrameworkCore;

namespace CodeGenerator.API.Data;

public partial class CodeGeneratorDbContext : DbContext
{
    public CodeGeneratorDbContext(DbContextOptions<CodeGeneratorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodeGeneratorDbContext).Assembly);
    }
}
