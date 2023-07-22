using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Core
{
    using Core.Geom;
    using Core.Queue;
    using Scripts.Character;

    public partial class Graph
    {
        /// <summary>
        /// The nodes contained within the graph
        ///     node key -> node
        /// </summary>
        private Dictionary<int, Node> Nodes { get; } = new Dictionary<int, Node>();

        /// <summary>
        /// The links contained within the graph
        ///     link key -> link
        /// </summary>
        /// TODO: MAKE PRIVATE! NOT THREADSAFE!
        public Dictionary<int, Graph.Link> Links { get; } = new Dictionary<int, Graph.Link>();

        /// <summary>
        /// The game objects contained within the graph
        ///     link key -> game object it represents
        /// </summary>
        public Dictionary<int, GameObject> LinkObjects { get; } = new Dictionary<int, GameObject>();
        public Dictionary<GameObject, List<Graph.Link>> ObjectLinks { get; } = new Dictionary<GameObject, List<Graph.Link>>();

        /// <summary>
        /// The multi-reader singl-writer lock
        /// </summary>
        private readonly ReaderWriterLock locker = new ReaderWriterLock();

        /// <summary>
        /// Tree read timeout: 10ms
        /// </summary>
        private TimeSpan readerTimeout = TimeSpan.FromMilliseconds(10);
        private int readerTimeoutCount = 0;

        /// <summary>
        /// Tree write timeout: 20ms
        /// </summary>
        private TimeSpan writerTimeout = TimeSpan.FromMilliseconds(20);
        private int writerTimeoutCount = 0;

        /// <summary>
        /// Returns the number of links 
        /// contained in the grpah
        /// </summary>
        public int Count
        {
            get
            {
                int ret = 0;

                try
                {
                    locker.AcquireReaderLock(readerTimeout);
                    try
                    {
                        ret = Links.Count;
                    }
                    finally
                    {
                        locker.ReleaseReaderLock();
                    }
                }
                catch (ApplicationException e)
                {
                    Interlocked.Increment(ref readerTimeoutCount);
                    Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
                }

                return ret;
            }
        }

        public void DrawGizmos()
        {
            try
            {
                locker.AcquireReaderLock(readerTimeout);
                try
                {
                    // draw graph links
                    foreach (var graphLink in Links)
                    {
                        var link = graphLink.Value;
                        if (link.Action.HasFlag(Character.State.Walking))
                            Utils.Debug.DrawLine(link.StartNode.Location, link.EndNode.Location, Color.cyan, 2);
                        if (link.Action.HasFlag(Character.State.Falling))
                            Utils.Debug.DrawLine(link.StartNode.Location, link.EndNode.Location, Color.red, 2);
                        if (link.Action.HasFlag(Character.State.Jumping))
                            Utils.Debug.DrawLine(link.StartNode.Location, link.EndNode.Location, Color.green, 2);
                    }

                    // draw yellow squares around nodes
                    foreach (var nodeinfo in Nodes)
                    {
                        var node = nodeinfo.Value;
                        Rect2 nodeRect = Rect2.Inflated(new Rect2(node.Location.x, node.Location.y, 0, 0), 0.1f, 0.1f);
                        Utils.Debug.DrawRectangle(nodeRect, Color.yellow, 3);
                    }
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref readerTimeoutCount);
                Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
            }
        }

        /// <summary>
        /// Returns true if the graph is empty
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                bool ret = true;

                try
                {
                    locker.AcquireReaderLock(readerTimeout);
                    try
                    {
                        ret = Links.Count == 0
                           && Nodes.Count == 0;
                    }
                    finally
                    {
                        locker.ReleaseReaderLock();
                    }
                }
                catch (ApplicationException e)
                {
                    Interlocked.Increment(ref readerTimeoutCount);
                    Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
                }

                return ret;
            }
        }

        /// <summary>
        /// Adds a new link to the graph.
        /// </summary>
        /// <returns>
        /// True if link was successfully added, 
        /// false if it wasn't added.
        /// </returns>
        public bool Add(Graph.Link link, GameObject obj)
        {
            bool ret = false;

            try
            {
                locker.AcquireWriterLock(writerTimeout);
                try
                {
                    // check if the nodes already exist. 
                    // If they do, make sure the link's
                    // nodes are referencing those nodes.
                    bool addedA = Add(ref link.startNode);
                    bool addedB = Add(ref link.endNode);
                    Debug.Assert(addedA && addedB);

                    if (addedA && addedB)
                    {
                        Nodes[link.StartNode.Key].AddLink(link);
                        Nodes[link.EndNode.Key].AddLink(link);

                        if (!Links.ContainsKey(link.Key))
                        {
                            Links.Add(link.Key, link);
                            if (obj != null)
                            {
                                LinkObjects.Add(link.Key, obj);
                                if (!ObjectLinks.ContainsKey(obj))
                                    ObjectLinks[obj] = new List<Graph.Link>();

                                ObjectLinks[obj].Add(link);
                            }

                            ret = true;
                        }
                    }
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }

            return ret;
        }

        /// <summary>
        /// Adds a new node to the graph.
        /// </summary>
        /// <returns>
        /// Whether or not the node was added.
        /// </returns>
        private bool Add(ref Node node)
        {
            bool ret = false;

            try
            {
                locker.AcquireWriterLock(writerTimeout);
                try
                {

                    if (ContainsNode(node))
                        node = Nodes[node.Key];
                    else
                    {
                        // Add the graph node if it's
                        // not already in the graph
                        Nodes.Add(node.Key, node);
                    }

                    ret = ContainsNode(node);
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }

            return ret;
        }

        public bool Remove(Graph.Link link, bool removeConnected = false, Character.State connectedRemovalState = Character.State.None)
        {
            bool ret = false;

            try
            {
                locker.AcquireWriterLock(writerTimeout);
                try
                {
                    if (ContainsLink(link))
                    {
                        var nodeA = Nodes[link.StartNode.Key];
                        var nodeB = Nodes[link.EndNode.Key];
                        var deletions = new List<Graph.Link>();

                        nodeA.RemoveLink(link);
                        if (removeConnected)
                        {
                            foreach (var adjLinkA in nodeA.Links)
                            {
                                if ((adjLinkA.Action & connectedRemovalState) != Character.State.None)
                                    deletions.Add(adjLinkA);
                            }
                        }

                        if (nodeA.Links.Count == 0)
                            Nodes.Remove(nodeA.Key);

                        nodeB.RemoveLink(link);
                        if (removeConnected)
                        {
                            foreach (var adjLinkB in nodeB.Links)
                            {
                                if ((adjLinkB.Action & connectedRemovalState) != Character.State.None)
                                    deletions.Add(adjLinkB);
                            }
                        }
                        if (nodeB.Links.Count == 0)
                            Nodes.Remove(nodeB.Key);

                        foreach (var deletion in deletions)
                            Remove(deletion, false);

                        Links.Remove(link.Key);
                        if (LinkObjects.ContainsKey(link.Key))
                        {
                            var objKey = LinkObjects[link.Key];
                            if (ObjectLinks.ContainsKey(objKey))
                            {
                                var delIdx = ObjectLinks[objKey].IndexOf(link);
                                ObjectLinks[objKey].RemoveAt(delIdx);
                                if (ObjectLinks[objKey].Count == 0)
                                    ObjectLinks.Remove(objKey);
                            }

                            LinkObjects.Remove(link.Key);
                        }
                    }

                    ret = !ContainsLink(link);
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }

            return ret;

        }

        /// <summary>
        /// Clears the graph of all links and nodes.
        /// </summary>
        public bool Clear()
        {
            bool ret = false;

            try
            {
                locker.AcquireWriterLock(writerTimeout);
                try
                {
                    Nodes.Clear();
                    Links.Clear();
                    LinkObjects.Clear();
                    ObjectLinks.Clear();

                    ret = Links.Count == 0
                       && Nodes.Count == 0;
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }

            return ret;
        }

        private List<Graph.Link> AdjacentLinks(int linkKey, bool respectMovementDirection = true)
        {
            var ret = new List<Graph.Link>();

            try
            {
                locker.AcquireReaderLock(readerTimeout);
                try
                {
                    if (Links.ContainsKey(linkKey))
                    {
                        var link = Links[linkKey];
                        var except = new Graph.Link[] { link };

                        ret.AddRange(link.StartNode.Links.Except(except));
                        ret.AddRange(link.EndNode.Links.Except(except));
                    }
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref readerTimeoutCount);
                Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
            }

            return ret;
        }

        /// <summary>
        /// Returns the graph link that's connected to or representing <paramref name="graphObject"/> 
        /// that's closest to <paramref name="otherObject"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Graph.Link FindObjectLinks(GameObject graphObject, GameObject otherObject)
        {
            Graph.Link ret = null;

            if (graphObject == null)
                return ret;

            try
            {
                locker.AcquireReaderLock(readerTimeout);
                try
                {
                    if (ObjectLinks.ContainsKey(graphObject))
                    {
                        var links = ObjectLinks[graphObject];

                        if (links.Count == 1)
                            ret = links[0];
                        else if (links.Count > 1)
                        {
                            var objRect = Utils.Object.GetBoundingRect(otherObject);
                            if (objRect != Rect2.Empty)
                            {
                                List<Graph.Link> candidates = new List<Graph.Link>();

                                foreach (var link in links)
                                {
                                    if (objRect.OverlapsOnAxis(link.Line, Spatial.Axis.Horizontal))
                                        candidates.Add(link);
                                }

                                if (candidates.Count == 1)
                                    ret = candidates[0];
                                else if (candidates.Count > 1)
                                {
                                    Graph.Link closestLink = null;
                                    float closestDist = float.PositiveInfinity;
                                    var objBottomCenterPt = objRect.GetPoint(Rect2.Location.BottomCenter);
                                    foreach (var candLink in candidates)
                                    {
                                        var dist = candLink.Line.Distance(objBottomCenterPt);
                                        if (dist < closestDist)
                                        {
                                            closestDist = dist;
                                            closestLink = candLink;
                                        }

                                        ret = closestLink;
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref readerTimeoutCount);
                Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
            }

            return ret;
        }

        /// <summary>
        /// Returns any/all links connected to the node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Graph.Link FindClosestLink(Vector2 point)
        {
            Graph.Link ret = null;

            try
            {
                locker.AcquireReaderLock(readerTimeout);
                try
                {
                    Graph.Link closestLink = null;
                    Node closestNode = null;

                    float closestNodeDist = float.PositiveInfinity;
                    foreach (var node in Nodes.Values)
                    {
                        var dist = Vector2.Distance(point, node.Location);
                        if (dist < closestNodeDist)
                        {
                            closestNodeDist = dist;
                            closestNode = node;
                        }
                    }

                    if (closestNode != null)
                    {
                        float closestLinkDist = float.PositiveInfinity;
                        foreach (var link in closestNode.Links)
                        {
                            var linkDist = link.Line.Distance(point);
                            if (linkDist < closestLinkDist)
                            {
                                closestLinkDist = linkDist;
                                closestLink = link;
                            }
                        }

                        if (closestLink != null)
                            ret = closestLink;
                    }
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref readerTimeoutCount);
                Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
            }

            return ret;
        }

        /// <summary>
        /// Returns true if the link exists in the graph. 
        /// If a filter is specified, it will only check 
        /// for the link of that movement action type.
        /// </summary>
        /// <returns>
        /// Whether or not the link exists in the graph.
        /// </returns>
        public bool ContainsLink(Graph.Link link, Character.State actionFilter = Character.State.None)
        {
            bool found = false;

            try
            {
                locker.AcquireReaderLock(readerTimeout);
                try
                {
                    if (ContainsNode(link.StartNode) && ContainsNode(link.EndNode))
                    {
                        var existingANode = Nodes[link.StartNode.Key];
                        foreach (var existingLink in existingANode.Links)
                        {
                            if (link.GeomEquals(existingLink) && link.Action.HasFlag(actionFilter))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            var existingBNode = Nodes[link.EndNode.Key];
                            foreach (var existingLink in existingBNode.Links)
                            {
                                if (link.GeomEquals(existingLink) && link.Action.HasFlag(actionFilter))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref readerTimeoutCount);
                Debug.LogError($"Graph Read Error ({readerTimeoutCount}): {e}");
            }

            return found;
        }

        /// <summary>
        /// Returns true if the node exists in the graph.
        /// </summary>
        /// <returns>
        /// Whether or not the node exists in the graph.
        /// </returns>
        public bool ContainsNode(Node node)
        {
            return Nodes.ContainsKey(node.Key);
        }

        // already locked when called
        public float Heuristic(Graph.Link link, Graph.Link dest)
        {
            return Vector2.Distance(link.Line.Centroid, dest.Line.Centroid);
        }

        // already locked when called
        private float ComputeCost(Graph.Link link, Graph.Link pred)
        {
            float cost = link.Line.Length;

            var startLinkNode = link.StartNode;
            var startPredNode = pred.StartNode;
            var endLinkNode = link.EndNode;
            var endPredNode = pred.EndNode;

            bool traversalFromStartNode = startLinkNode.Equals(startPredNode) || startLinkNode.Equals(endPredNode);
            bool traversalfromEndNode = endLinkNode.Equals(startPredNode) || endLinkNode.Equals(endPredNode);

            // if we're approaching this link from the left side of the predecessor, 
            // but the from/predecessor link doesn't allow movement from left to right
            if (traversalFromStartNode && !link.FlowDir.HasFlag(Graph.Link.FlowDirection.StartToEnd))
                cost += float.PositiveInfinity;

            // if we're approaching this link from the right side of the predecessor, 
            // but the from/predecessor link doesn't allow movement from right to left 
            if (traversalfromEndNode && !link.FlowDir.HasFlag(Graph.Link.FlowDirection.EndToStart))
                cost += float.PositiveInfinity;

            return cost;
        }

        /// <summary>
        /// Resets traversal data for
        /// every link in the graph
        /// </summary>
        private void ResetTraversalData()
        {
            try
            {
                locker.AcquireWriterLock(writerTimeout);
                try
                {
                    foreach (var link in Links.Values)
                        link.ResetTraversalData();
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }
        }

        /// <summary>
        /// Runs the A* algorithm to find the 
        /// shortest path between the two links passed 
        /// in assuming both exist within the graph.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <returns>
        /// The path of links taken from the orig -> dest minpath.
        /// </returns>
        public List<Tuple<Graph.Link, GameObject>> AStar(Graph.Link orig, Graph.Link dest)
        {
            var path = new List<Tuple<Graph.Link, GameObject>>();

            try
            {
                // TODO: keep track of cost data locally so this 
                //       can be converted to a read function 
                locker.AcquireWriterLock(writerTimeout);
                try
                {
                    var openSet = new PriorityQueue<Graph.Link>();
                    var openSetIDs = new HashSet<int>();

                    // Make sure both links exist in the graph.
                    // TODO: handle this in realease builds?
                    Debug.Assert(ContainsLink(orig));
                    Debug.Assert(ContainsLink(dest));

                    // reset all link costs
                    ResetTraversalData();

                    // The set of discovered nodes that may need to be
                    // (re)expanded. Initially, only the orig node is known.
                    openSet.Enqueue(orig);
                    openSetIDs.Add(orig.Key);

                    // For node n, g_score[n] is the cost of the
                    // cheapest path from orig to n currently known
                    orig.Cost = 0.0f;
                    orig.HeuristicCost = Heuristic(orig, dest);

                    while (openSet.Count > 0)
                    {
                        // get the node with the lowest/closest
                        // heuristic cost value from the origin
                        var current = openSet.Dequeue();
                        openSetIDs.Remove(current.Key);

                        if (current.Equals(dest))
                        {
                            // The destination has been reached.
                            // Start the minpath traceback here.
                            GameObject curPathObj = null;
                            if (LinkObjects.ContainsKey(current.Key))
                                curPathObj = LinkObjects[current.Key];

                            if (curPathObj == null)
                                Debug.Assert(false);

                            path.Add(new Tuple<Graph.Link, GameObject>(
                                current, curPathObj
                            ));

                            // Now traverse the path back to the 
                            // origin from this link and return 
                            // the traceback of the entire path
                            while (current.Previous != null)
                            {
                                current = current.Previous;

                                curPathObj = null;
                                if (LinkObjects.ContainsKey(current.Key))
                                    curPathObj = LinkObjects[current.Key];

                                if (curPathObj == null)
                                    Debug.Assert(false);

                                path.Add(new Tuple<Graph.Link, GameObject>(
                                    current, curPathObj
                                ));
                            }

                            // Reverse the traceback so it's ordered 
                            // from origin to destination. This is 
                            // necessary because the path was traversed 
                            // by walking it backwards in the loop above
                            path.Reverse();
                            break;
                        }

                        var adjacentLinks = this.AdjacentLinks(current.Key);
                        foreach (var adj in adjacentLinks)
                        {
                            var adjTraverseCost = ComputeCost(adj, current);
                            var currentPathCost = current.Cost + adjTraverseCost;
                            if (currentPathCost < adj.Cost)
                            {
                                var heuristicCost = Heuristic(adj, dest);

                                adj.Previous = current;
                                adj.Cost = currentPathCost;
                                adj.HeuristicCost = currentPathCost + heuristicCost;

                                if (!openSetIDs.Contains(adj.Key))
                                {
                                    openSet.Enqueue(adj);
                                    openSetIDs.Add(adj.Key);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException e)
            {
                Interlocked.Increment(ref writerTimeoutCount);
                Debug.LogError($"Graph Write Error ({writerTimeoutCount}): {e}");
            }

            return path;
        }
    }
}
