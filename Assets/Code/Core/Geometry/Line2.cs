using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Geom
{
    public enum LineInterType
    {
        /// <summary>
        /// Two segments with different slopes which do not intersect
        /// </summary>
        NonParallelNone,
        /// <summary>
        /// Two segments with different slopes which intersect at a 
        /// single point.
        /// </summary>
        NonParallelPoint,
        /// <summary>
        /// Two parallel but not coincident segments. These never intersect
        /// </summary>
        ParallelNone,
        /// <summary>
        /// Two coincident segments which do not intersect
        /// </summary>
        CoincidentNone,
        /// <summary>
        /// Two coincident segments which intersect at a point
        /// </summary>
        CoincidentPoint,
        /// <summary>
        /// Two coincident segments which intersect on infinitely many points
        /// </summary>
        CoincidentLine
    }

    /// <summary>
    /// Describes a line. Does not have position and is meant to be reused.
    /// </summary>
    public class Line2
    {
        public static readonly Line2 Empty = new Line2();

        /// <summary>
        /// Where the line begins
        /// </summary>
        public readonly Vector2 Start;

        /// <summary>
        /// Where the line ends
        /// </summary>
        public readonly Vector2 End;

        /// <summary>
        /// End - Start
        /// </summary>
        public readonly Vector2 Delta;

        /// <summary>
        /// Normalized Delta
        /// </summary>
        public readonly Vector2 Axis;

        /// <summary>
        /// The normalized normal of axis.
        /// </summary>
        public readonly Vector2 Normal;

        /// <summary>
        /// The center point of the line.
        /// </summary>
        public readonly Vector2 Centroid;

        /// <summary>
        /// Square of the magnitude of this line
        /// </summary>
        public readonly float MagnitudeSquared;

        /// <summary>
        /// The magnitude
        /// </summary>
        public readonly float Length;

        /// <summary>
        /// The minimum x coordinate
        /// </summary>
        public readonly float MinX;
        /// <summary>
        /// The minimum y coordinate
        /// </summary>
        public readonly float MinY;

        /// <summary>
        /// The maximum x coordinate
        /// </summary>
        public readonly float MaxX;

        /// <summary>
        ///The maximum y coordinate
        /// </summary>
        public readonly float MaxY;

        /// <summary>
        /// The slope of this line
        /// </summary>
        public readonly float Slope;

        /// <summary>
        /// Where this line would hit the y 
        /// intercept. NaN if vertical line.
        /// </summary>
        public readonly float YIntercept;

        /// <summary>
        /// If this line is horizontal
        /// </summary>
        public readonly bool Horizontal;

        /// <summary>
        /// If this line is vertical
        /// </summary>
        public readonly bool Vertical;

        /// <summary>
        /// Creates a new empty line
        /// </summary>
        public Line2()
            : this(new Vector2(float.NegativeInfinity, float.NegativeInfinity), 
                   new Vector2(float.PositiveInfinity, float.PositiveInfinity))
        {
        }

        /// <summary>
        /// Copy construct a new line 
        /// from an existing line
        /// </summary>
        /// <param name="line"></param>
        public Line2(Line2 line)
            : this(line.Start, line.End)
        {
        }

        /// <summary>
        /// Creates a line from start to end
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="end">End</param>
        public Line2(Vector2 start, Vector2 end)
        {
            if (Math2.Approximately(start, end))
                throw new ArgumentException($"start is approximately end - that's a point, not a line. start={start}, end={end}");

            Start = start;
            End = end;
            Delta = End - Start;
            Centroid = Start + (Delta * 0.5f);

            Axis = Delta.normalized;
            Normal = Math2.Perpendicular(Delta).normalized;
            MagnitudeSquared = Delta.SqrMagnitude();
            Length = Mathf.Sqrt(MagnitudeSquared);

            MinX = Math.Min(Start.x, End.x);
            MinY = Math.Min(Start.y, End.y);
            MaxX = Math.Max(Start.x, End.x);
            MaxY = Math.Max(Start.y, End.y);
            Horizontal = Math.Abs(End.y - Start.y) <= Math2.DEFAULT_EPSILON;
            Vertical = Math.Abs(End.x - Start.x) <= Math2.DEFAULT_EPSILON;

            if (Vertical)
                Slope = float.PositiveInfinity;
            else
                Slope = (End.y - Start.y) / (End.x - Start.x);

            if (Vertical)
                YIntercept = float.NaN;
            else
            {
                // y = mx + b
                // Start.y = Slope * Start.x + b
                // b = Start.y - Slope * Start.x
                YIntercept = Start.y - Slope * Start.x;
            }
        }

        /// <summary>
        /// Determines if the two lines are parallel. Shifting lines will not
        /// effect the result.
        /// </summary>
        /// <param name="line1">The first line</param>
        /// <param name="line2">The second line</param>
        /// <returns>True if the lines are parallel, false otherwise</returns>
        public static bool Parallel(Line2 line1, Line2 line2)
        {
            return (
                Math2.Approximately(line1.Axis, line2.Axis)
                || Math2.Approximately(line1.Axis, -line2.Axis)
                );
        }

        /// <summary>
        /// Determines if the given point is along the infinite line described
        /// by the given line shifted the given amount.
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="pos">The shift for the line</param>
        /// <param name="pt">The point</param>
        /// <returns>True if pt is on the infinite line extension of the segment</returns>
        public static bool AlongInfiniteLine(Line2 line, Vector2 pos, Vector2 pt)
        {
            float normalPart = Vector2.Dot(pt - pos - line.Start, line.Normal);
            return Math2.Approximately(normalPart, 0);
        }

        /// <summary>
        /// Determines if the given line contains the given point.
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <param name="pos">The offset for the line</param>
        /// <param name="pt">The point to check</param>
        /// <returns>True if pt is on the line, false otherwise</returns>
        public static bool Contains(Line2 line, Vector2 pos, Vector2 pt)
        {
            // The horizontal/vertical checks are not required but are
            // very fast to calculate and short-circuit the common case
            // (false) very quickly
            if(line.Horizontal)
            {
                return Math2.Approximately(line.Start.y + pos.y, pt.y)
                    && AxisAlignedLine2.Contains(line.MinX, line.MaxX, pt.x - pos.x, false, false);
            }
            if(line.Vertical)
            {
                return Math2.Approximately(line.Start.x + pos.x, pt.x)
                    && AxisAlignedLine2.Contains(line.MinY, line.MaxY, pt.y - pos.y, false, false);
            }

            // Our line is not necessarily a linear space, but if we shift
            // our line to the origin and adjust the point correspondingly
            // then we have a linear space and the problem remains the same.

            // Our line at the origin is just the infinite line with slope
            // Axis. We can form an orthonormal basis of R2 as (Axis, Normal).
            // Hence we can write pt = line_part * Axis + normal_part * Normal. 
            // where line_part and normal_part are floats. If the normal_part
            // is 0, then pt = line_part * Axis, hence the point is on the
            // infinite line.

            // Since we are working with an orthonormal basis, we can find
            // components with dot products.

            // To check the finite line, we consider the start of the line
            // the origin. Then the end of the line is line.Magnitude * line.Axis.

            Vector2 lineStart = pos + line.Start;

            float normalPart = Math2.DotProduct(pt - lineStart, line.Normal);
            if (!Math2.Approximately(normalPart, 0))
                return false;

            float axisPart = Math2.DotProduct(pt - lineStart, line.Axis);
            return axisPart > -Math2.DEFAULT_EPSILON 
                && axisPart < line.Length + Math2.DEFAULT_EPSILON;
        }

        public Vector2 ClosestEndPoint(Vector2 pt)
        {
            // TODO: optimize this
            var startDist = Vector2.Distance(Start, pt);
            var endDist = Vector2.Distance(End, pt);
            
            if (startDist < endDist)
                return Start;
            else
                return End;
        }

        public Vector2 FurthestEndPoint(Vector2 pt)
        {
            // TODO: optimize this
            var startDist = Vector2.Distance(Start, pt);
            var endDist = Vector2.Distance(End, pt);

            if (startDist > endDist)
                return Start;
            else
                return End;
        }

        public override string ToString()
        {
            return $"[{Point2.ToString(Start)}, {Point2.ToString(End)}]";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Above(Line2 other)
        {
            if (this.MaxY <= other.MaxY)
                return false;
            if (this.MinY <= other.MinY)
                return false;
            
            return true;
        }

        /// <summary>
        /// Split line in half, returning array of both halves
        /// </summary>
        /// <returns>Array of segments</returns>
        public List<Line2> Split()
        {
            return SplitIntoNSegments(2);
        }

        /// <summary>
        /// Returns the line split into segments that are shorter than targetLength (or split into maxSegments pieces)
        /// </summary>
        /// <param name="targetLength">The maximum segment length being targeted</param>
        /// <param name="maxSegments">The upper bound of line segments to be returned (overrides targetLength)</param>
        /// <returns></returns>
        public List<Line2> Split(float targetLength = 1.0f, int maxSegments = 100)
        {

            // first check if the link is already short enough
            if (this.Length < targetLength)
            {
                // The cirrent line is already shorter than 
                // the target legnth, no splitting necessary
                return new List<Line2>{ this };
            }

            var segCount = 1;
            var targetLengthSqr = targetLength * targetLength;
            while (++segCount < maxSegments)
            {
                var ratio = 1f / segCount;
                var offset = Delta * ratio;
                var delta = (Start + offset) - Start;
                var magnitude = delta.SqrMagnitude();

                if (magnitude < targetLengthSqr)
                {
                    // If the magnitude difference is negative, the 
                    // segment length is shorter than taget length. 
                    // The segment should be split {segmentCount} times.
                    break;
                }
            }

            return SplitIntoNSegments(segCount);
        }

        /// <summary>
        /// Split line into N equal segments
        /// </summary>
        /// <param name="segmentCount">
        /// The number of segments to return
        /// </param>
        /// <returns>
        /// Array of segments
        /// </returns>
        public List<Line2> SplitIntoNSegments(int segmentCount = 2)
        {
            var segments = new List<Line2>();

            Vector2 currStart = Start;
            Vector2 splitOffset = Delta * (1.0f / segmentCount);

            for (int i = 1; i < segmentCount; ++i)
            {
                Vector2 splitPoint = currStart + splitOffset;
                segments.Add(new Line2(currStart, splitPoint));
                currStart = splitPoint;
            }

            segments.Add(new Line2(currStart, End));

            return segments;
        }

        /// <summary>
        /// Gets the point to line distance. This works, but doesn't respect the nodes yet. 
        /// The current implementation assumes the line is infinite.
        /// TODO: prevent this from getting distance to infinite line.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float Distance(Vector2 point)
        {
            var lineP1 = this.Start;
            var lineP2 = this.End;
            
            // The numerator is twice the area of the 
            // triangle with its vertices at the three 
            // points: {x0, y0}, {lineP1} and {lineP2}.
            var numerator = Mathf.Abs(
                ((lineP2.y - lineP1.y) * point.x) - 
                ((lineP2.x - lineP1.x) * point.y) + 
                (lineP2.x * lineP1.y) - 
                (lineP2.y * lineP1.x)
            );
            
            // The denominator of this expression 
            // is the distance between P1 and P2. 
            var denominator = Mathf.Sqrt(
                Mathf.Pow(lineP2.y - lineP1.y, 2) + 
                Mathf.Pow(lineP2.x - lineP1.x, 2)
            );

            // calculate the perpendicular distance to 
            // this line as if it was infinitely long.
            float pointToLineDist = numerator / denominator;

            // calculate the distance to each end node
            // since the above calculation ignores them
            float startPointDist = Vector2.Distance(point, Start);
            float endPointDist = Vector2.Distance(point, End);

            // see if the perpendicular dist is less
            // than the distance to either end point
            if (pointToLineDist < startPointDist && 
                pointToLineDist < endPointDist)
            {
                // if the perpendicular line distance is less than the distance to 
                // either of the end points, then we know the perpendicular calculation 
                // landed on a location of the line that extends beyond the end points.
                // When this is detected the distance to the closest end point is returned.
                pointToLineDist = Mathf.Min(startPointDist, endPointDist);
            }

            return pointToLineDist;
        }
    }
}
