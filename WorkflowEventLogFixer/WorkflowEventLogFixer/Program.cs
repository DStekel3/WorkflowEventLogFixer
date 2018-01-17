using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Excel;
using Excel = Microsoft.Office.Interop.Excel;

namespace WorkflowEventLogFixer
{
  class Program
  {
    static void Main(string[] args)
    {
      var fileDirectory = @"C:\Workflow logs\Profit analyses\08-01-2018\Gesplitst\csv";
      var files = Directory.EnumerateFiles(fileDirectory);
      foreach(var file in files)
      {
        var eventLog = GetCsvFile(file);
        WriteCsv(eventLog, $"{fileDirectory}\\filtered3\\{Path.GetFileNameWithoutExtension(file)}.csv");
      }
      Console.WriteLine("Done.");
      Console.Read();
    }

    private static List<XesObject> GetCsvFile(string filePath)
    {
      var contents = File.ReadAllLines(filePath).Select(a => a.Split(','));
      var csv = from line in contents.Skip(1)
                select (from piece in line
                        select piece).ToList();

      var activityKeys = new Dictionary<string, string>();
      var totalEventLog = new List<CsvObject>();
      var currentDossierItem = "-1";
      var dossierItemEvents = new List<CsvObject>();
      foreach(var row in csv.Reverse())
      {
        var currentEvent = new CsvObject
        {
          Workflow = row[0],
          WorkflowOmschrijving = row[1],
          DossierItem = row[2],
          TypeDossierItem = row[3],
          Taak = row[4],
          TaakOmschrijving = row[5],
          Volgnummer = row[6],
          VolgnummerVia = row[7],
          ActieType = row[8],
          ActieOmschrijving = row[9],
          ActietypeOmschrijving = row[10],
          Begin = row[11],
          Eind = row[12],
          StandaardBijschrift = row[13],
          Status = row[14],
          DoorPersoon = row[15],
          WorkflowGegevensStatus = row[16]
        };
        if(currentEvent.DossierItem != currentDossierItem)
        {
          totalEventLog.AddRange(dossierItemEvents);
          currentDossierItem = currentEvent.DossierItem;
          dossierItemEvents.Clear();
        }

        if(!activityKeys.ContainsKey($"{currentEvent.Taak}:{currentEvent.Volgnummer}"))
        {
          activityKeys.Add($"{currentEvent.Taak}:{currentEvent.Volgnummer}", $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}");
        }

        if(activityKeys[$"{currentEvent.Taak}:{currentEvent.Volgnummer}"] != $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}")
        {
          Console.WriteLine($"{currentEvent.Workflow}");
          dossierItemEvents.Clear();
          break;
        }

        dossierItemEvents.Add(currentEvent);
      }
      totalEventLog.AddRange(dossierItemEvents);
      totalEventLog.Reverse();

      var xesLog = new List<XesObject>();
      foreach(CsvObject csvObject in totalEventLog)
      {
        xesLog.Add(new XesObject(csvObject));
      }

      return xesLog;
    }

    private static void WriteCsv<T>(IEnumerable<T> items, string path)
    {
      var itemType = typeof(T);
      var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .OrderBy(p => p.Name);

      using(var writer = new StreamWriter(path))
      {
        writer.WriteLine(string.Join(";", props.Select(p => p.Name)));

        foreach(var item in items)
        {
          writer.WriteLine(string.Join(";", props.Select(p => p.GetValue(item, null))));
        }
      }
    }
  }
}
