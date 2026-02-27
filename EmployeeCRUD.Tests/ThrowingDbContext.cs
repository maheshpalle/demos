using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using EmployeeCRUD.Data;

namespace EmployeeCRUD.Tests;

/// <summary>
/// A test-only DbContext subclass that can be configured to throw
/// <see cref="DbUpdateConcurrencyException"/> on the next call to
/// <see cref="SaveChangesAsync"/>.
/// </summary>
public class ThrowingDbContext : ApplicationDbContext
{
    public bool ShouldThrow { get; set; } = false;

    public ThrowingDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            var mockEntry = new Mock<IUpdateEntry>().Object;
            throw new DbUpdateConcurrencyException(
                "Simulated concurrency conflict",
                new List<IUpdateEntry> { mockEntry });
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
