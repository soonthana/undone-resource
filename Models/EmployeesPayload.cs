using System;
using System.Collections.Generic;

namespace Undone.Resource.Models
{
  public class EmployeesPayload : Employees
  {
    public EmployeesPayloadLinksDetail links { get; set; }
  }

  public class EmployeesPayloadLinksDetail
  {
    public string rel { get; set; }
    public string href { get; set; }
    public string action { get; set; }
    public List<string> types { get; set; }
  }
}