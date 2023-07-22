using System;
using UnityEngine;

namespace Core
{
    using Core.Geom;
    using Scripts.Character;

    public partial class Graph
    {
        /// <summary>
        /// Represents a link/edge within a connected graph.
        /// </summary>
        public class Link : IEquatable<Link>, IComparable<Link>
        {
            [Flags]
            public enum FlowDirection
            {
                None = 0x00,
                StartToEnd = 0x01,
                EndToStart = 0x02,
                All = StartToEnd | EndToStart
            }

            protected internal Node startNode = null;
            protected internal Node endNode = null;
            public readonly string Name = "";
            public readonly Line2 Line = null;
            public readonly Character.State Action = Character.State.None;
            public readonly FlowDirection FlowDir = FlowDirection.All;
            public readonly float Velocity = 1.0f;
            public readonly int Key = int.MaxValue;
            public float Cost = float.PositiveInfinity;
            public float HeuristicCost = float.PositiveInfinity;
            public Link Previous = null;

            public Link(string description, Character.State action, FlowDirection flow, float velocity, Node start, Node end)
            {
                Name = description;
                Action = action;
                FlowDir = flow;
                Velocity = velocity;
                startNode = start;
                endNode = end;

                Cost = float.PositiveInfinity;
                Line = new Line2(StartNode.Location, EndNode.Location);
                Key = GetHashCode();

                startNode.AddLink(this);
                endNode.AddLink(this);
            }

            public Link(string description, Character.State action, FlowDirection dir, float velocity, Vector2 startPt, Vector2 endPt)
                : this(description, action, dir, velocity,
                       new Node(description, startPt),
                       new Node(description, endPt))
            {
            }

            public Node StartNode
            {
                get => startNode;
            }

            public Node EndNode
            {
                get => endNode;
            }

            public Node LeftNode
            {
                get => (startNode.Location.x < endNode.Location.x)
                        ? startNode : endNode;
            }

            public Node RightNode
            {
                get => (startNode.Location.x > endNode.Location.x)
                        ? startNode : endNode;
            }

            public bool AllowsAction(Character.State action)
            {
                return (this.Action & action) == action;
            }

            public bool AllowsAnyAction(Character.State action)
            {
                return (Action & action) != Character.State.None;
            }
            
            public bool AllowsFlow(FlowDirection flowDir)
            {
                return (this.FlowDir & flowDir) == flowDir;
            }

            public bool AllowsAnyFlow(FlowDirection flowDir)
            {
                return (this.FlowDir & flowDir) != FlowDirection.None;
            }

            public void ResetTraversalData()
            {
                Previous = null;
                Cost = float.PositiveInfinity;
                HeuristicCost = float.PositiveInfinity;
            }

            public override bool Equals(object obj)
            {
                return obj is Link
                    && Equals((Link)obj);
            }

            public bool Equals(Link other)
            {
                if (!Cost.Equals(other.Cost))
                    return false;
                if (!HeuristicCost.Equals(other.HeuristicCost))
                    return false;
                if (!GetHashCode().Equals(other.GetHashCode()))
                    return false;

                return true;
            }

            public bool GeomEquals(Link other)
            {
                return (StartNode.Equals(other.StartNode) && EndNode.Equals(other.EndNode))
                    || (EndNode.Equals(other.StartNode) && StartNode.Equals(other.EndNode));
            }

            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }

            public override string ToString()
            {
                return $"{Name} : {Line.ToString()}";
            }

            public int CompareTo(Link other)
            {
                // This comparison function is used
                // to sort links in the priority queue
                // during shortest path calculation
                if (other == null)
                    return 1;

                // This should cause the cheapest link (according to the A* 
                // heuristic function) to float to the top of the link queue 
                return HeuristicCost.CompareTo(other.HeuristicCost);
            }
        }
    }
}
