using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace InternProject.EntityFrameworkCore;

public static class InternProjectDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<InternProjectDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<InternProjectDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
