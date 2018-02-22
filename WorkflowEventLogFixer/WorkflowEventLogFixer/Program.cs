using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;

namespace WorkflowEventLogFixer
{
  internal static class Program
  {
    private static readonly Process _process = new Process();

    private const string _baseDirectory = @"C:\Thesis\Profit analyses\22-02-2018";
    private static readonly string _baseCsvfileDirectory = Path.Combine(_baseDirectory, "csv");
    private static readonly string _baseXesFileDirectory = Path.Combine(_baseDirectory, "xes");
    private static readonly string _newXesFileDirectory = $"{_baseXesFileDirectory}";

    static void Main(string[] args)
    {
      var files = Directory.EnumerateFiles(_baseDirectory).ToList();
      //convert each event log to:
      // 1. A csv-file, which is filtered on workflow instances.
      // 2. A xes-file, which is needed for further workflow analysis.

      for(int t = 0; t < files.Count; t++)
      {
        var file = files[t];
        Console.WriteLine($"Busy with {Path.GetFileNameWithoutExtension(file)}...({t + 1}/{files.Count})");
        SplitExcelFiles(file);

        // Variant 1: Using ProM's command line approach for importing csv files and converting into xes-files.
        //var exitCode = CreateXes(csvFile, newXesFile);
        //if(exitCode != 0)
        //{
        //  throw new Exception("Something went wrong when creating the xes file!");
        //}
        //ExtractFile(newXesFile);
      }

      // Variant 2: Using CSVtoXES git project: 
      Console.WriteLine("Creating XES files...");
      ConvertAllCsvFiles(_baseCsvfileDirectory, _newXesFileDirectory);

      Console.WriteLine("Done.");
      Console.Read();
    }

    private static void SplitExcelFiles(string file)
    {
      var events = GetEvents(file);
      var groups = events.GroupBy(e => e.WorkflowID);
      foreach(var group in groups)
      {
        string csvFile = $"{Path.Combine(_baseCsvfileDirectory, Path.GetFileNameWithoutExtension(file))}-{group.Key}.csv";

        var filteredEvents = FilterEvents(group.ToList());
        WriteCsv(filteredEvents, csvFile);
      }
    }

    static List<Event> GetEvents(string excelFile)
    {
      using(ExcelPackage xlPackage = new ExcelPackage(new FileInfo(excelFile)))
      {
        var myWorksheet = xlPackage.Workbook.Worksheets.First();
        var totalRows = myWorksheet.Dimension.End.Row;
        var totalColumns = myWorksheet.Dimension.End.Column;

        var events = new List<Event>();
        for(int rowNum = 2; rowNum <= totalRows; rowNum++)
        {
          var row = myWorksheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value?.ToString() ?? string.Empty).ToList();


          events.Add(new Event
          {
            EventID = row[0],
            Doorlooptijd = row[1],
            WorkflowID = row[2],
            WorkflowOmschrijving = row[3],
            InstanceID = row[4],
            TypeDossierItem = row[5],
            TaakID = row[6],
            TaakOmschrijving = row[7],
            ActieID = row[8],
            ActieType = row[9],
            ActieOmschrijving = row[10],
            ActieBijschrift = row[11],
            Begin = row[12],
            Eind = row[13]
          });
        }

        return events
          .OrderBy(e => e.WorkflowID).ToList()
          .OrderBy(e => e.InstanceID).ToList()
          .OrderBy(e => e.Eind).ToList();
      }
    }

    private static void ConvertAllCsvFiles(string csvfileDirectory, string xesFileDirectory)
    {
      var javaExe = @"C:\Java\jdk1.8.0_91\bin\java.exe";

      var startInfo = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Maximized,
        UseShellExecute = false,
        FileName = @"C:\Users\dst\Source\Repos\CSV-to-XES\CSVtoXES\CsvToXesDirectory.bat",
        Arguments = $"\"{javaExe}\" \"{csvfileDirectory }\" \"{xesFileDirectory}\"",
        WorkingDirectory = @"C:\Users\dst\Source\Repos\CSV-to-XES\CSVtoXES"
      };

      _process.StartInfo = startInfo;
      _process.Start();
      _process.WaitForExit();
    }

    public static List<XesObject> FilterEvents(List<Event> events)
    {
      var activityKeys = new Dictionary<string, string>();
      var totalEventLog = new List<Event>();
      var currentInstance = "-1";
      var dossierItemEvents = new List<Event>();
      events.Reverse();

      var badInstances = new List<string>();

      foreach(Event currentEvent in events)
      {
        if(!badInstances.Contains(currentEvent.InstanceID))
        {
          if(!EventContainsNoise(currentEvent))
          {
            if(currentEvent.InstanceID != currentInstance)
            {
              totalEventLog.AddRange(dossierItemEvents);
              currentInstance = currentEvent.InstanceID;
              dossierItemEvents.Clear();
            }

            if(!activityKeys.ContainsKey($"{currentEvent.TaakID}:{currentEvent.ActieID}"))
            {
              activityKeys.Add($"{currentEvent.TaakID}:{currentEvent.ActieID}", $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}");
            }

            if(activityKeys[$"{currentEvent.TaakID}:{currentEvent.ActieID}"] != $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}")
            {
              Console.WriteLine($"{currentEvent.WorkflowID}");
              dossierItemEvents.Clear();
              break;
            }

            dossierItemEvents.Add(currentEvent);
          }
          else
          {
            badInstances.Add(currentEvent.InstanceID);
          }
        }
      }
      if(badInstances.Any())
      {
        Console.WriteLine($"Removed {badInstances.Count} bad instances.");
      }
      totalEventLog.AddRange(dossierItemEvents);
      totalEventLog.Reverse();

      var filteredLog = new List<XesObject>();
      foreach(Event currentEvent in totalEventLog)
      {
        filteredLog.Add(new XesObject(currentEvent));
      }

      return filteredLog;
    }

    private static bool EventContainsNoise(Event currentEvent)
    {
      if(!int.TryParse(currentEvent.InstanceID, out int a))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.TaakID, out int b))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.ActieID, out int c))
      {
        return true;
      }
      if(currentEvent.TaakOmschrijving == "NULL")
      {
        return true;
      }
      if(currentEvent.ActieOmschrijving == "NULL")
      {
        return true;
      }
      if(currentEvent.Eind == "NULL")
      {
        return true;
      }
      return false;
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
