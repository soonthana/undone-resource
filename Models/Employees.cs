using System;

namespace Undone.Resource.Models
{
  public class Employees
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string PhoneExtension { get; set; }
    public string JobCode { get; set; }
    public string JobName { get; set; }
    public string TeamCode { get; set; }
    public string TeamName { get; set; }
    public string DepartmentCode { get; set; }
    public string DepartmentName { get; set; }
    public bool Status { get; set; }
  }
}