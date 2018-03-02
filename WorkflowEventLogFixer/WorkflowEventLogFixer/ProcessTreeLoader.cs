using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WorkflowEventLogFixer
{
  class ProcessTreeLoader
  {
    private readonly string _sourceFile;

    public enum NodeType
    {
      processTree,
      xor,
      xorLoop,
      and,
      sequence,
      manualTask,
      automaticTask,
      parentsNode,
      Other
    };

    public ProcessTreeLoader(string filePath)
    {
      _sourceFile = filePath;
      ProcessTree tree = null;
      XmlReaderSettings settings = new XmlReaderSettings();
      settings.DtdProcessing = DtdProcessing.Parse;
      XmlReader reader = XmlReader.Create(_sourceFile, settings);
      reader.MoveToContent();
      while(reader.Read())
      {
        if(reader.IsStartElement())
        {
          switch(reader.Name)
          {
            case "processTree":
              {
                tree = new ProcessTree(reader["id"], reader["root"]);
                break;
              }
            case "xor":
              {
                tree.AddNode(new Node(NodeType.xor, reader["id"]));
                break;
              }
            case "and":
              {
                tree.AddNode(new Node(NodeType.and, reader["id"]));
                break;
              }
            case "sequence":
              {
                tree.AddNode(new Node(NodeType.sequence, reader["id"]));
                break;
              }
            case "manualTask":
              {
                tree.AddNode(new Node(NodeType.manualTask, reader["id"]));
                break;
              }
            case "automaticTask":
              {
                tree.AddNode(new Node(NodeType.automaticTask, reader["id"]));
                break;
              }
            case "parentsNode":
              {
                Guid parentId = new Guid(reader["sourceId"] ?? throw new InvalidOperationException());
                Guid childId = new Guid(reader["targetId"] ?? throw new InvalidOperationException());
                tree.SetParentalRelation(parentId, childId);
                break;
              }
          }
        }
      }
    }
  }
}
