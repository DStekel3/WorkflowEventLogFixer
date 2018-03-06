using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogFixer
{
  public class XesObject
  {
    private string Event { get; set; }

    private string Trace { get; set; }

    private string Timestamp { get; set; }

    private string LifeCycle { get; set; }
    public XesObject(Event csv)
    {
      Trace = csv.InstanceID;
      Event = $"{csv.TaakOmschrijving}|{csv.ActieOmschrijving}";
      Timestamp = csv.Eind;
      LifeCycle = "complete";
    }
  }
}
