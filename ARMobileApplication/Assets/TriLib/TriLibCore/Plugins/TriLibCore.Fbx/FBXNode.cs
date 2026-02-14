using System.Collections;
using System.Collections.Generic;
using TriLibCore.Utils;

namespace TriLibCore.Fbx
{
    public class FBXNode : IEnumerable<FBXNode> 
    {
        public long NameHashCode;
        public List<FBXNode> Children = new List<FBXNode>();
        public FBXProperties Properties;
        public string Name;

        public FBXNode(string name)
        {
            NameHashCode = string.IsNullOrEmpty(name) ? 0 : HashUtils.GetHash(name);
        }

        public FBXNode(long nameHashCode)
        {
            NameHashCode = nameHashCode;
        }

        public bool HasSubNodes => Children.Count > 0;

        public bool Valid => NameHashCode != 0;
        public void Add(FBXNode node)
        {
            Children.Add(node);
        }

        public FBXNode GetNodeByIndex(int index)
        {
            var currentIndex = 0;
            foreach (var item in Children)
            {
                if (currentIndex++ == index)
                {
                    return item;
                }
            }
            return null;
        }

        public FBXNode GetNodeByName(long nameHashCode)
        {
            if (Children != null)
            {
                foreach (var node in Children)
                {
                    if (node.NameHashCode == nameHashCode)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        public IEnumerable<FBXNode> GetNodesByName(long nameHashCode)
        {
            if (Children != null)
            {
                foreach (var node in Children)
                {
                    if (node.NameHashCode == nameHashCode)
                    {
                        yield return node;
                    }
                }
            }
        }

        public IEnumerator<FBXNode> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Children).GetEnumerator();
        }
    }
}
