using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    using Core.Geom;

    public partial class Graph
    {
        /// <summary>
        /// Represents a node/vertex (link intersection
        /// points) within a connected graph
        /// </summary>
        public class Node : IEquatable<Node>
        {
            public List<Graph.Link> Links = new List<Graph.Link>();
            public Vector2 Location = Vector2.zero;
            public string Name = "";

            public Node(string name, Vector2 location, Graph.Link adjacentLink = null)
            {
                Name = name;
                Location = location;
                if (adjacentLink != null)
                    Links.Add(adjacentLink);
            }

            /// <summary>
            /// Returns a unique identifier for this node,
            /// which is just internally the Location's
            /// coordinate based HashCode value.
            /// </summary>
            public int Key
            {
                // Location is the only thing currently used
                // to uniquely identify a node. This may need
                // to change if different nodes can exist at
                // the same exact coordinates within a graph.
                get => Point2.GetKey(Location);
            }

            public bool AddLink(Graph.Link link)
            {
                if (link != null)
                {
                    int index = Links.FindIndex(l => l.Key == link.Key);
                    if (index == -1)
                    {
                        Links.Add(link);
                        return true;
                    }
                }

                return false;
            }

            public bool RemoveLink(Graph.Link link)
            {
                if (link != null)
                {
                    var index = Links.FindIndex(l => l.Key == link.Key);
                    if (index != -1)
                    {
                        Links.RemoveAt(index);
                        return true;
                    }
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                return obj is Node
                    && Equals((Node)obj);
            }

            public bool Equals(Node other)
            {
                return this.Key == other.Key;
            }

            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{Name} : {Point2.ToString(Location)}";
            }
        }
    }
}
