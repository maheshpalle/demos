using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using EmployeeCRUD.Controllers;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Tests;

// -----------------------------------------------------------------------
// Ahoy! These be the tests for the HomeController — the ship's figurehead
// that greets all sailors who come aboard.  We make sure the home port,
// the Pirate Code (Privacy) page, and the emergency lifeboats (Error)
// all work shipshape and Bristol fashion!
// -----------------------------------------------------------------------

public class HomeControllerTests
{
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        // Man the helm with a null logger — no logbook required for unit testing
        _controller = new HomeController(NullLogger<HomeController>.Instance);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // -----------------------------------------------------------------------
    // Index — Welcome to the home port!
    // -----------------------------------------------------------------------

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

    // -----------------------------------------------------------------------
    // Privacy — Read the Pirate Code!
    // -----------------------------------------------------------------------

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

    // -----------------------------------------------------------------------
    // Error — Man the lifeboats!
    // -----------------------------------------------------------------------

    [Fact]
    public void Error_ReturnsViewResultWithErrorViewModel()
    {
        var result = _controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.NotNull(model);
    }

    [Fact]
    public void Error_SetsRequestIdFromTraceIdentifier()
    {
        // No ambient Activity in unit tests — the seas are calm
        Assert.Null(Activity.Current);
        _controller.HttpContext.TraceIdentifier = "test-trace-id";

        var result = _controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal("test-trace-id", model.RequestId);
    }
}
