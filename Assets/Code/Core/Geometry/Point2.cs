using System;
using System.Diagnostics;
using UnityEngine;

namespace Core.Geom
{
    [DebuggerDisplay("({X,nq}, {Y,nq})")] 
    public static class Point2
    {
        /// <summary>
        /// Returns the leftmost point (horizontally)
        /// </summary>
        public static Vector2 LeftPoint(Vector2 ptA, Vector2 ptB)
        {
            return (ptA.x < ptB.x) ? ptA : ptB;
        }

        /// <summary>
        /// Returns the rightmost point (horizontally)
        /// </summary>
        public static Vector2 RightPoint(Vector2 ptA, Vector2 ptB)
        {
            return (ptA.x > ptB.x) ? ptA : ptB;
        }

        /// <summary>
        /// Returns the highest point (vertically)
        /// </summary>
        public static Vector2 TopPoint(Vector2 ptA, Vector2 ptB)
        {
            return (ptA.y > ptB.y) ? ptA : ptB;
        }

        /// <summary>
        /// Returns the lowest point (vertically)
        /// </summary>
        public static Vector2 BottomPoint(Vector2 ptA, Vector2 ptB)
        {
            return (ptA.y < ptB.y) ? ptA : ptB;
        }

        /// <summary>
        /// Returns true if ptA is located to the left of ptB
        /// </summary>
        public static bool IsLeftOf(Vector2 ptA, Vector2 ptB)
        {
            return ptA.x < ptB.x;
        }

        /// <summary>
        /// Returns true if ptA is located to the right of ptB
        /// </summary>
        public static bool IsRightOf(Vector2 ptA, Vector2 ptB)
        {
            return ptA.x > ptB.x;
        }

        /// <summary>
        /// Returns true if ptA is located above ptB
        /// </summary>
        public static bool IsAbove(Vector2 ptA, Vector2 ptB)
        {
            return ptA.y > ptB.y;
        }

        /// <summary>
        /// Returns true if ptA is located below ptB
        /// </summary>
        public static bool IsBelow(Vector2 ptA, Vector2 ptB)
        {
            return ptA.y < ptB.y;
        }

        /// <summary>
        /// Returns a string representation of the point 
        /// truncated at 2 decimal places. The trunation
        /// is applied since this string is the base data
        /// used to create the HashCode for anything that
        /// contains a Vector2 point to account for very
        /// small differences in points that should be 
        /// considered identical.
        /// </summary>
        public static string ToString(Vector2 pt)
        {
            double roundedX = Math.Round(pt.x, 2);
            double roundedY = Math.Round(pt.y, 2);
            var str = $"({roundedX} {roundedY})";
            return str;
        }

        /// <summary>
        /// Returns a unique integer key 
        /// generated from it's location string.
        /// </summary>
        public static int GetKey(Vector2 pt)
        {
            return Point2.ToString(pt).GetHashCode();
        }
    }
}
