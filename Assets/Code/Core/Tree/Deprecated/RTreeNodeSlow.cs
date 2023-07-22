using System;
using System.Linq;
using System.Collections.Generic;

namespace Core.Tree
{
    using Core.Geom;
    using Core.Spatial;
    using UnityEngine;

    public class RTreeNodeSlow<T> : ISpatial, IEquatable<RTreeNodeSlow<T>>
        where T : ISpatial
    {
        public const int DefaultNodeSize = 5;

        public RTreeNodeSlow(int size = DefaultNodeSize)
        {
            MaxElementCount = Math.Max(3, size);
        }

        private int MaxElementCount
        {
            get;
        }

        private int MinElementCount
        {
            get => Math.Max(2, (int)(MaxElementCount * 0.4f));
        }

        public List<T> Items
        {
            get;
            private set;
        } = new List<T>();

        public RTreeNodeSlow<T> Parent
        {
            get;
            private set;
        } = null;

        public List<RTreeNodeSlow<T>> Children
        {
            get;
            private set;
        } = new List<RTreeNodeSlow<T>>();

        public Rect2 BoundingBox
        {
            get;
            private set;
        } = Rect2.Empty;

        public bool IsLeaf
        {
            get => Items.Count > 0 &&
                   Children.Count == 0;
        }

        public bool IsBranch
        {
            get => Items.Count == 0 &&
                   Children.Count > 0;
        }

        public bool IsEmpty
        {
            get => Items.Count == 0 &&
                   Children.Count == 0 &&
                   BoundingBox.IsEmpty;
        }

        public int ElementCount
        {
            get
            {
                if (IsBranch)
                {
                    return Children.Count;
                }
                else
                {
                    return Items.Count;
                }
            }
        }

        public int Depth
        {
            get
            {
                if (Parent != null)
                    return Parent.Depth + 1;
                return 0;
            }
        }

        public bool Equals(RTreeNodeSlow<T> other)
        {
            if (BoundingBox != other.BoundingBox)
                return false;
            if (Depth != other.Depth)
                return false;
            if (MinElementCount != other.MinElementCount)
                return false;
            if (MaxElementCount != other.MaxElementCount)
                return false;
            if (Items.Count != other.Items.Count)
                return false;
            if (Children.Count != other.Children.Count)
                return false;
            if (Items != other.Items)
                return false;
            if (Children != other.Children)
                return false;
            return true;
        }

        public List<T> Find(Rect2 rect)
        {
            List<T> ret = new List<T>();

            if (BoundingBox.IntersectsWith(rect))
            {
                if (IsBranch)
                {
                    foreach (var child in Children)
                    {
                        var matches = child.Find(rect);
                        ret.AddRange(matches.Except(ret));
                    }
                }
                else if (IsLeaf)
                {
                    foreach (var item in Items)
                    {
                        if (item.BoundingBox.IntersectsWith(rect))
                            ret.Add(item);
                    }
                }
            }

            return ret;
        }

        public bool Insert(T item)
        {
            bool added = false;

            if (IsBranch)
            {
                // Find the child node that needs to be expanded 
                int idx = SelectBestInsertSubtree(item);
                if (Children.ElementAtOrDefault(idx) != null)
                {
                    // Traverse down the tree by calling AddItem
                    // for the child node that fits the item best
                    added = Children[idx].Insert(item);
                    if (added && !BoundingBox.Contains(item.BoundingBox))
                    {
                        // If the item was added, make sure to update this 
                        // branch node's extent to include the item
                        BoundingBox = Rect2.Merge(BoundingBox, item.BoundingBox);
                    }
                }
                else
                {
                    // if we weren't able to find a child node, something went wrong
                    throw new Exception("Failed to find best child for item insert");
                }
            }
            else
            {
                added = InsertItem(item);
                if (added && Items.Count > MaxElementCount)
                {
                    // If this insert just led to an overflow of this 
                    // node it needs to be split so the elements can 
                    // be spread amongst the new children nodes
                    added &= HandleElementOverflow();
                }

                Debug.Assert(added);
            }

            return added;
        }

        private bool InsertItem(T item)
        {
            Items.Add(item);
            BoundingBox = Rect2.Merge(BoundingBox, item.BoundingBox);
            return true;
        }

        private bool AddChild(RTreeNodeSlow<T> child)
        {
            child.Parent = this;
            Children.Add(child);
            BoundingBox = Rect2.Merge(BoundingBox, child.BoundingBox);
            return true;
        }

        private int SelectBestInsertSubtree(T item)
        {
            float leastExpansionAmount = int.MaxValue;
            float leastExpansionArea = 0;
            int leastExpansionIndex = -1;

            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
                float enlargement = child.BoundingBox.MergeEnlargement(item.BoundingBox);
                if (enlargement < leastExpansionAmount ||
                    (enlargement == leastExpansionAmount &&
                     child.BoundingBox.Area < leastExpansionArea))
                {
                    leastExpansionIndex = i;
                    leastExpansionAmount = enlargement;
                    leastExpansionArea = child.BoundingBox.Area;
                }
            }

            return leastExpansionIndex;
        }

       private bool HandleElementOverflow()
        {
            bool ret = false;

            if (IsLeaf)
            {
                // split this leaf into two new nodes
                var leafSplit = LeafSplitQuadratic();

                if (leafSplit == null)
                {
                    // If the split result is null, something went wrong
                    Debug.LogError("RTree : failed to split leaf node");
                }
                else if (Parent == null)
                {
                    // We should only ever get here if 
                    // the root node is being split
                    Debug.Assert(!IsBranch);

                    // Clear all items from this leaf
                    // so we can grow the tree taller
                    // by assigning the split nodes
                    // as children of this one.
                    BoundingBox.Clear();
                    Items.Clear();

                    ret = AddChild(leafSplit.Item1)
                       && AddChild(leafSplit.Item2);
                }
                else
                {
                    // Grow the tree wider by swapping out
                    // this node for both of the new split
                    // nodes. This will allow the tree to 
                    // stay better balanced at the cost of
                    // possibly having to split the parent.
                    if (Parent.RemoveChild(this))
                    {
                        ret = Parent.AddChild(leafSplit.Item1)
                           && Parent.AddChild(leafSplit.Item2);
                    }
                }
            }
            else
            {
                // split this branch into two new branch nodes
                var branchSplit = BranchSplitQuadratic();

                if (branchSplit == null)
                {
                    Debug.LogError("RTree : Failed to split branch node");
                    ret = false;
                }
                else if (Parent == null)
                {
                    // this condition means the root
                    // branch node is being split. Since
                    // there's no direct parent node to 
                    // worry about, just make these two 
                    // new nodes the root's children
                    this.BoundingBox.Clear();
                    this.Children.Clear();

                    ret = this.AddChild(branchSplit.Item1)
                       && this.AddChild(branchSplit.Item2);
                }
                else
                {
                    // Grow the tree wider by swapping out
                    // this node for both of the new split
                    // nodes. This will allow the tree to 
                    // stay better balanced at the cost of
                    // possibly having to split the parent.
                    if (Parent.RemoveChild(this))
                    {
                        ret = Parent.AddChild(branchSplit.Item1)
                           && Parent.AddChild(branchSplit.Item2);
                    }
                }
            }

            if (Parent != null && Parent.Children.Count > MaxElementCount)
            {
                // If this node was split somewhere above when handling
                // the overflow, the resulting nodes of the split would
                // have been added to the parent node, leading to this.
                ret &= Parent.HandleElementOverflow();
            }

            return ret;
        }

        Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>> BranchSplitQuadratic()
        {
            Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>> branchSplit = null;

            Axis[] dimensions = {
                Axis.Horizontal,
                Axis.Vertical
            };

            int highestLowIndex = -1;
            int lowestHighIndex = -1;
            float maxNormalizedSeparation = float.NegativeInfinity;

            // For each node axis, find the two node items
            // that are most separated from one another. 
            foreach (var dim in dimensions)
            {
                float nodeAxisMin = BoundingBox.AxisMinimum(dim);
                float nodeAxisMax = BoundingBox.AxisMaximum(dim);

                int tempHighestLowIndex = Children.Count - 1;
                int tempLowestHighIndex = Children.Count - 1;
                float tempHighestLow = Children.Last().BoundingBox.AxisMinimum(dim);
                float tempLowestHigh = Children.Last().BoundingBox.AxisMaximum(dim);

                for (int i = 0; i < Children.Count; i++)
                {
                    float tempLow = Children[i].BoundingBox.AxisMinimum(dim);
                    if (tempLow >= tempHighestLow)
                    {
                        tempHighestLow = tempLow;
                        tempHighestLowIndex = i;
                    }
                    else
                    {
                        float tempHigh = Children[i].BoundingBox.AxisMaximum(dim);
                        if (tempHigh <= tempLowestHigh)
                        {
                            tempLowestHigh = tempHigh;
                            tempLowestHighIndex = i;
                        }
                    }

                    // calculate the separation of each axis based on it's 
                    // ratio within the minimum bounding rect of this node 
                    float separation = (tempHighestLow - tempLowestHigh) / (nodeAxisMax - nodeAxisMin);
                    Debug.Assert(separation <= 1 && separation >= -1);

                    // choose the pair with the greatest
                    // normalized separation along any dimension.
                    if (separation > maxNormalizedSeparation)
                    {
                        maxNormalizedSeparation = separation;
                        highestLowIndex = tempHighestLowIndex;
                        lowestHighIndex = tempLowestHighIndex;
                    }
                }
            }

            // make sure we found two different children
            if (highestLowIndex > -1 && lowestHighIndex > -1 && highestLowIndex != lowestHighIndex)
            {
                RTreeNodeSlow<T> c1 = new RTreeNodeSlow<T>(MaxElementCount);
                RTreeNodeSlow<T> c2 = new RTreeNodeSlow<T>(MaxElementCount);

                // insert each of the children we found above 
                // into the two new split child nodes
                c1.AddChild(Children[lowestHighIndex]);
                c2.AddChild(Children[highestLowIndex]);

                // create a list of any remaining items
                List<RTreeNodeSlow<T>> remainingChildren = new List<RTreeNodeSlow<T>>(
                    Children.Except(new RTreeNodeSlow<T>[] { Children[lowestHighIndex], Children[highestLowIndex] })
                );

                // continue to loop through until all remaining children have 
                // been assigned to either of these new nodes
                while (remainingChildren.Count > 0)
                {
                    // if we're at risk of running out of children before meeting the min
                    // elem count needed in the other node, add all remaining to it
                    if (c1.ElementCount + remainingChildren.Count == MinElementCount)
                    {
                        foreach (var child in remainingChildren)
                            c1.AddChild(child);
                        remainingChildren.Clear();
                    }
                    else if (c2.ElementCount + remainingChildren.Count == MinElementCount)
                    {
                        foreach (var child in remainingChildren)
                            c2.AddChild(child);
                        remainingChildren.Clear();
                    }

                    // if neither node is full yet, iterate through every remaining item
                    // to find the one that leads to a biggest enlargement difference 
                    // between the two nodes. Whichever is the worst offender should 
                    // be added to the node it affects the least. This should lead to
                    // an optimal set of elements in each node if we don't end up in 
                    // the two if statements above and have to blindly insert items.
                    else
                    {
                        int insertNodeID = -1;
                        int nodeElementIdx = -1;

                        float maxEnlargementDiff = float.NegativeInfinity;
                        for (int remIdx = 0; remIdx < remainingChildren.Count; ++remIdx)
                        {
                            var remItem = remainingChildren[remIdx];

                            // measure how much each node would have to be enlarged for this item
                            float c1Enlargement = c1.BoundingBox.MergeEnlargement(remItem.BoundingBox);
                            float c2Enlargement = c2.BoundingBox.MergeEnlargement(remItem.BoundingBox);
                            float enlargementDiff = Math.Abs(c1Enlargement - c2Enlargement);

                            if (enlargementDiff > maxEnlargementDiff)
                            {
                                if (c1Enlargement < c2Enlargement)
                                    insertNodeID = 1;
                                else if (c1Enlargement > c2Enlargement)
                                    insertNodeID = 2;
                                else if (c1.BoundingBox.Area < c2.BoundingBox.Area)
                                    insertNodeID = 1;
                                else if (c1.BoundingBox.Area > c2.BoundingBox.Area)
                                    insertNodeID = 2;
                                else if (c2.ElementCount < (MaxElementCount / 2))
                                    insertNodeID = 1;
                                else
                                    insertNodeID = 2;

                                maxEnlargementDiff = enlargementDiff;
                                nodeElementIdx = remIdx;
                            }
                        }

                        Debug.Assert(insertNodeID > -1 && insertNodeID > -1);
                        var elem = remainingChildren[nodeElementIdx];

                        if (insertNodeID == 1)
                            c1.AddChild(elem);
                        else if (insertNodeID == 2)
                            c2.AddChild(elem);

                        remainingChildren.RemoveAt(nodeElementIdx);
                    }
                }

                branchSplit = new Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>>(c1, c2);
            }

            Debug.Assert(branchSplit != null);
            return branchSplit;
        }

        Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>> LeafSplitQuadratic()
        {
            Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>> leafSplit = null;

            Axis[] dimensions = { 
                Axis.Horizontal, 
                Axis.Vertical 
            };

            int highestLowIndex = -1;
            int lowestHighIndex = -1;
            float maxNormalizedSeparation = float.NegativeInfinity;
            
            // For each node axis, find the two node items
            // that are most separated from one another. 
            foreach (var dim in dimensions)
            {
                float nodeAxisMin = BoundingBox.AxisMinimum(dim);
                float nodeAxisMax = BoundingBox.AxisMaximum(dim);

                int tempHighestLowIndex = Items.Count - 1;
                int tempLowestHighIndex = Items.Count - 1;
                float tempHighestLow = Items.Last().BoundingBox.AxisMinimum(dim);
                float tempLowestHigh = Items.Last().BoundingBox.AxisMaximum(dim);
                
                for (int i = 0; i < Items.Count; i++)
                {
                    float tempLow = Items[i].BoundingBox.AxisMinimum(dim);
                    if (tempLow >= tempHighestLow)
                    {
                        tempHighestLow = tempLow;
                        tempHighestLowIndex = i;
                    }
                    else
                    { 
                        float tempHigh = Items[i].BoundingBox.AxisMaximum(dim);
                        if (tempHigh <= tempLowestHigh)
                        {
                            tempLowestHigh = tempHigh;
                            tempLowestHighIndex = i;
                        }
                    }

                    // calculate the separation of each axis based on it's 
                    // ratio within the minimum bounding rect of this node 
                    float separation = (tempHighestLow - tempLowestHigh) / (nodeAxisMax - nodeAxisMin);
                    Debug.Assert(separation <= 1 && separation >= -1);

                    // choose the pair with the greatest
                    // normalized separation along any dimension.
                    if (separation > maxNormalizedSeparation)
                    {
                        maxNormalizedSeparation = separation;
                        highestLowIndex = tempHighestLowIndex;
                        lowestHighIndex = tempLowestHighIndex;
                    }
                }
            }

            // make sure we found two different items
            if (highestLowIndex > -1 && lowestHighIndex > -1 && highestLowIndex != lowestHighIndex)
            {
                RTreeNodeSlow<T> c1 = new RTreeNodeSlow<T>(MaxElementCount);
                RTreeNodeSlow<T> c2 = new RTreeNodeSlow<T>(MaxElementCount);

                // insert each of the items we found above 
                // into the two new split child nodes
                c1.InsertItem(Items[lowestHighIndex]);
                c2.InsertItem(Items[highestLowIndex]);

                // create a list of any remaining items
                List<T> remainingItems = new List<T>(
                    Items.Except(new T[] { Items[lowestHighIndex], Items[highestLowIndex] })
                );

                // continue to loop through until all remaining items have 
                // been assigned to either of these new nodes
                while (remainingItems.Count > 0)
                {
                    // if we're at risk of running out of items before meeting the min
                    // elem count needed in the other node, add all remaining to it
                    if (c1.ElementCount + remainingItems.Count == MinElementCount)
                    {
                        foreach (var item in remainingItems)
                            c1.InsertItem(item);
                        remainingItems.Clear();
                    }
                    else if (c2.ElementCount + remainingItems.Count == MinElementCount)
                    {
                        foreach (var item in remainingItems)
                            c2.InsertItem(item);
                        remainingItems.Clear();
                    }

                    // if neither node is full yet, iterate through every remaining item
                    // to find the one that leads to a biggest enlargement difference 
                    // between the two nodes. Whichever is the worst offender should 
                    // be added to the node it affects the least. This should lead to
                    // an optimal set of elements in each node if we don't end up in 
                    // the two if statements above and have to blindly insert items.
                    else
                    {
                        int insertNodeID = -1;
                        int nodeElementIdx = -1;

                        float maxEnlargementDiff = float.NegativeInfinity;
                        for (int remIdx = 0; remIdx < remainingItems.Count; ++remIdx)
                        {
                            var remItem = remainingItems[remIdx];

                            // measure how much each node would have to be enlarged for this item
                            float c1Enlargement = c1.BoundingBox.MergeEnlargement(remItem.BoundingBox);
                            float c2Enlargement = c2.BoundingBox.MergeEnlargement(remItem.BoundingBox);
                            float enlargementDiff = Math.Abs(c1Enlargement - c2Enlargement);

                            if (enlargementDiff > maxEnlargementDiff)
                            {
                                if (c1Enlargement < c2Enlargement)
                                    insertNodeID = 1;
                                else if (c1Enlargement > c2Enlargement)
                                    insertNodeID = 2;
                                else if (c1.BoundingBox.Area < c2.BoundingBox.Area)
                                    insertNodeID = 1;
                                else if (c1.BoundingBox.Area > c2.BoundingBox.Area)
                                    insertNodeID = 2;
                                else if (c2.ElementCount < (MaxElementCount / 2))
                                    insertNodeID = 1;
                                else
                                    insertNodeID = 2;

                                maxEnlargementDiff = enlargementDiff;
                                nodeElementIdx = remIdx;
                            }
                        }

                        Debug.Assert(insertNodeID > -1 && insertNodeID > -1);
                        var elem = remainingItems[nodeElementIdx];
                        
                        if (insertNodeID == 1)
                            c1.InsertItem(elem);
                        else if (insertNodeID == 2)
                            c2.InsertItem(elem);
                        
                        remainingItems.RemoveAt(nodeElementIdx);
                    }
                }

                leafSplit = new Tuple<RTreeNodeSlow<T>, RTreeNodeSlow<T>>(c1, c2);
            }

            Debug.Assert(leafSplit != null);
            return leafSplit;
        }

        private bool RemoveChild(RTreeNodeSlow<T> child)
        {
            int removalIdx = Children.FindIndex(c => c.Equals(child));
            if (removalIdx == -1)
            {
                Debug.LogError("RTree : failed to find child to remove");
                return false;
            }

            //if (!ExtentRect.Contains(child.ExtentRect))
            //{
                // if the current extent rect doesn't fully contain the child
                // extent rect, it needs to be updated to reflect the removal
                BoundingBox = Rect2.Empty;
                foreach (var c in Children)
                {
                    if (!c.Equals(child))
                        BoundingBox = Rect2.Merge(BoundingBox, c.BoundingBox);
                }
            //}

            Children.RemoveAt(removalIdx);
            return true;
        }

        public List<Tuple<Rect2, int, bool>> GetRectangles()
        {
            var ret = new List<Tuple<Rect2, int, bool>>();

            // add the rect info for this node
            ret.Add(new Tuple<Rect2, int, bool>(
                BoundingBox, Depth, IsLeaf));

            // append all child node rect info
            foreach (var child in Children)
            {
                var childRects = child.GetRectangles();
                ret.AddRange(childRects.Except(ret));
            }

            // collect all item rectangles
            foreach (var item in Items)
            {
                ret.Add(new Tuple<Rect2, int, bool>(
                    item.BoundingBox, Depth + 1, true));
            }

            return ret;
        }

        public float GetPerimiterSum()
        {
            float ret = 0;

            var rects = GetRectangles();
            foreach (var rect in rects)
            {
                ret += rect.Item1.Perimeter;
            }

            return ret;
        }
    }
}