using System;
using System.Collections.Generic;

namespace WorkflowEventLogFixer
{
  internal class Node
  {
    private ProcessTreeLoader.NodeType _type;
    private Guid _id;
    private Guid _parent;
    private List<Guid> _children = new List<Guid>();

    public Node(ProcessTreeLoader.NodeType type, string id)
    {
      _type = type;
      _id = new Guid(id);
    }

    public Guid GetId()
    {
      return _id;
    }

    public new ProcessTreeLoader.NodeType GetType()
    {
      return _type;
    }

    public void SetParent(Guid parent)
    {
      _parent = parent;
    }

    public void AddChild(Guid child)
    {
      _children.Add(child);
    }
  }
}