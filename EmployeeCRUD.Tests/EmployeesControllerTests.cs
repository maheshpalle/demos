using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using EmployeeCRUD.Controllers;
using EmployeeCRUD.Data;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Tests;

public class EmployeesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _controller = BuildController(_context);
    }

    public void Dispose() => _context.Dispose();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static EmployeesController BuildController(ApplicationDbContext ctx)
    {
        var controller = new EmployeesController(ctx);
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private Employee CreateEmployee(int id) => new Employee
    {
        Id = id,
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        Department = "Engineering",
        JobTitle = "Software Engineer",
        Salary = 75000,
        DateOfJoining = new DateTime(2022, 1, 15)
    };

    // -----------------------------------------------------------------------
    // Index
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Index_ReturnsViewWithEmployeeList()
    {
        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<Employee>>(viewResult.Model);
    }

    // -----------------------------------------------------------------------
    // Details
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        var result = await _controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_NonExistentId_ReturnsNotFound()
    {
        var result = await _controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ValidId_ReturnsViewWithEmployee()
    {
        var emp = CreateEmployee(10);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(10);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(10, model.Id);
    }

    // -----------------------------------------------------------------------
    // Create GET
    // -----------------------------------------------------------------------

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        var result = _controller.Create();

        Assert.IsType<ViewResult>(result);
    }

    // -----------------------------------------------------------------------
    // Create POST
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreatePost_ValidModel_RedirectsToIndexAndSetsTempData()
    {
        var emp = CreateEmployee(0);

        var result = await _controller.Create(emp);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Employee created successfully.", _controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task CreatePost_InvalidModel_ReturnsViewWithEmployee()
    {
        _controller.ModelState.AddModelError("FirstName", "Required");
        var emp = CreateEmployee(0);

        var result = await _controller.Create(emp);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(emp, viewResult.Model);
    }

    // -----------------------------------------------------------------------
    // Edit GET
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EditGet_NullId_ReturnsNotFound()
    {
        var result = await _controller.Edit(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditGet_NonExistentId_ReturnsNotFound()
    {
        var result = await _controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditGet_ValidId_ReturnsViewWithEmployee()
    {
        var emp = CreateEmployee(20);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(20);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(20, model.Id);
    }

    // -----------------------------------------------------------------------
    // Edit POST
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EditPost_IdMismatch_ReturnsNotFound()
    {
        var emp = CreateEmployee(30);

        var result = await _controller.Edit(99, emp);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_InvalidModel_ReturnsViewWithEmployee()
    {
        var emp = CreateEmployee(30);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _controller.ModelState.AddModelError("FirstName", "Required");

        var result = await _controller.Edit(30, emp);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(emp, viewResult.Model);
    }

    [Fact]
    public async Task EditPost_ValidModel_RedirectsToIndexAndSetsTempData()
    {
        var emp = CreateEmployee(40);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _context.Entry(emp).State = EntityState.Detached;

        var updated = CreateEmployee(40);
        updated.FirstName = "Jane";

        var result = await _controller.Edit(40, updated);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Employee updated successfully.", _controller.TempData["SuccessMessage"]);
    }

    // -----------------------------------------------------------------------
    // Edit POST – concurrency handling
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EditPost_ConcurrencyException_EmployeeNotExists_ReturnsNotFound()
    {
        // ThrowingDbContext has no employee with Id=50 → EmployeeExists returns false
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);

        throwingCtx.ShouldThrow = true;
        var emp = CreateEmployee(50);

        var result = await controller.Edit(50, emp);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_ConcurrencyException_EmployeeExists_RethrowsException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);

        // Persist the employee so EmployeeExists returns true
        var emp = CreateEmployee(80);
        throwingCtx.Employees.Add(emp);
        await throwingCtx.SaveChangesAsync();
        throwingCtx.Entry(emp).State = EntityState.Detached;

        throwingCtx.ShouldThrow = true;
        var toEdit = CreateEmployee(80);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => controller.Edit(80, toEdit));
    }

    // -----------------------------------------------------------------------
    // Delete GET
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteGet_NullId_ReturnsNotFound()
    {
        var result = await _controller.Delete(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteGet_NonExistentId_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteGet_ValidId_ReturnsViewWithEmployee()
    {
        var emp = CreateEmployee(60);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var result = await _controller.Delete(60);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(60, model.Id);
    }

    // -----------------------------------------------------------------------
    // Delete POST (DeleteConfirmed)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteConfirmed_ExistingEmployee_DeletesAndRedirectsWithTempData()
    {
        var emp = CreateEmployee(70);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteConfirmed(70);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Employee deleted successfully.", _controller.TempData["SuccessMessage"]);
        Assert.Null(await _context.Employees.FindAsync(70));
    }

    [Fact]
    public async Task DeleteConfirmed_NonExistentEmployee_RedirectsToIndexWithoutError()
    {
        var result = await _controller.DeleteConfirmed(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // -----------------------------------------------------------------------
    // EmployeeExists (tested indirectly via Edit POST concurrency path)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EmployeeExists_ReturnsTrueForExistingEmployee()
    {
        var emp = CreateEmployee(90);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        // Verify indirectly: an Edit that succeeds confirms the employee was found and updated
        _context.Entry(emp).State = EntityState.Detached;
        var updated = CreateEmployee(90);

        var result = await _controller.Edit(90, updated);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task EmployeeExists_ReturnsFalseForNonExistingEmployee_ViaNotFound()
    {
        // No employee with Id=91 in the DB; concurrency path should return NotFound
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);

        throwingCtx.ShouldThrow = true;
        var emp = CreateEmployee(91);

        var result = await controller.Edit(91, emp);

        Assert.IsType<NotFoundResult>(result);
    }
}
