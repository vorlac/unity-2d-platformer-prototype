using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Tree
{
    using Core.Geom;
    using UnityEngine;

    public class RTree<TObj, TGeom>
    {
        /// <summary>
        /// Maps all graph object hash IDs to the object data:
        /// <list type="number">
        /// <item>the object</item>
        /// <item>the object's bounding box</item>
        /// <item>the object's geometry</item>
        /// </list>
        /// </summary>
        public Dictionary<int, Tuple<TObj, Rect2, TGeom>> Items { get; } = 
           new Dictionary<int, Tuple<TObj, Rect2, TGeom>>();

        /// <summary>
        /// The number of items in the tree
        /// </summary>
        public int Count { get => Items.Count; }

        /// <summary>
        /// The maximum number of elements allowed in 
        /// each rtree node before it has to be split
        /// </summary>
        public int NodeSize { get; } = RTreeNode.DefaultNodeSize;

        /// <summary>
        /// The root node of the rtree 
        /// </summary>
        private RTreeNode root = null;

        /// <summary>
        /// The multi-reader lock
        /// </summary>
        private ReaderWriterLock locker = new ReaderWriterLock();

        /// <summary>
        /// Tree read timeout (10ms)
        /// </summary>
        private static TimeSpan readerLockTimeout = System.TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// Tree write timeout (20ms)
        /// </summary>
        private static TimeSpan writerLockTimeout = System.TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// Read timeout counter
        /// </summary>
        private volatile int readerTimeouts = 0;

        /// <summary>
        /// Write timout counter
        /// </summary>
        private volatile int writerTimeouts = 0;


        public RTree(int nodeSize = RTreeNode.DefaultNodeSize)
        {
            NodeSize = nodeSize;
            root = new RTreeNode(NodeSize);
        }

        public bool Insert(TObj obj, Rect2 rect, TGeom geom)
        {
            bool inserted = false;

            Debug.Assert(rect.Area > 0);

            try
            {
                locker.AcquireWriterLock(writerLockTimeout);
                try
                {
                    // TODO: change this... pass in key instead
                    int key = geom.GetHashCode();
                    inserted = root.Insert(key, rect);
                    Items.Add(key, new Tuple<TObj, Rect2, TGeom>(obj, rect, geom));
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                Debug.Assert(false);
                Interlocked.Increment(ref writerTimeouts);
                Console.WriteLine("Writer Timeout: {0}", writerTimeouts);
            }

            return inserted;
        }

        public List<Tuple<TObj, Rect2, TGeom>> Find(Rect2 rect)
        {
            List<Tuple<TObj, Rect2, TGeom>> ret = new List<Tuple<TObj, Rect2, TGeom>>();

            try
            {
                locker.AcquireReaderLock(readerLockTimeout);
                try
                {
                    var matches = root.Find(rect);
                    foreach (var match in matches)
                    {
                        var key = match.Item1;
                        var box = match.Item2;
                        var item = Items[key];
                        ret.Add(item);
                    }
                    
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException)
            {
                Debug.Assert(false);
                Interlocked.Increment(ref readerTimeouts);
                Console.WriteLine("Reader Timeout: {0}", readerTimeouts);
            }

            return ret;
        }

        public void Clear()
        {
            try
            {
                locker.AcquireWriterLock(writerLockTimeout);
                try
                {
                    root = new RTreeNode(NodeSize);
                    Items.Clear();
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                Debug.Assert(false);
                Interlocked.Increment(ref writerTimeouts);
                Console.WriteLine("Writer Timeout: {0}", writerTimeouts);
            }
        }

        public List<Tuple<Rect2, int, bool>> GetAllTreeRectangles()
        {
            return root.GetRectangles();
        }

        public float GetPerimiterSum()
        {
            return root.GetPerimiterSum();
        }
    }
}