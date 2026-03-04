using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeCRUD.Data;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Controllers
{
    /// <summary>
    /// Ahoy! This here controller be the captain of all employee CRUD operations.
    /// It manages the crew manifest — adding, viewing, editing, and removing sailors from the ship's roster.
    /// </summary>
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeesController> _logger;

        /// <summary>
        /// Initializes the controller with the ship's database context and the captain's logbook (logger).
        /// </summary>
        /// <param name="context">The database context for accessing the crew manifest.</param>
        /// <param name="logger">The logger used to record notable events in the captain's logbook.</param>
        public EmployeesController(ApplicationDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Displays the full crew manifest — all hands on deck!
        /// </summary>
        /// <returns>A view containing the list of all employees.</returns>
        // GET: Employees
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Ahoy! Fetching the full crew manifest from Davy Jones' locker (the database).");
            return View(await _context.Employees.ToListAsync());
        }

        /// <summary>
        /// Shows the detailed dossier of a specific crew member identified by their unique sailor ID.
        /// </summary>
        /// <param name="id">The unique identifier (sailor ID) of the crew member to inspect.</param>
        /// <returns>A view with the employee's details, or a 404 if the scallywag cannot be found.</returns>
        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // No ID? Walk the plank!
            if (id == null)
            {
                _logger.LogWarning("Shiver me timbers! Details requested without a sailor ID — returning NotFound.");
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                _logger.LogWarning("Blimey! No crew member found with sailor ID {EmployeeId} — they must have jumped ship.", id);
                return NotFound();
            }

            _logger.LogInformation("Pulled up the dossier for crew member {EmployeeId}: {FullName}.", id, employee.FullName);
            return View(employee);
        }

        /// <summary>
        /// Presents the form to enlist a new crew member aboard the ship.
        /// </summary>
        /// <returns>A view containing the crew enlistment form.</returns>
        // GET: Employees/Create
        public IActionResult Create()
        {
            // Hoist the sails — show the recruit form!
            _logger.LogInformation("Showing the crew enlistment form. A new sailor wants to join the ship!");
            return View();
        }

        /// <summary>
        /// Enlists a new crew member by saving their details to the ship's manifest (the database).
        /// </summary>
        /// <param name="employee">The new crew member's details submitted from the enlistment form.</param>
        /// <returns>Redirects to the crew manifest on success, or re-displays the form with errors if the model is invalid.</returns>
        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Email,Department,JobTitle,Salary,DateOfJoining")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Splice the mainbrace! New crew member {FullName} has been added to the manifest.", employee.FullName);
                TempData["SuccessMessage"] = "Employee created successfully.";
                return RedirectToAction(nameof(Index));
            }

            // The recruit's papers are invalid — back to the brig!
            _logger.LogWarning("The enlistment form for {FullName} had invalid data. Sending them back to fill it out properly.", employee.FullName);
            return View(employee);
        }

        /// <summary>
        /// Fetches a crew member's details for editing. Even pirates need paperwork!
        /// </summary>
        /// <param name="id">The unique identifier (sailor ID) of the crew member to edit.</param>
        /// <returns>A view populated with the employee's current data, or a 404 if the sailor is not on the manifest.</returns>
        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Arrr! Edit requested without a sailor ID — returning NotFound.");
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Yo ho ho! Crew member with sailor ID {EmployeeId} not found for editing — they must have swum away.", id);
                return NotFound();
            }

            _logger.LogInformation("Opening the edit logbook for crew member {EmployeeId}: {FullName}.", id, employee.FullName);
            return View(employee);
        }

        /// <summary>
        /// Saves the updated details for an existing crew member back to the ship's manifest.
        /// </summary>
        /// <param name="id">The sailor ID from the route, used to verify it matches the submitted employee record.</param>
        /// <param name="employee">The updated crew member data submitted from the edit form.</param>
        /// <returns>Redirects to the crew manifest on success, or re-displays the form with errors if the model is invalid.</returns>
        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Department,JobTitle,Salary,DateOfJoining")] Employee employee)
        {
            // Mutiny check — make sure the route ID and the form ID match!
            if (id != employee.Id)
            {
                _logger.LogWarning("Mutiny detected! Route ID {RouteId} does not match employee ID {EmployeeId}.", id, employee.Id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("The crew manifest has been updated for sailor {EmployeeId}: {FullName}. All hands rejoice!", id, employee.FullName);
                    TempData["SuccessMessage"] = "Employee updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        // The sailor has vanished like treasure in a storm!
                        _logger.LogWarning("Concurrency clash! Sailor {EmployeeId} no longer exists in the manifest — returning NotFound.", employee.Id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError("A fierce concurrency storm hit while updating sailor {EmployeeId}. Re-throwing the exception!", employee.Id);
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("Edit form for sailor {EmployeeId} had invalid data. Sending back to the crow's nest.", id);
            return View(employee);
        }

        /// <summary>
        /// Displays the confirmation page before keelhauling (deleting) a crew member from the manifest.
        /// </summary>
        /// <param name="id">The unique identifier of the crew member to be removed.</param>
        /// <returns>A view asking for confirmation to delete the employee, or a 404 if not found.</returns>
        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yo ho! Delete requested without a sailor ID — returning NotFound.");
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                _logger.LogWarning("Blimey! Crew member with sailor ID {EmployeeId} not found for keelhauling — they must have fled.", id);
                return NotFound();
            }

            _logger.LogInformation("Showing the keelhaul confirmation page for crew member {EmployeeId}: {FullName}.", id, employee.FullName);
            return View(employee);
        }

        /// <summary>
        /// Confirms and executes the removal of a crew member from the ship's manifest. There be no coming back from this!
        /// </summary>
        /// <param name="id">The unique identifier of the crew member to permanently remove.</param>
        /// <returns>Redirects to the crew manifest after the deed is done.</returns>
        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                // Keelhaul them! Remove the scallywag from the manifest.
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Crew member {EmployeeId}: {FullName} has been keelhauled — removed from the manifest.", id, employee.FullName);
                TempData["SuccessMessage"] = "Employee deleted successfully.";
            }
            else
            {
                _logger.LogWarning("Attempted to keelhaul sailor {EmployeeId} but they were not found in the manifest.", id);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Checks whether a crew member with the given sailor ID exists in the ship's manifest.
        /// </summary>
        /// <param name="id">The unique identifier of the crew member to look up.</param>
        /// <returns><c>true</c> if the sailor is found; <c>false</c> if they have gone overboard.</returns>
        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
