using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using EmployeeCRUD.Controllers;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Tests;

public class HomeControllerTests
{
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _controller = new HomeController(NullLogger<HomeController>.Instance);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

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
        Assert.Null(Activity.Current); // no ambient Activity in unit tests
        _controller.HttpContext.TraceIdentifier = "test-trace-id";

        var result = _controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal("test-trace-id", model.RequestId);
    }
}
