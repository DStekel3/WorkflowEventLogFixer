using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogFixer
{
    static class SubTreeFinder
    {
        public static bool IsValidSubTree(ProcessTree tree, ProcessTree pattern, bool induced)
        {
            var tNode = tree.GetRoot();
            var pNode = pattern.GetRoot();
            return DoesBranchContainPattern(tNode, pNode, induced);
        }

        private static bool DoesBranchContainPattern(Node tNode, Node pNode, bool induced)
        {
            // if current node in tree = node in pattern
            if(tNode.GetEvent() == pNode.GetEvent())
            {
                var pChildren = pNode.GetChildren();
                var tChildren = tNode.GetChildren();

                if(!ContainsSiblings(tNode, pNode))
                {
                    return false;
                }

                if(pChildren.Any())
                {
                    foreach(var patternChild in pChildren)
                    {
                        foreach(Node treeChild in tChildren)
                        {
                            var patternChildFound = DoesBranchContainPattern(treeChild, patternChild, induced);
                            if(patternChildFound)
                            {
                                return true;
                            }
                        }
                        break;
                    }
                }
                else
                {
                    return true;
                }
            }

            // otherwise, search further into the tree
            else if(induced && !pNode.IsRoot())
            {
                return false;
            }
            else
            {
                foreach(Node treeChild in tNode.GetChildren())
                {
                    return DoesBranchContainPattern(treeChild, pNode, induced);
                }
            }
            return false;
        }

        private static bool ContainsSiblings(Node tNode, Node pNode)
        {
            if(tNode.GetSiblings().Intersect(pNode.GetSiblings()).ToList().Count == pNode.GetSiblings().Count)
            {
                return true;
            }
            return false;
        }
    }
}