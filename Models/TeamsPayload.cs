using System;
using System.Collections.Generic;

namespace Undone.Resource.Models
{
  public class TeamsPayload : Teams
  {
    public TeamsPayloadLinksDetail links { get; set; }
  }

  public class TeamsPayloadLinksDetail
  {
    public string rel { get; set; }
    public string href { get; set; }
    public string action { get; set; }
    public List<string> types { get; set; }
  }
}