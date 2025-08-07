// *************************************
// Demo classes to be used in Demo Mode.
//
// The application will open the
// executing assembly and look for all
// classes having the [UseInFrontend]
// attribute set on them. Since it only
// checks for the name of the attribute,
// you can easily create your own empty
// attribute class like in this file.
// *************************************

using System;
using System.Collections.Generic;

namespace Zestien3
{
    /// <summary>
    /// Attribute which tells the application to process the class it is set on.
    /// </summary>
    internal class UseInFrontendAttribute : Attribute
    {
        public string? SubFolder { get; set; }
    }

    /// <summary>
    /// Main class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// It represents a company with various departments.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public abstract class Company
    {
        /// <summary>
        /// The name of the Company.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The ID of this company.
        /// </summary>
        public int companyID;

        /// <summary>
        /// The Departments of this Company.
        /// </summary>
        public List<Department>? Departments { get; set; }
    }

    /// <summary>
    /// Another class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// It represents a department with its manager and employees.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class Department
    {
        /// <summary>
        /// The name of this Department.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The Manager of this Department.
        /// </summary>
        public Employee? Manager { get; set; }

        /// <summary>
        /// The Employees of this Department.
        /// </summary>
        public List<Employee>? Employees { get; set; }
    }

    /// <summary>
    /// Last class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// It represents an employee.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class Employee
    {
        /// <summary>
        /// The name of the Employee
        /// </summary>
        public string? Name { get; set; }
    }
}