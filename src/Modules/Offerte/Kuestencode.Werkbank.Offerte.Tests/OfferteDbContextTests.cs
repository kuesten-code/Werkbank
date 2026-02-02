using Kuestencode.Werkbank.Offerte.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests;

public class OfferteDbContextTests
{
    [Fact]
    public void DbContext_CanBeCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OfferteDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        // Act
        using var context = new OfferteDbContext(options);

        // Assert
        Assert.NotNull(context);
    }
}
