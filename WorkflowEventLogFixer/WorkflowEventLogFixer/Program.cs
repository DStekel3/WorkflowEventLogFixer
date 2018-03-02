using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using OfficeOpenXml;

namespace WorkflowEventLogFixer
{
  internal static class Program
  {
    private static readonly Process _process = new Process();

    private static string _baseDirectory = @"C:\Thesis\Profit analyses\22-02-2018";
    private static readonly string _baseFilteredFileDirectory = Path.Combine(_baseDirectory, "filter");
    private static readonly string _baseCsvFileDirectory = Path.Combine(_baseDirectory, "csv");
    private static readonly string _baseXesFileDirectory = Path.Combine(_baseDirectory, "xes");
    private static readonly string _basePtmlFileDirectory = Path.Combine(_baseDirectory, "ptml");
    private static string _pythonExe = @"C:\Python34\py.exe";
    private static string _word2vecScriptFile = @"C:\Thesis\Word2Vec\word2vec.py";
    private static string _processTreeScriptFile = @"C:\Users\dst\eclipse-workspace\ProM\ProcessTreeMiner.txt";


    //convert each event log to:
    // 1. A csv-file, which is filtered on workflow instances.
    // 2. A xes-file, which is needed for further workflow analysis.


    static void Main(string[] args)
    {
      //if(args.Length > 0)
      //{
      //  _baseDirectory = args[0];
      //  _pythonExe = args[1];
      //  _word2vecScriptFile = args[2];
      //  _processTreeScriptFile = args[3];
      //}

      //CreateDirectoriesIfNeeded();
      //CheckExistanceOfScriptFiles();

      //var files = Directory.EnumerateFiles(_baseDirectory).ToList();

      //for(int t = 0; t < files.Count; t++)
      //{
      //  var file = files[t];
      //  Console.WriteLine($"Busy with {Path.GetFileNameWithoutExtension(file)}...({t + 1}/{files.Count})");
      //  SplitExcelFileIntoSeparateWorkflowLogs(file);
      //}

      //// Apply word2vec throughout the workflow logs and give similar events similar names.
      //ApplyWord2VecThroughGensimScript(_baseCsvFileDirectory);
      //Console.WriteLine("Creating XES files...");
      //ConvertCsvToXesFiles(_baseCsvFileDirectory, _baseXesFileDirectory);

      //UpdatePathsInProcessTreeScript(_processTreeScriptFile, _baseXesFileDirectory, _basePtmlFileDirectory);
      //CreatePtmlFiles(_processTreeScriptFile);

      LoadProcessTrees(_basePtmlFileDirectory);

      Console.WriteLine("Done.");
    }

    private static void LoadProcessTrees(string basePtmlFileDirectory)
    {
      foreach(string file in Directory.EnumerateFiles(basePtmlFileDirectory))
      {
        var loader = new ProcessTreeLoader(file);
      }
    }


    private static void CheckExistanceOfScriptFiles()
    {
      if(!File.Exists(_pythonExe))
      {
        throw new Exception("Python executable not found.");
      }
      if(!File.Exists(_word2vecScriptFile))
      {
        throw new Exception("Word2Vec script not found.");
      }
      if(!File.Exists(_processTreeScriptFile))
      {
        throw new Exception("Process tree script not found.");
      }
    }

    private static void CreateDirectoriesIfNeeded()
    {
      if(!Directory.Exists(_baseDirectory))
      {
        throw new Exception("This directory does not exist.");
      }
      if(!Directory.Exists(_baseFilteredFileDirectory))
      {
        Directory.CreateDirectory(_baseFilteredFileDirectory);
      }
      if(!Directory.Exists(_baseCsvFileDirectory))
      {
        Directory.CreateDirectory(_baseCsvFileDirectory);
      }
      if(!Directory.Exists(_baseXesFileDirectory))
      {
        Directory.CreateDirectory(_baseXesFileDirectory);
      }
      if(!Directory.Exists(_basePtmlFileDirectory))
      {
        Directory.CreateDirectory(_basePtmlFileDirectory);
      }
    }

    /// <summary>
    /// Calls inductive miner script which input xes-directory and outputs ptml files.
    /// </summary>
    /// <param name="processTreeScriptFile"></param>
    private static void CreatePtmlFiles(string processTreeScriptFile)
    {
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.WindowStyle = ProcessWindowStyle.Maximized;
      startInfo.WorkingDirectory = Path.GetDirectoryName(processTreeScriptFile) ?? throw new InvalidOperationException();
      startInfo.FileName = "ProM_CLI.bat";
      startInfo.Arguments = "-f ProcessTreeMiner.txt";
      process.StartInfo = startInfo;
      process.Start();
    }

    private static void SplitExcelFileIntoSeparateWorkflowLogs(string file)
    {
      var events = GetEvents(file);
      var groups = events.GroupBy(e => e.WorkflowID);
      foreach(var group in groups)
      {
        string csvFile = $"{Path.Combine(_baseCsvFileDirectory, Path.GetFileNameWithoutExtension(file) ?? throw new InvalidOperationException())}-{group.Key}.csv";

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

    private static void ConvertCsvToXesFiles(string csvfileDirectory, string xesFileDirectory)
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

    /// <summary>
    /// Re-writes the xes- and ptml-directory paths in the script.
    /// </summary>
    /// <param name="scriptPath"></param>
    /// <param name="xesDirectoryPath">Directory which contains xes files. Used as input for the IM.</param>
    /// <param name="ptmlDirectoryPath">Directory where resulting ptml files are saved. Used as output for the IM.</param>
    private static void UpdatePathsInProcessTreeScript(string scriptPath, string xesDirectoryPath, string ptmlDirectoryPath)
    {
      // new path lines in script
      string xesLineToWrite = $"xesDirectoryPath = \"{xesDirectoryPath}\\\";".Replace("\\", "\\\\");
      string ptmlLineToWrite = $"ptmlDirectoryPath = \"{ptmlDirectoryPath}\\\";".Replace("\\", "\\\\");

      if(File.Exists(scriptPath))
      {
        string[] lines = File.ReadAllLines(scriptPath);

        if(lines.Length > 0)
        {
          // Write the new file over the old file.
          using(StreamWriter writer = new StreamWriter(scriptPath))
          {
            for(int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
              if(currentLine == 2)
              {
                writer.WriteLine(xesLineToWrite);
              }
              else if(currentLine == 3)
              {
                writer.WriteLine(ptmlLineToWrite);
              }
              else
              {
                writer.WriteLine(lines[currentLine]);
              }
            }
          }
        }
        else
        {
          throw new Exception("Script is empty.");
        }
      }
    }

    private static void ApplyWord2VecThroughGensimScript(string csvDirectory)
    {
      ProcessStartInfo start = new ProcessStartInfo();
      start.FileName = _pythonExe;//cmd is full path to python.exe
      start.Arguments =$"-f {_word2vecScriptFile} '{csvDirectory}'";//args is path to .py file and any cmd line args
      start.UseShellExecute = false;
      start.RedirectStandardOutput = true;
      using(Process process = Process.Start(start))
      {
        using(StreamReader reader = process?.StandardOutput)
        {
          string result = reader?.ReadToEnd();
          Console.Write(result);
        }
      }
    }
  }
}