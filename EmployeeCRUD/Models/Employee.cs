using System.ComponentModel.DataAnnotations;

namespace EmployeeCRUD.Models
{
    /// <summary>
    /// Represents a crew member (employee) aboard the EmployeeCRUD ship.
    /// Each sailor in the manifest has identifying information, their rank (job title),
    /// which deck they work on (department), their plunder share (salary), and the date
    /// they first set sail with the crew.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// The sailor's unique identifier — their number in the crew manifest.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The sailor's given name — what their mother calls them.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The sailor's family name — their pirate clan name.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The sailor's message-in-a-bottle address (email) used to contact them from afar.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The ship's department (deck) on which the sailor serves — e.g. "Engineering" or "Crow's Nest".
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// The sailor's rank aboard the ship — their official job title.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>
        /// The sailor's share of the plunder — their salary in gold doubloons (or local currency).
        /// Must be a positive value; pirates don't work for free!
        /// </summary>
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive value.")]
        public decimal Salary { get; set; }

        /// <summary>
        /// The date the sailor first stepped aboard — when they joined the crew.
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "Date of Joining")]
        public DateTime DateOfJoining { get; set; }

        /// <summary>
        /// The sailor's full name — first and last combined for the captain's roll call.
        /// </summary>
        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
