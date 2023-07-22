using System;
using System.Collections.Generic;

namespace Core.Queue
{
    public class PriorityQueue<T> 
        where T : IComparable<T>
    {
        private List<T> data;

        public PriorityQueue()
        {
            this.data = new List<T>();
        }

        /// <summary>
        /// Returns the number of items 
        /// in the priority queue
        /// </summary>
        public int Count
        {
            get => data.Count;
        }

        /// <summary>
        /// Push a new item on to the queue.
        /// </summary>
        public void Enqueue(T item)
        {
            data.Add(item);

            // child index; start at end
            int childIndex = data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2; // parent index
                if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
                {
                    // child item is larger than (or 
                    // equal) parent so we're done
                    break;
                }

                T tmp = data[childIndex]; 
                data[childIndex] = data[parentIndex]; 
                data[parentIndex] = tmp;
                
                childIndex = parentIndex;
            }
        }

        /// <summary>
        /// Pops off the front item of the queue, which should always be
        /// the item with the highest priority determined by IComparible.
        /// </summary>
        public T Dequeue()
        {
            if (Count == 0)
                return default;
            else
            {
                int lastIndex = data.Count - 1;

                // Fetch the item at the front of the queue
                T frontItem = data[0];
                data[0] = data[lastIndex];
                data.RemoveAt(lastIndex);

                // Update the last item index to 
                // compensate for the item removal/pop
                --lastIndex;

                // Parent index, starting
                // at front of the queue
                int parentIndex = 0;

                while (true)
                {
                    int leftChildIndex = parentIndex * 2 + 1;
                    if (leftChildIndex > lastIndex)
                    {
                        // no children exist
                        break;
                    }

                    int rightChildIndex = leftChildIndex + 1;
                    if (rightChildIndex <= lastIndex && data[rightChildIndex].CompareTo(data[leftChildIndex]) < 0)
                    {
                        // if there is a right child (leftChildIdx + 1), and it's
                        // less than the left child idx, use the right one instead
                        leftChildIndex = rightChildIndex;
                    }

                    if (data[parentIndex].CompareTo(data[leftChildIndex]) <= 0)
                    {
                        // parent is smaller than or equal
                        // to the smallest child so done
                        break;
                    }

                    // Swap parent and child items 
                    // within the priority queue
                    T tmp = data[parentIndex]; 
                    data[parentIndex] = data[leftChildIndex]; 
                    data[leftChildIndex] = tmp; 

                    parentIndex = leftChildIndex;
                }

                return frontItem;
            }
        }

        /// <summary>
        /// Returns the front item of the queue
        /// without removing it from the queue.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            T frontItem = data[0];
            return frontItem;
        }

        /// <summary>
        /// Returns true if the heap 
        /// property is true for all data
        /// </summary>
        public bool IsConsistent()
        {
            if (data.Count == 0) 
                return true;

            int lastIndex = data.Count - 1;
            for (int parentIdx = 0; parentIdx < data.Count; ++parentIdx)
            {
                int leftChildIdx = 2 * parentIdx + 1; 
                if (leftChildIdx <= lastIndex && data[parentIdx].CompareTo(data[leftChildIdx]) > 0)
                    return false;

                int rightChildIdx = 2 * parentIdx + 2;
                if (rightChildIdx <= lastIndex && data[parentIdx].CompareTo(data[rightChildIdx]) > 0)
                    return false; 
            }

            return true;
        }

        /// <summary>
        /// Returns a string representation of the queue
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < data.Count; ++i)
            {
                ret += $"{data[i].ToString()} ";
            }

            ret += $"count = {data.Count}";
            return ret;
        }
    }
}
