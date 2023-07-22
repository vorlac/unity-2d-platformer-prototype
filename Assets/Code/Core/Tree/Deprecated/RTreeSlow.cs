using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Tree
{
    using Core.Geom;
    using Core.Spatial;

    public class RTreeSlow<T>
        where T : ISpatial
    {
        private RTreeNodeSlow<T> root = null;
        private ReaderWriterLock locker = new ReaderWriterLock();
        private static TimeSpan ReaderLockTimeout = System.TimeSpan.FromMilliseconds(10);
        private static TimeSpan WriterLockTimeout = System.TimeSpan.FromMilliseconds(20);
        private volatile int readerTimeouts = 0;
        private volatile int writerTimeouts = 0;

        public int ItemCount 
        { 
            get; 
            private set; 
        } = 0;

        public int NodeSize 
        { 
            get; 
        } = RTreeNodeSlow<T>.DefaultNodeSize;

        public RTreeSlow(int nodeSize = RTreeNodeSlow<T>.DefaultNodeSize)
        {
            NodeSize = nodeSize;
            root = new RTreeNodeSlow<T>(NodeSize);
        }

        public bool Insert(T item)
        {
            bool inserted = false;

            try
            {
                locker.AcquireWriterLock(WriterLockTimeout);
                try
                {
                    inserted = root.Insert(item);
                    ++ItemCount;
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                Interlocked.Increment(ref writerTimeouts);
                Console.WriteLine("Writer Timeout: {0}", writerTimeouts);
            }

            return inserted;
        }

        public List<T> Find(Rect2 rect)
        {
            List<T> items = new List<T>();

            try
            {
                locker.AcquireReaderLock(ReaderLockTimeout);
                try
                {
                    ++ItemCount;
                    items = root.Find(rect);
                }
                finally
                {
                    locker.ReleaseReaderLock();
                }
            }
            catch (ApplicationException)
            {
                Interlocked.Increment(ref readerTimeouts);
                Console.WriteLine("Reader Timeout: {0}", readerTimeouts);
            }

            return items;
        }

        public void Clear()
        {
            try
            {
                locker.AcquireWriterLock(WriterLockTimeout);
                try
                {
                    root = new RTreeNodeSlow<T>(NodeSize);
                    ItemCount = 0;
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
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