using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowEventLogFixer
{
  internal class ProcessTree
  {
    private readonly Guid _id;
    private readonly Guid _root;
    private readonly List<Node> _nodes = new List<Node>();

    public ProcessTree(string id, string root)
    {
      _id = new Guid(id);
      _root = new Guid(root);
    }

    public void AddNode(Node node)
    {
      _nodes.Add(node);
    }

    public Node GetNode(Guid id)
    {
      return _nodes.Single(n => n.GetId() == id);
    }

    public Guid GetId()
    {
      return _id;
    }

    public Guid GetRoot()
    {
      return _root;
    }

    public void SetParentalRelation(Guid parentId, Guid childId)
    {
      Node parent = GetNode(parentId);
      Node child = GetNode(childId);
      parent.AddChild(childId);
      child.SetParent(parentId);
    }
  }
}