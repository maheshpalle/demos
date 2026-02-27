using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;
using EmployeeCRUD.Controllers;
using EmployeeCRUD.Data;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Tests;

/// <summary>
/// Integration-style unit tests for <see cref="EmployeesController"/>.
/// Each test uses an isolated in-memory database so tests are fully independent.
/// <see cref="ITestOutputHelper"/> is injected by xUnit and used throughout to
/// emit structured diagnostic output that appears in the test runner for every
/// test run, making failures and execution flow easy to follow.
/// </summary>
public class EmployeesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeesController _controller;

    /// <summary>
    /// xUnit injects <see cref="ITestOutputHelper"/> automatically; its
    /// <c>WriteLine</c> calls appear in the test runner output window alongside
    /// pass/fail results, providing structured per-test diagnostic output.
    /// </summary>
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initialises a fresh in-memory database and controller for every test so
    /// that no state leaks between test cases.
    /// </summary>
    public EmployeesControllerTests(ITestOutputHelper output)
    {
        _output = output;

        // Each test gets its own uniquely-named in-memory database, preventing
        // data from one test from affecting another.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _output.WriteLine("[Setup] Creating in-memory ApplicationDbContext.");
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _output.WriteLine("[Setup] Building EmployeesController with test HTTP context.");
        _controller = BuildController(_context);
    }

    /// <summary>
    /// Disposes the DbContext after each test to release in-memory database resources.
    /// </summary>
    public void Dispose()
    {
        _output.WriteLine("[Teardown] Disposing ApplicationDbContext.");
        _context.Dispose();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an <see cref="EmployeesController"/> wired to the given context
    /// with a minimal HTTP context and a mock TempData provider, matching what
    /// the real ASP.NET pipeline would supply.
    /// TempData requires a real <see cref="ITempDataProvider"/>; <c>Mock.Of&lt;&gt;</c>
    /// supplies a no-op implementation that still allows reading and writing TempData entries.
    /// </summary>
    private static EmployeesController BuildController(ApplicationDbContext ctx)
    {
        var controller = new EmployeesController(ctx);
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    /// <summary>
    /// Builds a fully-populated <see cref="Employee"/> test fixture with a
    /// predictable, realistic set of field values.  Using a factory method
    /// keeps every test DRY and ensures consistent seed data.
    /// </summary>
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

    /// <summary>
    /// Verifies that GET /Employees returns the default view and passes an
    /// <see cref="IEnumerable{Employee}"/> model (even when the table is empty).
    /// This is the most basic smoke test for the list page.
    /// </summary>
    [Fact]
    public async Task Index_ReturnsViewWithEmployeeList()
    {
        _output.WriteLine("[Act] Calling Index() on an empty database.");
        var result = await _controller.Index();

        // The result must be a ViewResult – i.e. the controller did not redirect
        // or return an error response.
        var viewResult = Assert.IsType<ViewResult>(result);

        // The model must be a sequence of Employee objects (could be empty).
        Assert.IsAssignableFrom<IEnumerable<Employee>>(viewResult.Model);

        _output.WriteLine("[Assert] Index returned a ViewResult with an IEnumerable<Employee> model.");
    }

    // -----------------------------------------------------------------------
    // Details
    // -----------------------------------------------------------------------

    /// <summary>
    /// Passing <c>null</c> as the id bypasses database look-up entirely;
    /// the controller should immediately return 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Details(null).");
        var result = await _controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Details(null) correctly returned NotFoundResult.");
    }

    /// <summary>
    /// When the id is a valid integer but no matching employee row exists in the
    /// database, the controller should return 404 Not Found rather than throw.
    /// </summary>
    [Fact]
    public async Task Details_NonExistentId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Details(999) with no employees in the database.");
        var result = await _controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Details(999) correctly returned NotFoundResult for a missing record.");
    }

    /// <summary>
    /// When a matching employee exists, Details should return the default view
    /// with that exact employee as the model.
    /// </summary>
    [Fact]
    public async Task Details_ValidId_ReturnsViewWithEmployee()
    {
        // Arrange: seed a single employee with a known Id.
        var emp = CreateEmployee(10);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id} ({emp.FirstName} {emp.LastName}).");

        _output.WriteLine("[Act] Calling Details(10).");
        var result = await _controller.Details(10);

        // The view must be returned with the correct employee as its model.
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(10, model.Id);
        _output.WriteLine($"[Assert] Details(10) returned ViewResult with model Id={model.Id}.");
    }

    // -----------------------------------------------------------------------
    // Create GET
    // -----------------------------------------------------------------------

    /// <summary>
    /// The Create GET action simply renders an empty form; no database access
    /// occurs and the result must always be a ViewResult.
    /// </summary>
    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        _output.WriteLine("[Act] Calling Create() GET.");
        var result = _controller.Create();

        Assert.IsType<ViewResult>(result);
        _output.WriteLine("[Assert] Create GET returned a ViewResult (empty form).");
    }

    // -----------------------------------------------------------------------
    // Create POST
    // -----------------------------------------------------------------------

    /// <summary>
    /// When the posted model is valid (ModelState has no errors), the controller
    /// should persist the employee, set a success message in TempData, and
    /// redirect back to the Index action.
    /// </summary>
    [Fact]
    public async Task CreatePost_ValidModel_RedirectsToIndexAndSetsTempData()
    {
        // Arrange: Id=0 lets EF assign a generated key on insert.
        var emp = CreateEmployee(0);
        _output.WriteLine($"[Arrange] Built employee model (Id=0) for POST.");

        _output.WriteLine("[Act] Calling Create(emp) POST with a valid model.");
        var result = await _controller.Create(emp);

        // Expect a redirect to Index after a successful save.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        // The success banner message must be set so the Index view can display it.
        Assert.Equal("Employee created successfully.", _controller.TempData["SuccessMessage"]);
        _output.WriteLine($"[Assert] Redirected to '{redirect.ActionName}'. TempData[SuccessMessage]='{_controller.TempData["SuccessMessage"]}'.");
    }

    /// <summary>
    /// When ModelState contains validation errors (e.g. a required field is
    /// missing), the controller must re-render the form view with the original
    /// employee model so the user can correct their input.
    /// </summary>
    [Fact]
    public async Task CreatePost_InvalidModel_ReturnsViewWithEmployee()
    {
        // Arrange: inject a validation error to simulate a failed form submission.
        _controller.ModelState.AddModelError("FirstName", "Required");
        var emp = CreateEmployee(0);
        _output.WriteLine("[Arrange] Added ModelState error for 'FirstName' to simulate invalid form input.");

        _output.WriteLine("[Act] Calling Create(emp) POST with an invalid model.");
        var result = await _controller.Create(emp);

        // The same view should be returned so the user sees their input and the
        // validation errors; the model must be the same instance that was posted.
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(emp, viewResult.Model);
        _output.WriteLine("[Assert] Create POST with invalid model returned ViewResult with the original employee model.");
    }

    // -----------------------------------------------------------------------
    // Edit GET
    // -----------------------------------------------------------------------

    /// <summary>
    /// Passing <c>null</c> to Edit GET should return 404 immediately, without
    /// hitting the database.
    /// </summary>
    [Fact]
    public async Task EditGet_NullId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Edit(null) GET.");
        var result = await _controller.Edit(null);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Edit(null) GET correctly returned NotFoundResult.");
    }

    /// <summary>
    /// A valid integer id that does not correspond to any row in the database
    /// should produce a 404 Not Found response.
    /// </summary>
    [Fact]
    public async Task EditGet_NonExistentId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Edit(999) GET with no matching employee in the database.");
        var result = await _controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Edit(999) GET correctly returned NotFoundResult for a missing record.");
    }

    /// <summary>
    /// When a matching employee exists, Edit GET should return the default view
    /// populated with that employee's data so the user can modify it.
    /// </summary>
    [Fact]
    public async Task EditGet_ValidId_ReturnsViewWithEmployee()
    {
        // Arrange: persist an employee to be edited.
        var emp = CreateEmployee(20);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id} for edit.");

        _output.WriteLine("[Act] Calling Edit(20) GET.");
        var result = await _controller.Edit(20);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(20, model.Id);
        _output.WriteLine($"[Assert] Edit(20) GET returned ViewResult with model Id={model.Id}.");
    }

    // -----------------------------------------------------------------------
    // Edit POST
    // -----------------------------------------------------------------------

    /// <summary>
    /// The route id and the employee's Id property must match.  When they
    /// differ the controller returns 404 to prevent accidentally updating the
    /// wrong record.
    /// </summary>
    [Fact]
    public async Task EditPost_IdMismatch_ReturnsNotFound()
    {
        // Arrange: employee Id=30 but route id=99 — intentional mismatch.
        var emp = CreateEmployee(30);
        _output.WriteLine($"[Arrange] Created employee with Id={emp.Id}; will POST to route id=99.");

        _output.WriteLine("[Act] Calling Edit(99, emp) POST where route id != emp.Id.");
        var result = await _controller.Edit(99, emp);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Edit POST with mismatched id correctly returned NotFoundResult.");
    }

    /// <summary>
    /// When ModelState is invalid, Edit POST should re-render the edit form
    /// with the submitted employee so the user can see their changes alongside
    /// the validation errors.
    /// </summary>
    [Fact]
    public async Task EditPost_InvalidModel_ReturnsViewWithEmployee()
    {
        // Arrange: seed the employee and then simulate a validation failure.
        var emp = CreateEmployee(30);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _controller.ModelState.AddModelError("FirstName", "Required");
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id}; added ModelState error for 'FirstName'.");

        _output.WriteLine("[Act] Calling Edit(30, emp) POST with an invalid model.");
        var result = await _controller.Edit(30, emp);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(emp, viewResult.Model);
        _output.WriteLine("[Assert] Edit POST with invalid model returned ViewResult with the original employee model.");
    }

    /// <summary>
    /// A successful edit with a valid model should persist the changes, set a
    /// success message in TempData, and redirect to the Index action.
    /// </summary>
    [Fact]
    public async Task EditPost_ValidModel_RedirectsToIndexAndSetsTempData()
    {
        // Arrange: persist the original employee, then detach it so EF does not
        // complain about tracking the same key twice when we post the updated version.
        var emp = CreateEmployee(40);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _context.Entry(emp).State = EntityState.Detached;
        _output.WriteLine($"[Arrange] Seeded and detached employee Id={emp.Id}.");

        // Build the updated employee model with a changed first name.
        var updated = CreateEmployee(40);
        updated.FirstName = "Jane";
        _output.WriteLine($"[Arrange] Prepared updated employee: FirstName changed to '{updated.FirstName}'.");

        _output.WriteLine("[Act] Calling Edit(40, updated) POST with a valid model.");
        var result = await _controller.Edit(40, updated);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Employee updated successfully.", _controller.TempData["SuccessMessage"]);
        _output.WriteLine($"[Assert] Redirected to '{redirect.ActionName}'. TempData[SuccessMessage]='{_controller.TempData["SuccessMessage"]}'.");
    }

    // -----------------------------------------------------------------------
    // Edit POST – concurrency handling
    // -----------------------------------------------------------------------

    /// <summary>
    /// When a <see cref="DbUpdateConcurrencyException"/> is thrown and the
    /// employee no longer exists in the database (deleted by another user), the
    /// controller should gracefully return 404 instead of propagating the
    /// exception to the caller.
    /// </summary>
    [Fact]
    public async Task EditPost_ConcurrencyException_EmployeeNotExists_ReturnsNotFound()
    {
        // Arrange: use ThrowingDbContext with no pre-seeded employee so that
        // EmployeeExists() returns false when the concurrency handler queries it.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);
        _output.WriteLine("[Arrange] Created ThrowingDbContext with no seeded employee (Id=50).");

        // Activate the throw flag so the next SaveChangesAsync raises the exception.
        throwingCtx.ShouldThrow = true;
        var emp = CreateEmployee(50);
        _output.WriteLine("[Arrange] ShouldThrow=true; SaveChangesAsync will raise DbUpdateConcurrencyException.");

        _output.WriteLine("[Act] Calling Edit(50, emp) POST expecting concurrency exception → NotFound path.");
        var result = await controller.Edit(50, emp);

        // Since Id=50 does not exist, EmployeeExists returns false and the
        // controller must return NotFound rather than rethrow.
        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Controller returned NotFoundResult because employee Id=50 does not exist.");
    }

    /// <summary>
    /// When a <see cref="DbUpdateConcurrencyException"/> is thrown but the
    /// employee still exists in the database (a true concurrent edit conflict),
    /// the controller should rethrow the exception so the framework or a
    /// higher-level handler can deal with it appropriately.
    /// </summary>
    [Fact]
    public async Task EditPost_ConcurrencyException_EmployeeExists_RethrowsException()
    {
        // Arrange: seed the employee first so EmployeeExists() returns true.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);

        // Persist the employee so EmployeeExists returns true when the
        // concurrency handler runs.
        var emp = CreateEmployee(80);
        throwingCtx.Employees.Add(emp);
        await throwingCtx.SaveChangesAsync();
        throwingCtx.Entry(emp).State = EntityState.Detached;
        _output.WriteLine($"[Arrange] Seeded and detached employee Id={emp.Id} in ThrowingDbContext.");

        // Now activate the throw flag for the subsequent edit save.
        throwingCtx.ShouldThrow = true;
        var toEdit = CreateEmployee(80);
        _output.WriteLine("[Arrange] ShouldThrow=true; SaveChangesAsync will raise DbUpdateConcurrencyException.");

        _output.WriteLine("[Act] Calling Edit(80, toEdit) POST — expect the exception to be rethrown.");

        // Because the employee still exists, the controller must rethrow the
        // DbUpdateConcurrencyException rather than swallowing it.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => controller.Edit(80, toEdit));
        _output.WriteLine("[Assert] DbUpdateConcurrencyException was rethrown as expected (employee Id=80 still exists).");
    }

    // -----------------------------------------------------------------------
    // Delete GET
    // -----------------------------------------------------------------------

    /// <summary>
    /// Passing <c>null</c> to Delete GET should return 404 immediately without
    /// touching the database.
    /// </summary>
    [Fact]
    public async Task DeleteGet_NullId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Delete(null) GET.");
        var result = await _controller.Delete(null);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Delete(null) GET correctly returned NotFoundResult.");
    }

    /// <summary>
    /// A valid integer id that does not match any employee row should produce a
    /// 404 Not Found response on the Delete confirmation page.
    /// </summary>
    [Fact]
    public async Task DeleteGet_NonExistentId_ReturnsNotFound()
    {
        _output.WriteLine("[Act] Calling Delete(999) GET with no matching employee.");
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] Delete(999) GET correctly returned NotFoundResult for a missing record.");
    }

    /// <summary>
    /// When a matching employee exists, Delete GET must return the confirmation
    /// view populated with that employee so the user can review the record
    /// before confirming deletion.
    /// </summary>
    [Fact]
    public async Task DeleteGet_ValidId_ReturnsViewWithEmployee()
    {
        // Arrange: seed the employee to be deleted.
        var emp = CreateEmployee(60);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id} for delete confirmation page.");

        _output.WriteLine("[Act] Calling Delete(60) GET.");
        var result = await _controller.Delete(60);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Employee>(viewResult.Model);
        Assert.Equal(60, model.Id);
        _output.WriteLine($"[Assert] Delete(60) GET returned ViewResult with model Id={model.Id}.");
    }

    // -----------------------------------------------------------------------
    // Delete POST (DeleteConfirmed)
    // -----------------------------------------------------------------------

    /// <summary>
    /// After the user confirms deletion, the controller should remove the
    /// employee from the database, set a success message in TempData, and
    /// redirect to Index.  A subsequent FindAsync must return null to confirm
    /// the row was actually removed.
    /// </summary>
    [Fact]
    public async Task DeleteConfirmed_ExistingEmployee_DeletesAndRedirectsWithTempData()
    {
        // Arrange: seed the employee that will be deleted.
        var emp = CreateEmployee(70);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id}.");

        _output.WriteLine("[Act] Calling DeleteConfirmed(70) POST.");
        var result = await _controller.DeleteConfirmed(70);

        // Verify the redirect and success message.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Employee deleted successfully.", _controller.TempData["SuccessMessage"]);
        _output.WriteLine($"[Assert] Redirected to '{redirect.ActionName}'. TempData[SuccessMessage]='{_controller.TempData["SuccessMessage"]}'.");

        // Verify the record is gone from the database.
        var deletedEmp = await _context.Employees.FindAsync(70);
        Assert.Null(deletedEmp);
        _output.WriteLine("[Assert] Employee Id=70 is no longer present in the database.");
    }

    /// <summary>
    /// If the employee was already deleted (e.g. by another user) before
    /// DeleteConfirmed is called, the controller should still redirect to Index
    /// without raising an error — it is already in the desired end state.
    /// </summary>
    [Fact]
    public async Task DeleteConfirmed_NonExistentEmployee_RedirectsToIndexWithoutError()
    {
        _output.WriteLine("[Act] Calling DeleteConfirmed(999) POST with no matching employee.");
        var result = await _controller.DeleteConfirmed(999);

        // The controller should silently succeed and redirect.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _output.WriteLine($"[Assert] DeleteConfirmed(999) redirected to '{redirect.ActionName}' without error.");
    }

    // -----------------------------------------------------------------------
    // EmployeeExists (tested indirectly via Edit POST concurrency path)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies the "employee found" branch indirectly: if a valid Edit POST
    /// completes without errors it means <c>EmployeeExists</c> returned true
    /// (or was not invoked because no concurrency exception occurred) and the
    /// record was successfully located and updated.
    /// </summary>
    [Fact]
    public async Task EmployeeExists_ReturnsTrueForExistingEmployee()
    {
        // Arrange: seed the employee, then detach so the later Update call does
        // not conflict with a tracked entity.
        var emp = CreateEmployee(90);
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();
        _output.WriteLine($"[Arrange] Seeded employee Id={emp.Id}.");

        // Verify indirectly: an Edit that succeeds confirms the employee was found and updated.
        _context.Entry(emp).State = EntityState.Detached;
        var updated = CreateEmployee(90);
        _output.WriteLine("[Arrange] Detached original; built updated model for Id=90.");

        _output.WriteLine("[Act] Calling Edit(90, updated) POST — expects successful redirect.");
        var result = await _controller.Edit(90, updated);

        Assert.IsType<RedirectToActionResult>(result);
        _output.WriteLine("[Assert] Edit succeeded (redirected), confirming employee Id=90 exists.");
    }

    /// <summary>
    /// Verifies the "employee not found" branch of <c>EmployeeExists</c>
    /// indirectly: by provoking a concurrency exception on a context that has
    /// no employee with the given id, the controller must return 404, proving
    /// <c>EmployeeExists</c> evaluated to false.
    /// </summary>
    [Fact]
    public async Task EmployeeExists_ReturnsFalseForNonExistingEmployee_ViaNotFound()
    {
        // Arrange: create a separate ThrowingDbContext with no seeded employees
        // so that EmployeeExists(91) returns false.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var throwingCtx = new ThrowingDbContext(options);
        throwingCtx.Database.EnsureCreated();
        var controller = BuildController(throwingCtx);
        _output.WriteLine("[Arrange] Created ThrowingDbContext with no employee for Id=91.");

        // No employee with Id=91 in the DB; concurrency path should return NotFound.
        throwingCtx.ShouldThrow = true;
        var emp = CreateEmployee(91);
        _output.WriteLine("[Arrange] ShouldThrow=true; SaveChangesAsync will raise DbUpdateConcurrencyException.");

        _output.WriteLine("[Act] Calling Edit(91, emp) POST — expects NotFound because Id=91 does not exist.");
        var result = await controller.Edit(91, emp);

        Assert.IsType<NotFoundResult>(result);
        _output.WriteLine("[Assert] NotFoundResult returned, confirming EmployeeExists returned false for Id=91.");
    }
}
