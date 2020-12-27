using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Szlem.Models.Editions;

using Szlem.Models.Schools;
using Szlem.Persistence.EF;
using Xunit;

public class MappingTests
{
    [Fact]
    public async Task MigrationTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Szlem1Db-test;Trusted_Connection=True;MultipleActiveResultSets=true",
                x => x.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .Options;

        using (var context = new AppDbContext(options, null))
        {
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.MigrateAsync();
            }
            finally
            {
                await context.Database.EnsureDeletedAsync();
            }
        }
    }

    #region Editions

    [Fact]
    public async Task EditionMappingsTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(EditionMappingsTest))
            .Options;

        using (var context = new AppDbContext(options, null))
        {
            await context.Set<Edition>().FirstOrDefaultAsync();
        }
    }

    #endregion


    #region Schools

    [Fact]
    public async Task SchoolMappingsTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(SchoolMappingsTest))
            .Options;

        using (var context = new AppDbContext(options, null))
        {
            await context.Set<School>().FirstOrDefaultAsync();
        }
    }
    
    #endregion

}
