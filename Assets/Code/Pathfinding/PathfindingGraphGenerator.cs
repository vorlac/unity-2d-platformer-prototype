using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Pathfinding
{
    using UnityEngine;

    using Core;
    using Core.Geom;
    using Core.Spatial;
    using Core.Tree;
    using Scripts.Character;

    /// <summary>
    /// This class will manage everything necessary to 
    /// generate a shortest path between two characters 
    /// dynamically.
    /// </summary>
    public class PathfindingGraphGenerator : MonoBehaviour
    {
        /// <summary>
        /// The source / start object (follower AI) 
        /// TODO: remove the NPC Foller dependency
        ///       needed for jump/fall simulation
        /// </summary>
        public NPCFollower source;

        /// <summary>
        /// The object (player) to be followed
        /// </summary>
        public Character target;

        /// <summary>
        /// The traversable layers
        /// </summary>
        public LayerMask layers;

        /// <summary>
        /// The width multiplier that determines when
        /// to split each walkable platform/object.
        /// The value is a multiple of th source
        /// object width since it's doing the traversal.
        /// </summary>
        [Range(1f, 10f)]
        public float segmentWidthMultiplier = 5f;

        /// <summary>
        /// Object tags to filter objects from 
        /// the layerMask defined above
        /// </summary>
        [SerializeField]
        [TagSelector]
        public string[] TagFilterArray = new string[] { };

        /// <summary>
        /// The traversal graph rebuild interval 
        /// </summary>
        public float graphUpdateInterval = 0.25f;

        /// <summary>
        /// The traversal graph rebuild timer
        /// </summary>
        public float graphUpdateTimer = 0.00f;

        /// <summary>
        /// If true the graph will be fully
        /// rebuilt from scratch rather than
        /// partially updated only where necessary
        /// </summary>
        public bool fullGraphRebuild = false;

        /// <summary>
        /// The pathfinding graph that connects all 
        /// traversable paths between platforms for
        /// the npc character movement (run, jump...)
        /// </summary>
        private readonly Graph graph = new Graph();

        /// <summary>
        /// Spatial index of all game objects in the graph
        /// </summary>
        private readonly RTree<GameObject, Line2> rtree = new RTree<GameObject, Line2>();

        /// <summary>
        /// A collection of all scene game objects that were fetched during the 
        /// previous update that maps each game object to the links in the graph
        /// </summary>
        private readonly Dictionary<GameObject, (Rect2 rect, List<Graph.Link> links)> objGraphMap = 
                     new Dictionary<GameObject, (Rect2 rect, List<Graph.Link> links)>();

        /// <summary>
        /// The current path traceback
        /// </summary>
        private readonly List<Tuple<Graph.Link, GameObject>> traceback = new List<Tuple<Graph.Link, GameObject>>();

        /// <summary>
        /// The target object rectangle
        /// </summary>
        private Rect2 targetObjectRect = Rect2.Empty;

        /// <summary>
        /// The source object rectangle
        /// </summary>
        private Rect2 sourceObjectRect = Rect2.Empty;

        /// <summary>
        /// The rectangle representing the 
        /// source object's jumpable bounds
        /// </summary>
        private Rect2 sourceJumpAreaRect = Rect2.Empty;

        /// <summary>
        /// The rectangle representing the 
        /// source object's fallable bounds
        /// </summary>
        private Rect2 sourceFallAreaRect = Rect2.Empty;
        
        #region Debug Fields
        /// <summary>
        /// Debug settings
        /// </summary>
        [SerializeField]
        private int updateCount = 0;
        [SerializeField]
        private bool enableTiming = false;

        private readonly Stopwatch graphUpdateSW = Stopwatch.StartNew();
        private readonly Stopwatch spatialIndexSW = Stopwatch.StartNew();
        private readonly Stopwatch minPathSW = Stopwatch.StartNew();
        #endregion

        private void Start()
        {
            targetObjectRect = Utils.Object.GetBoundingRect(target.gameObject);
            sourceObjectRect = Utils.Object.GetBoundingRect(source.gameObject);
            sourceJumpAreaRect = source.GetJumpArcBoundingRect(Vector2.right);
            sourceFallAreaRect = source.GetFallArcBoundingRect(Vector2.right);
        }

        private void FixedUpdate()
        {
            graphUpdateTimer += Time.fixedDeltaTime;

            if (targetObjectRect != null && source != null)
            {
                targetObjectRect = Utils.Object.GetBoundingRect(target.gameObject);
                sourceObjectRect = Utils.Object.GetBoundingRect(source.gameObject);

                if (graphUpdateTimer >= graphUpdateInterval)
                {
                    ++updateCount;

                    if (UpdateTraversalGraph())
                        GetPathTraceback();

                    graphUpdateTimer = 0;
                }

                FollowPath(source);
            }
        }

        /// <summary>
        /// Updates the traversal graph
        /// </summary>
        /// <returns></returns>
        private bool UpdateTraversalGraph()
        {
            bool ret = false;

            DebugTiming(graphUpdateSW);

            var objects = Utils.World.GetSceneObjects(layers);
            if (graph.IsEmpty || fullGraphRebuild)
                ret = RebuildTraversalGraph(objects);
            else
                ret = RefreshTraversalGraph(objects);

            DebugTiming(graphUpdateSW, $"Graph Update [{updateCount}]: {graphUpdateSW.Elapsed}");

            return ret;
        }

        /// <summary>
        /// Generates the shortest path 
        /// from the NPC to the target
        /// </summary>
        private bool GetPathTraceback()
        {
            DebugTiming(minPathSW);

            if (graph.Count > 0)
            {
                var sourcePlatform = source.Controller.collisions.belowObject;
                var targetPlatform = target.Controller.collisions.belowObject;
                if (sourcePlatform != null && targetPlatform != null)
                {
                    var sourceLink = graph.FindObjectLinks(sourcePlatform, source.gameObject);
                    var targetLink = graph.FindObjectLinks(targetPlatform, target.gameObject);
                    if (targetLink != null && sourceLink != null)
                    {
                        traceback.Clear();
                        var shortestPath = graph.AStar(sourceLink, targetLink);
                        foreach (var pathItem in shortestPath)
                            traceback.Add(pathItem);
                    }
                }
            }

            DebugTiming(minPathSW, $"Minpath Calculation [{updateCount}]: {minPathSW.Elapsed}");

            return traceback.Count > 0;
        }

        /// <summary>
        /// Controls the source object towards the target 
        /// object by following the traceback path
        /// </summary>
        /// <param name="follower"></param>
        private void FollowPath(NPCFollower follower)
        {
            if (follower == null)
                return;

            // only apply movements to characters touching the ground
            if (follower.Controller.collisions.below && traceback.Count > 0)
            {
                Vector2 xInput = Vector2.zero;
                var standingObj = follower.Controller.collisions.belowObject;
                for (int i = 0; i < traceback.Count; ++i)
                {
                    var item = traceback[i];
                    var link = item.Item1;
                    var linkObj = item.Item2;

                    // if this is the link the followe is standing on
                    if (linkObj.Equals(standingObj))
                    {
                        Utils.Debug.DrawDebugLine(item.Item1.Line, Color.blue);

                        if (i + 1 < traceback.Count)
                        {
                            var nextItem = traceback[i + 1];
                            var nextLink = nextItem.Item1;
                            var nextLinkObj = nextItem.Item2;

                            // figure out which node is closest to the current link 
                            var leftNodeDist = nextLink.Line.Distance(link.LeftNode.Location);
                            var rightNodeDist = nextLink.Line.Distance(link.RightNode.Location);

                            // if the next link is walkable / standable / runnable
                            //if ((nextLink.Action & (Character.State.Walking | Character.State.Running | Character.State.Standing)) != Character.State.None)
                            {
                                // walk towards the next link
                                if (leftNodeDist < rightNodeDist)
                                    xInput = Vector2.left;
                                else
                                    xInput = Vector2.right;
                            }
                        }
                    }
                }

                follower.SetDirectionalInput(xInput);
            }
        }

        /// <summary>
        /// Clear and fully rebuild the r-rtree spatial index
        /// used to quickly find scene objects by location
        /// </summary>
        /// <returns></returns>
        private bool RebuildSpatialIndex()
        {
            DebugTiming(spatialIndexSW);

            // clear and rebuild the rtree
            rtree.Clear();

            if (sourceObjectRect != Rect2.Empty)
            {
                // TODO: implement RTree update code as well.
                var sceneObjects = Utils.World.GetSceneObjects(layers);
                foreach (var obj in sceneObjects)
                {
                    Line2 topFace = Utils.Object.GetTopFace(obj);
                    if (topFace != null)
                    {
                        var segments = topFace.Split(sourceObjectRect.Width * segmentWidthMultiplier);
                        for (int n = 0; n < segments.Count; ++n)
                        {
                            var seg = segments[n];
                            Rect2 segRect = new Rect2(seg.Start, seg.End);
                            rtree.Insert(obj, segRect.Inflate(0.01f, 0.01f), seg);
                        }
                    }
                }
            }

            DebugTiming(spatialIndexSW, $"RTree Rebuild [{updateCount}]: {spatialIndexSW.Elapsed}");

            return rtree.Count > 0;
        }

        private bool InsertPathfindingGraphObject(GameObject obj)
        {
            HashSet<Graph.Link> added = null;
            return InsertPathfindingGraphObject(obj, ref added);
        }

        private bool InsertPathfindingGraphObject(GameObject obj, ref HashSet<Graph.Link> added)
        {
            Rect2 objRect = Utils.Object.GetBoundingRect(obj);
            Line2 topFace = Utils.Object.GetTopFace(obj);
            List<Graph.Link> objLinks = new List<Graph.Link>();

            if (topFace != null)
            {
                added = new HashSet<Graph.Link>();
                objLinks = SplitObjectIntoSegmentLinks(obj, topFace);
                foreach (var objSegmentLink in objLinks)
                {
                    graph.Add(objSegmentLink, obj);
                    if (added != null)
                       added.Add(objSegmentLink);
                }
            }

            objGraphMap[obj] = (objRect, objLinks);
            return objLinks.Count > 0;
        }

        private bool RemovePathfindingGraphObject(GameObject obj)
        {
            bool success = false;
            if (objGraphMap.ContainsKey(obj))
            {
                var old = objGraphMap[obj];
                success = (old.rect.IsEmpty && old.links.Count == 0) 
                      || (!old.rect.IsEmpty && old.links.Count > 0);

                Debug.Assert(success);
                foreach (var oldLink in old.links)
                {
                    // delete this link and any adjacent links that 
                    // contain actions other than basic traversal states
                    Character.State adjRemovalLinkActions = ~Character.State.Traversing;
                    success &= graph.Remove(oldLink, true, adjRemovalLinkActions);
                    Debug.Assert(success);
                }

                // remove the object -> link mapping data for this
                // object now that's its been deleted from the graph
                success &= objGraphMap.Remove(obj);
                Debug.Assert(success);
            }

            return success;
        }

        private bool RefreshTraversalGraph(HashSet<GameObject> objects)
        {
            bool ret = false;

            if (RebuildSpatialIndex())
            {
                // keep track of the graph links that will be added or udpated
                // so we know which link connections needs to be refreshed
                HashSet<Graph.Link> refresh = new HashSet<Graph.Link>();

                var insert = new HashSet<GameObject>();
                var modify = new HashSet<GameObject>();
                var delete = new HashSet<GameObject>();

                // Figure out which scene objects in the current
                // graph no longer exist in the current scene sp
                // they can be removed from the graph.
                foreach (var graphObj in objGraphMap)
                {
                    if (!objects.Contains(graphObj.Key))
                        delete.Add(graphObj.Key);
                }

                // Figure out which scene objects need to be 
                // updated in the graph (updates & deletes)
                foreach (var obj in objects)
                {
                    if (!objGraphMap.ContainsKey(obj))
                        insert.Add(obj);
                    else
                    {
                        // it's possible that graphObjectRects contains 
                        // fewer objects in it than graphObjectSet since
                        // the rect dictionary only contains graph objects
                        // but the object set contains all layer objects
                        if (!objGraphMap.ContainsKey(obj))
                            insert.Add(obj);
                        else
                        {
                            var objectRect = Utils.Object.GetBoundingRect(obj);
                            var graphObjectRect = objGraphMap[obj].rect;
                            if (objectRect != graphObjectRect)
                                modify.Add(obj);
                        }
                    }
                }

                ret = true;

                var deletions = delete.Union(modify);
                foreach (var obj in deletions)
                {
                    ret &= RemovePathfindingGraphObject(obj);
                    Debug.Assert(ret);
                }

                var insertions = insert.Union(modify);
                foreach (var obj in insertions)
                {
                    ret &= InsertPathfindingGraphObject(obj, ref refresh);
                    Debug.Assert(ret);
                }

                // add any other links that are close enough to any 
                // of the links that have just been added to the graph
                // so they can be reevaluated for movement linkage
                var indirectRefresh = new HashSet<Graph.Link>();
                foreach (var link in refresh)
                {
                    Rect2[] movementBoundingRects = new Rect2[] {
                        // right jump
                        Rect2.SetLocation(sourceJumpAreaRect, link.LeftNode.Location,Rect2.Location.TopRight)
                             .Offset(sourceObjectRect.Width, sourceObjectRect.Height),
                        // left jump 
                        Rect2.SetLocation(sourceJumpAreaRect, link.RightNode.Location, Rect2.Location.TopLeft)
                             .Offset(-sourceObjectRect.Width, sourceObjectRect.Height),
                        // right fall 
                        Rect2.SetLocation(sourceFallAreaRect, link.LeftNode.Location, Rect2.Location.BottomRight)
                             .Offset(sourceObjectRect.Width, 0),
                        // left fall 
                        Rect2.SetLocation(sourceFallAreaRect, link.RightNode.Location, Rect2.Location.BottomLeft)
                             .Offset(-sourceObjectRect.Width, 0)
                    };

                    foreach (var movementActionRect in movementBoundingRects)
                    {
                        // not needed, can just add directly to indirectRefresh
                        // instea of using this. Remove after debugging.
                        var movementUpdates = new HashSet<Graph.Link>();
                        var reachablePlatforms = rtree.Find(movementActionRect);
                        foreach (var match in reachablePlatforms)
                        {
                            var obj = match.Item1;
                            if (objGraphMap.ContainsKey(obj))
                            {
                                var objGraphData = objGraphMap[obj];
                                foreach (var jumpableLink in objGraphData.links)
                                    movementUpdates.Add(jumpableLink);
                            }
                        }

                        // merge all unique records into indirectRefresh
                        indirectRefresh.UnionWith(movementUpdates);
                    }
                }

                refresh.UnionWith(indirectRefresh);

                // list of any added/updated graph links that 
                // require jump/fall links to be recalcuated
                List<Tuple<Graph.Link, GameObject>> newGraphLinks = new List<Tuple<Graph.Link, GameObject>>();
                foreach (var link in refresh)
                {
                    if (link.AllowsAction(Character.State.Walking) && link.AllowsFlow(Graph.Link.FlowDirection.All))
                    {
                        newGraphLinks.AddRange(
                            LinkJumpableObjects(link, sourceJumpAreaRect, Direction.Left)
                        );
                        newGraphLinks.AddRange(
                            LinkFallableObjects(link, sourceFallAreaRect, Direction.Left)
                        );
                        newGraphLinks.AddRange(
                            LinkJumpableObjects(link, sourceJumpAreaRect, Direction.Right)
                        );
                        newGraphLinks.AddRange(
                            LinkFallableObjects(link, sourceFallAreaRect, Direction.Right)
                        );
                    }
                }

                foreach (var link in newGraphLinks)
                    graph.Add(link.Item1, link.Item2);

                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// Clears and rebuilds a new traversal graph of all scene objects 
        /// contained in all layers specified in the layerMask.
        /// </summary>
        /// <returns>Construction success</returns>
        private bool RebuildTraversalGraph(HashSet<GameObject> objects)
        {
            objGraphMap.Clear();
            graph.Clear();
            rtree.Clear();

            if (AddObjectsToGraph(objects))
            {
                var reachableLinks = new List<Tuple<Graph.Link, GameObject>>();
                foreach (var currPlatformLink in graph.Links)
                {
                    Graph.Link link = currPlatformLink.Value;

                    // find all other links that are jumpable
                    // and link them to this ground link 
                    if (link.Action.HasFlag(Character.State.Walking) && 
                        link.FlowDir == Graph.Link.FlowDirection.All)
                    {
                        reachableLinks.AddRange(
                            LinkJumpableObjects(link, sourceJumpAreaRect, Direction.Left));
                        reachableLinks.AddRange(
                            LinkFallableObjects(link, sourceFallAreaRect, Direction.Left));
                        reachableLinks.AddRange(
                            LinkJumpableObjects(link, sourceJumpAreaRect, Direction.Right));
                        reachableLinks.AddRange(
                            LinkFallableObjects(link, sourceFallAreaRect, Direction.Right));
                    }
                }

                // Add these to the graph at the end so we 
                // don't modify the graph while iterating 
                // through  all links in the loop above
                foreach (var jumpLink in reachableLinks)
                {
                    if (!graph.ContainsLink(jumpLink.Item1, Character.State.Walking))
                        graph.Add(jumpLink.Item1, jumpLink.Item2);
                }
            }

            return true;
        }

        private List<Graph.Link> SplitObjectIntoSegmentLinks(GameObject obj, Line2 objectTopFace)
        {
            var objectSegmentLinks = new List<Graph.Link>();

            // split the object's top face into pieces no larger 
            // than <segmentWidthMultiplier> * the source obj width
            List<Line2> objectLineSegments = objectTopFace.Split(
                sourceObjectRect.Width * segmentWidthMultiplier);

            for (int i = 0; i < objectLineSegments.Count; ++i)
            {
                Line2 lineSegment = objectLineSegments[i];

                var linkName = $"{obj.name}.{i + 1}";
                var linkStates = Character.State.Traversing;
                var linkFlowDir = Graph.Link.FlowDirection.All;

                var link = new Graph.Link(
                    linkName, linkStates, 
                    linkFlowDir, 1.0f, 
                    lineSegment.Start, 
                    lineSegment.End
                );

                objectSegmentLinks.Add(link);
            }

            return objectSegmentLinks;
        }

        /// <summary>
        /// Adds a collection of GameObjects to the graph and spatial index.
        /// </summary>
        /// <param name="objects">The list of objects to be added to the graph</param>
        /// <returns></returns>
        private bool AddObjectsToGraph(HashSet<GameObject> objects)
        {
            var startCount = graph.Count;
            foreach (var obj in objects)
            {
                Rect2 objRect = Utils.Object.GetBoundingRect(obj);
                Line2 topFace = Utils.Object.GetTopFace(obj);
                List<Graph.Link> objLinks = new List<Graph.Link>();

                if (topFace != null)
                {
                    objLinks = SplitObjectIntoSegmentLinks(obj, topFace);
                    if (objLinks.Count > 0)
                    {
                        rtree.Insert(obj, objRect, topFace);
                        foreach (var objSegmentLink in objLinks)
                            graph.Add(objSegmentLink, obj);
                    }
                }

                // map this object to each segment link in the graph
                objGraphMap[obj] = (objRect, objLinks);

            }

            return graph.Count > startCount;
        }

        /// <summary>
        /// Creates all graph links that represent jumpable connections from a platform link.
        /// </summary>
        /// <param name="link">The starting platform link jumping from</param>
        /// <param name="jumpRect">The bounds of the ai character's full jump arc</param>
        /// <param name="direction">The direction of the jump</param>
        /// <returns></returns>
        private List<Tuple<Graph.Link, GameObject>> LinkJumpableObjects(Graph.Link link, Rect2 jumpRect, Direction direction)
        {
            var jumpLinks = new List<Tuple<Graph.Link, GameObject>>();

            var jumpDir = direction == Direction.Left ? Vector2.left : Vector2.right;
            var jumpNode = direction == Direction.Left ? link.LeftNode : link.RightNode;
            var jumpOffset = direction == Direction.Left ? sourceObjectRect.Width : -sourceObjectRect.Width;
            var jumpNodeAlignment = direction == Direction.Left ? Rect2.Location.BottomLeft : Rect2.Location.BottomRight;
            var jumpRectAlignment = direction == Direction.Left ? Rect2.Location.BottomRight : Rect2.Location.BottomLeft;

            // Offset the rect by the target obj width
            var jumpAreaRect = Rect2.SetLocation(
                jumpRect, jumpNode.Location,
                jumpRectAlignment);

            jumpAreaRect.Offset(jumpOffset, 0);

            // look for any other platforms that intersect 
            // with the jump arc prediction bounding rect.
            var reachablePlatforms = rtree.Find(jumpAreaRect);

            // align the follower rect to the edge of the platform so we can calculate the jump arc off of it
            Rect2 jumpPosition = Rect2.SetLocation(sourceObjectRect, jumpNode.Location, jumpNodeAlignment);
            List<Rect2> jumpPredictionArc = source.GetJumpArc(jumpDir, jumpPosition);

            // loop through each platform to see if any are reachable by jumping to it. 
            // If any are jumpable, create a jumpable graph link between the two platforms
            foreach (var plat in reachablePlatforms)
            {
                var platformObj = plat.Item1;
                var platformRect = plat.Item2;
                var platformGeom = plat.Item3;

                if (link.Name.StartsWith(platformObj.name))
                    continue;

                Debug.Assert(objGraphMap.ContainsKey(platformObj));
                if (objGraphMap.ContainsKey(platformObj))
                {
                    var objLinks = objGraphMap[platformObj].links;
                    foreach (var objLink in objLinks)
                    {
                        // only try jumping to this platform if the source 
                        // object's rect isn't above the platform candidate 
                        if (!jumpPosition.Above(objLink.Line))
                        {
                            // if the character isn't above this platform we should now 
                            // check if it's possible to jump to it from the right or left
                            foreach (var jumpArcPosition in jumpPredictionArc)
                            {
                                bool jumpLocAbove = jumpArcPosition.Above(objLink.Line);
                                bool jumpLocOverPlat = jumpArcPosition.OverlapsOnAxis(objLink.Line, Axis.Horizontal);

                                if (jumpLocAbove && jumpLocOverPlat)
                                {
                                    Vector2 closestPlatformPoint = objLink.Line.ClosestEndPoint(jumpNode.Location);
                                    Graph.Node startNode = new Graph.Node(jumpNode.Name, jumpNode.Location);
                                    Graph.Node endNode = new Graph.Node(objLink.Name, closestPlatformPoint);

                                    if (!startNode.Equals(endNode))
                                    {
                                        // create the new jump link that connects these two nodes within the traversal graph
                                        var leftJumpDescr = $"{(direction == Direction.Left ? "Left" : "Right")} Jump from {jumpNode.Name} -> {objLink.Name}";
                                        jumpLinks.Add(
                                            new Tuple<Graph.Link, GameObject>(
                                                new Graph.Link(
                                                    leftJumpDescr, Character.State.Jumping, 
                                                    Graph.Link.FlowDirection.StartToEnd, 
                                                    1.0f, startNode, endNode
                                                ),
                                                platformObj
                                            )
                                        );
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return jumpLinks;
        }

        /// <summary>
        /// Creates all graph links that represent connections from dropping off of a platform link.
        /// </summary>
        /// <param name="link">The starting platform link falling from</param>
        /// <param name="fallRect">The bounds of the ai character's full fall arc</param>
        /// <param name="direction">The direction of the drop</param>
        /// <returns></returns>
        private List<Tuple<Graph.Link, GameObject>> LinkFallableObjects(Graph.Link link, Rect2 fallRect, Direction direction)
        {
            var fallLinks = new List<Tuple<Graph.Link, GameObject>>();

            var fallDir = direction == Direction.Left ? Vector2.left : Vector2.right;
            var fallNode = direction == Direction.Left ? link.LeftNode : link.RightNode;
            var fallNodeAlignment = direction == Direction.Left ? Rect2.Location.BottomRight : Rect2.Location.BottomLeft;
            var fallRectAlignment = direction == Direction.Left ? Rect2.Location.TopRight : Rect2.Location.TopLeft;

            // Offset the rect by the target obj width
            var fallAreaRect = Rect2.SetLocation(
                fallRect, fallNode.Location,
                fallRectAlignment);
            fallAreaRect.Offset(0, sourceObjectRect.Height);

            // look for any other platforms that intersect 
            // with the fall arc prediction bounding rect.
            var reachablePlatforms = rtree.Find(fallAreaRect);

            // Sort the platforms so that we iterate through them 
            // from closest to furthest. This is important since only
            // one fall link is assigned per platform side. If multiple
            // fall linkage is possible it should always be the closest
            var reachablePlatformsSorted = reachablePlatforms.OrderBy(
                plat => Vector3.Distance(fallNode.Location, plat.Item2.Center)).ToArray();

            // align the follower rect to the edge of the platform so we can calculate the drop arc off of it
            Rect2 fallPosition = Rect2.SetLocation(sourceObjectRect, fallNode.Location, fallNodeAlignment);
            List<Rect2> fallPredictionArc = source.GetFallArc(fallDir, fallPosition);

            // loop through each platform to see if any are reachable by falling onto it. 
            // If any are jumpable, create a droppable graph link connection between platforms
            foreach (var plat in reachablePlatformsSorted)
            {
                var platformObj = plat.Item1;
                var platformRect = plat.Item2;
                var platformGeom = plat.Item3;

                if (link.Name.StartsWith(platformObj.name))
                    continue;

                Debug.Assert(objGraphMap.ContainsKey(platformObj));
                if (objGraphMap.ContainsKey(platformObj))
                {
                    var objLinks = objGraphMap[platformObj].links;
                    foreach (var objLink in objLinks)
                    {
                        if (!fallPosition.Below(platformRect))
                        {
                            // if the character isn't above this platform we should now 
                            // check if it's possible to jump to it from right to left
                            foreach (var fallArcPosition in fallPredictionArc)
                            {
                                bool fallLocationIntersectsPlat = fallArcPosition.Above(objLink.Line);
                                bool fallLocationOverlapsPlat = fallArcPosition.OverlapsOnAxis(objLink.Line, Axis.Horizontal);

                                if (fallLocationIntersectsPlat && fallLocationOverlapsPlat)
                                {
                                    Vector2 closestPlatformPoint = objLink.Line.ClosestEndPoint(fallNode.Location);
                                    Vector2 furthestPlatformPoint = closestPlatformPoint == objLink.Line.Start 
                                            ? objLink.Line.End : objLink.Line.Start;

                                    bool closestPointSideIsCorrect = direction == Direction.Left
                                            ? Point2.IsRightOf(fallNode.Location, closestPlatformPoint) 
                                            : Point2.IsLeftOf(fallNode.Location, closestPlatformPoint);

                                    bool furthestPointSideIsCorrect = direction == Direction.Left
                                            ? Point2.IsRightOf(fallNode.Location, furthestPlatformPoint) 
                                            : Point2.IsLeftOf(fallNode.Location, furthestPlatformPoint);

                                    Vector2 fallLinkPlatPoint = closestPointSideIsCorrect
                                            ? closestPlatformPoint : furthestPointSideIsCorrect
                                            ? furthestPlatformPoint : Vector2.zero;

                                    if (fallLinkPlatPoint != Vector2.zero)
                                    {
                                        Graph.Node startNode = new Graph.Node(fallNode.Name, fallNode.Location);
                                        Graph.Node endNode = new Graph.Node(objLink.Name, fallLinkPlatPoint);

                                        if (!startNode.Equals(endNode))
                                        {
                                            // create the new jump link that connects these two nodes within the traversal graph
                                            var leftJumpDescr = $"{(direction == Direction.Left ? "Left" : "Right")} Drop from {fallNode.Name} -> {objLink.Name}";
                                            fallLinks.Add(
                                                new Tuple<Graph.Link, GameObject>(
                                                    new Graph.Link(leftJumpDescr, Character.State.Falling, Graph.Link.FlowDirection.StartToEnd, 1.0f, startNode, endNode),
                                                    platformObj
                                                )
                                            );

                                            break;
                                        }
                                    }
                                }
                            }

                            // TODO: test this
                            // only allow falling down to one platform
                            if (fallLinks.Count > 0)
                                break;
                        }
                    }
                }
            }

            return fallLinks;
        }

        /// <summary>
        /// Wraps debugging timer functionality
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="message"></param>
        private void DebugTiming(Stopwatch sw, string message = null)
        {
            if (enableTiming)
            {
                if (message == null)
                {
                    sw.Start();
                }
                else
                {
                    sw.Stop();
                    UnityEngine.Debug.Log(message);
                }
            }
        }

        /// <summary>
        /// Draws traversal graph diagnostic information
        /// </summary>
        private void OnDrawGizmos()
        {
            graph.DrawGizmos();

            // Draw traceback
            foreach (var pathItem in traceback)
            {
                var line = pathItem.Item1.Line;
                Utils.Debug.DrawLine(line, Color.yellow, 6);
            }
        }
    }
}