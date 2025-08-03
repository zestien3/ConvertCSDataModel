// *************************************
// Demo classes to be used in Demo Mode.
//
// The application will open the
// executing assembly and look for all
// classes having the [UseInFrontend]
// attribute set on them. Since it only
// checks for the name of the attribute,
// you can easily create your own empty
// atribute class like in this file.
// *************************************

using System;
using System.Collections.Generic;

namespace Zestien3
{
    internal class UseInFrontendAttribute : Attribute { }

    [UseInFrontend()]
    public abstract class Company
    {
        public string? Name { get; set; }
        public List<Department>? Departments { get; set; }
    }

    [UseInFrontend()]
    public class Department
    {
        public string? Name { get; set; }
        public Employee? Manager { get; set; }
        public List<Employee>? Employees { get; set; }
    }

    [UseInFrontend()]
    public class Employee
    {
        public string? Name { get; set; }
    }
}