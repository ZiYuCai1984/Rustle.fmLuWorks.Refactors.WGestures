using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 67
#pragma warning disable 8618
// ReSharper disable EventNeverSubscribedTo.Global


namespace NativeMultiFileArchiveLib
{
    /// <summary>
    ///     a hierarchical collection of nodes.
    ///     the generic parameter allows a specified object type to be stored as the value in each node.
    ///     various methods provide enumerations of the node's hierarchy (these are created by searching
    ///     the
    ///     entire hierarchy, not just particular leaves)
    ///     eg:
    ///     GetTopLevel()
    ///     GetChildren()
    ///     GetSiblings()
    ///     GetDescendants()
    ///     Implements IList so that the Tree can be used like a list.
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    [Serializable]
    public class Tree<TV> : IList<TreeNode<TV>>
    {
        #region Fields

        private List<TreeNode<TV>> _nodes = new List<TreeNode<TV>>();

        #endregion

        #region Properties

        /// <summary>
        ///     access this tree-node as an IList.
        /// </summary>
        public IList<TreeNode<TV>> NodesList => this;

        #endregion

        #region IEnumerable<TreeNode<V>> Members

        public IEnumerator<TreeNode<TV>> GetEnumerator()
        {
            foreach (var node in _nodes)
            {
                yield return node;
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var node in _nodes)
            {
                yield return node;
            }
        }

        #endregion

        /// <summary>
        ///     print the tree's values
        ///     eg:
        ///     TopLevel
        ///     TopLevel\Level2_1
        ///     TopLevel\Level2_2
        ///     TopLevel\Level2_2\Level2_3\Sub\Value\Test
        ///     TopLevel\Level2_3\Level3_3
        /// </summary>
        /// <param name="writeToConsole"></param>
        /// <returns></returns>
        public string PrintTree(bool writeToConsole)
        {
            var sb = new StringBuilder();
            foreach (var node in this.GetAllNodesInHierarchyOrder())
            {
                sb.AppendLine(node.Path);
                if (writeToConsole)
                {
                    Console.WriteLine(node.Path);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        ///     string description of the item: (not a value)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{TreeNode<" + typeof(TV).Name + "> Count: " + this.Count + "}";
        }

        #region Events

        public delegate void NodeEvent(object sender, TreeNode<TV> node);

        public event NodeEvent NodeAdded;
        public event NodeEvent NodeRemoved;

        #endregion

        #region Hierarchy Enumerators

        /// <summary>
        ///     gets every node in the list, in hierarchy order (parents first)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TreeNode<TV>> GetAllNodesInHierarchyOrder()
        {
            foreach (var node in this.GetTopLevel())
            {
                yield return node;
                foreach (var descNode in this.GetDescendents(node))
                {
                    yield return descNode;
                }
            }
        }

        /// <summary>
        ///     get the top-level nodes (those without parents)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TreeNode<TV>> GetTopLevel()
        {
            return from n in _nodes
                where n.Parent == null
                orderby n.Index
                select n;
        }

        /// <summary>
        ///     actually calculates the children of a specified node, by searching the entire
        ///     tree (not just the children stored at the node)
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IEnumerable<TreeNode<TV>> GetChildren(TreeNode<TV> parent)
        {
            return from n in _nodes
                where n.Parent == parent
                orderby n.Index
                select n;
        }

        /// <summary>
        ///     calculate the siblings of the current node by searching the entire tree.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<TreeNode<TV>> GetSiblings(TreeNode<TV> node)
        {
            return from n in _nodes
                where n.Parent == node.Parent && n != node
                orderby n.Index
                select n;
        }

        /// <summary>
        ///     calculate all the descendents of the current node in hierarchy (top-down) order.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<TreeNode<TV>> GetDescendents(TreeNode<TV> node)
        {
            // iterate the node's children:
            foreach (var child in this.GetChildren(node))
            {
                // return the node;
                yield return child;

                // recurse:
                foreach (var desc in this.GetDescendents(child))
                {
                    yield return desc;
                }
            }
        }

        #endregion

        #region TreeNode.Add

        /// <summary>
        ///     add a tree-node with the specified parent.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public TreeNode<TV> Add(TV value, TreeNode<TV> parent)
        {
            // create the node;
            var node = new TreeNode<TV>
            {
                Value = value,
                Parent = parent
            };

            // set it's parent and value.

            // add into the collection:
            this.Add(node);

            // return it.
            return node;
        }

        /// <summary>
        ///     add a tree-node with the specified parent.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parentIndex"></param>
        /// <returns></returns>
        public TreeNode<TV> Add(TV value, int parentIndex)
        {
            if (parentIndex >= this.Count)
            {
                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("Parent Index is Invalid");
            }

            // create the node:
            var node = new TreeNode<TV>
            {
                Parent = _nodes[parentIndex],
                Value = value
            };

            // set it's parent:

            // set it's value:

            // add to the collection:
            this.Add(node);

            // return the new node:
            return node;
        }

        /// <summary>
        ///     add a new top-level node.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TreeNode<TV> Add(TV value)
        {
            // create the node:
            var node = new TreeNode<TV>
            {
                Value = value
            };

            // set it's value:

            // add to the collection:
            this.Add(node);

            // return the new node:
            return node;
        }

        #endregion

        #region IList<TreeNode<V>> Members

        public int IndexOf(TreeNode<TV> item)
        {
            return _nodes.IndexOf(item);
        }

        public void Insert(int index, TreeNode<TV> item)
        {
            _nodes.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _nodes.RemoveAt(index);
        }

        public TreeNode<TV> this[int index]
        {
            get => _nodes[index];
            set => _nodes[index] = value;
        }

        public TreeNode<TV>? this[TV value]
        {
            get
            {
                var qry = from n in _nodes where n.Value.Equals(value) select n;
                var treeNodes = qry as TreeNode<TV>[] ?? qry.ToArray();
                return treeNodes.Any() ? treeNodes.First() : null;
            }
        }

        #endregion

        #region ICollection<TreeNode<V>> Members

        /// <summary>
        ///     the main add method for the Tree-Node.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TreeNode<TV> item)
        {
            if (!_nodes.Contains(item))
            {
                if (item.Parent != null)
                {
                    if (_nodes.Contains(item.Parent))
                    {
                        // add the node to the collection
                        _nodes.Add(item);

                        // set the owner:
                        item.SetOwner(this);

                        // raise node added.
                        this.NodeAdded?.Invoke(this, item);
                    }
                    else
                    {
                        throw new ApplicationException("Parent Node not in Collection!");
                    }
                }
                else
                {
                    // add the node to the collection
                    _nodes.Add(item);

                    // set the owner:
                    item.SetOwner(this);

                    // raise node added.
                    this.NodeAdded?.Invoke(this, item);
                }
            }
            else
            {
                if (_nodes.Contains(item.Parent))
                {
                    // find it's index location:
                    var index = _nodes.IndexOf(item);

                    // update the value to be sure:
                    _nodes[index] = item;

                    // set the owner:
                    item.SetOwner(this);

                    // raise node-added.
                    this.NodeAdded?.Invoke(this, item);
                }
                else
                {
                    throw new ApplicationException("Parent Node not in Collection!");
                }
            }
        }

        public void Clear()
        {
            _nodes.Clear();
        }

        public bool Contains(TreeNode<TV> item)
        {
            return _nodes.Contains(item);
        }

        public bool Contains(TV item)
        {
            // search the nodes for a matching v.
            var qry = from n in _nodes
                where n.Value.Equals(item)
                select n;

            // return true if the results are > 0
            return qry.Any();
        }

        public void CopyTo(TreeNode<TV>[] array, int arrayIndex)
        {
            for (var i = arrayIndex; i < _nodes.Count; i++)
            {
                array[i] = _nodes[i - arrayIndex];
            }
        }

        public int Count => _nodes.Count;

        public bool IsReadOnly => false;

        public bool Remove(TreeNode<TV> item)
        {
            return _nodes.Remove(item);
        }

        #endregion
    }
}
