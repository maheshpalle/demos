using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Controllers;

/// <summary>
/// Ahoy! This be the Home controller — the ship's figurehead that welcomes sailors to port
/// and handles the main navigation of the EmployeeCRUD galleon.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    /// <summary>
    /// Initializes the Home controller with the captain's logbook (logger).
    /// </summary>
    /// <param name="logger">The logger used to record notable voyages and misadventures.</param>
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Drops anchor at the home port — displays the application's main landing page.
    /// </summary>
    /// <returns>The default home view for weary sailors who have just come aboard.</returns>
    public IActionResult Index()
    {
        _logger.LogInformation("Ahoy! A sailor has arrived at the home port. Welcome aboard the EmployeeCRUD galleon!");
        return View();
    }

    /// <summary>
    /// Shows the privacy policy scroll — because even pirates respect the Pirate Code.
    /// </summary>
    /// <returns>The privacy policy view.</returns>
    public IActionResult Privacy()
    {
        _logger.LogInformation("A curious sailor is reading the Pirate Code (Privacy Policy).");
        return View();
    }

    /// <summary>
    /// Signals an error has occurred — man the lifeboats! Displays the error page with diagnostic information.
    /// </summary>
    /// <returns>An error view containing the <see cref="ErrorViewModel"/> with the current request's trace identifier.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Batten down the hatches — something went wrong in the rigging!
        _logger.LogError("Shiver me timbers! An error has been encountered on the voyage. RequestId: {RequestId}",
            Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
