using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace WorkflowEventLogFixer
{
  class Program
  {
    private static Process _process = new Process();

    static void Main(string[] args)
    {
      var baseDirectory = @"C:\Workflow logs\Profit analyses\08-01-2018\Gesplitst\";
      var baseCsvfileDirectory = Path.Combine(baseDirectory, "csv");
      var baseXesFileDirectory = Path.Combine(baseDirectory, "xes");
      var newCsvFileDirectory = $"{baseCsvfileDirectory}\\filtered2";
      var newXesFileDirectory = $"{baseXesFileDirectory}\\filtered3";
      var files = Directory.EnumerateFiles(baseCsvfileDirectory);
      //convert each event log to:
      // 1. A csv-file, which is filtered on workflow instances.
      // 2. A xes-file, which is needed for further workflow analysis.


      // Variant 1: Using ProM's command line approach for importing csv files and converting into xes-files.
      //foreach(var file in files)
      //{
      //  var eventLog = GetCsvFile(file);
      //  string csvFile = $"{Path.Combine(newCsvFileDirectory, Path.GetFileNameWithoutExtension(file))}.csv";
      //  string newXesFile = $"{Path.Combine(newXesFileDirectory, Path.GetFileNameWithoutExtension(file))}.xes";
      //  WriteCsv(eventLog, csvFile);
      //  var exitCode = CreateXes(csvFile, newXesFile);
      //  if(exitCode != 0)
      //  {
      //    throw new Exception("Something went wrong when creating the xes file!");
      //  }
      //  //ExtractFile(newXesFile);
      //}

      // Variant 2: Using CSVtoXES git project: 
      ConvertAllCsvFiles(newCsvFileDirectory, newXesFileDirectory);

      Console.WriteLine("Done.");
      Console.Read();
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

    private static void ExtractFile(string sourceFile)
    {
      var destination = $"{Path.GetDirectoryName(sourceFile)}\\{Path.GetFileNameWithoutExtension(sourceFile)}-real.xes";
      string zPath = @"C:\Program Files\7-Zip\7zG.exe";
      try
      {
        ProcessStartInfo pro = new ProcessStartInfo
        {
          WindowStyle = ProcessWindowStyle.Maximized,
          FileName = zPath,
          Arguments = "x \"" + sourceFile + "\" -o" + destination
        };
        Process x = Process.Start(pro);
        x.WaitForExit();
      }
      catch(Exception e) { throw e; }
    }

    private static int CreateXes(string csvFile, string newXesFile)
    {
      var startInfo = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Maximized,
        UseShellExecute = false,
        FileName = @"C:\Users\dst\ProM-nightly-20180129-1.7\ProM-nightly-20180129-1.7\CSV.bat",
        Arguments = $"\"{csvFile}\" -start \"Timestamp\" -event \"Event\" -trace \"Trace\" -xes \"{newXesFile}\"",
        WorkingDirectory = @"C:\Users\dst\ProM-nightly-20180129-1.7\ProM-nightly-20180129-1.7"
      };

      _process.StartInfo = startInfo;
      _process.Start();
      _process.WaitForExit();
      return _process.ExitCode;
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

      var filteredLog = new List<XesObject>();
      foreach(CsvObject csvObject in totalEventLog)
      {
        filteredLog.Add(new XesObject(csvObject));
      }

      return filteredLog;
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
