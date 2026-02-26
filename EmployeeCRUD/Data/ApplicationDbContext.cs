using Microsoft.EntityFrameworkCore;
using EmployeeCRUD.Models;

namespace EmployeeCRUD.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial data
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    Department = "Engineering",
                    JobTitle = "Software Engineer",
                    Salary = 75000,
                    DateOfJoining = new DateTime(2022, 1, 15)
                },
                new Employee
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    Department = "Human Resources",
                    JobTitle = "HR Manager",
                    Salary = 65000,
                    DateOfJoining = new DateTime(2021, 6, 1)
                },
                new Employee
                {
                    Id = 3,
                    FirstName = "Bob",
                    LastName = "Johnson",
                    Email = "bob.johnson@example.com",
                    Department = "Finance",
                    JobTitle = "Financial Analyst",
                    Salary = 70000,
                    DateOfJoining = new DateTime(2023, 3, 20)
                }
            );
        }
    }
}
