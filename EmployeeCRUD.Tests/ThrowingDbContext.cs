using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using EmployeeCRUD.Data;

namespace EmployeeCRUD.Tests;

/// <summary>
/// Ahoy! This be a test-only DbContext — a saboteur's trick to simulate rough seas
/// (concurrency conflicts) in unit tests without needing a real database battle.
/// When <see cref="ShouldThrow"/> is set to <c>true</c>, the next call to
/// <see cref="SaveChangesAsync"/> will hoist the Jolly Roger and throw a
/// <see cref="DbUpdateConcurrencyException"/>, just like two pirates grabbing
/// the same piece of treasure at the same time.
/// </summary>
public class ThrowingDbContext : ApplicationDbContext
{
    /// <summary>
    /// When <c>true</c>, the next <see cref="SaveChangesAsync"/> call will throw a
    /// <see cref="DbUpdateConcurrencyException"/> to simulate a concurrency conflict.
    /// Raise this flag to trigger the storm!
    /// </summary>
    public bool ShouldThrow { get; set; } = false;

    /// <summary>
    /// Initializes the saboteur context with the given database options.
    /// </summary>
    /// <param name="options">The in-memory database options used during testing.</param>
    public ThrowingDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    /// <inheritdoc/>
    /// <remarks>
    /// If <see cref="ShouldThrow"/> is <c>true</c>, this method throws a
    /// <see cref="DbUpdateConcurrencyException"/> instead of actually saving — arr!
    /// </remarks>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            // Fire the cannon! Simulate a concurrency conflict.
            var mockEntry = new Mock<IUpdateEntry>().Object;
            throw new DbUpdateConcurrencyException(
                "Simulated concurrency conflict",
                new List<IUpdateEntry> { mockEntry });
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
