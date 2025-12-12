using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SetlistStudio.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// This allows EF Core tools to create the DbContext without running the full application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SetlistStudioDbContext>
{
    public SetlistStudioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SetlistStudioDbContext>();
        
        // Use SQLite for migrations (default development database)
        optionsBuilder.UseSqlite("Data Source=setliststudio.db");
        
        return new SetlistStudioDbContext(optionsBuilder.Options);
    }
}
