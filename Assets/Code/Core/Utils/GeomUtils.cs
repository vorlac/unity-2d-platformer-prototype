using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    using Core.Geom;

    static class Geom
    {
        /// <summary>
        /// A dictionary containing the circle shapes.
        /// </summary>
        private static readonly Dictionary<Tuple<float, float, float, float>, Polygon2> CircleCache = 
            new Dictionary<Tuple<float, float, float, float>, Polygon2>();

        /// <summary>
        /// A dictionary containing the rectangle shapes.
        /// </summary>
        private static readonly Dictionary<Tuple<float, float, float, float>, Polygon2> RectangleCache = 
            new Dictionary<Tuple<float, float, float, float>, Polygon2>();

        /// <summary>
        /// A dictionary containing the convex polygon shapes.
        /// </summary>
        private static readonly Dictionary<int, Polygon2> ConvexPolygonCache = 
            new Dictionary<int, Polygon2>();

        /// <summary>
        /// Fetches the convex polygon (the smallest possible polygon containing 
        /// all the non-transparent pixels) of the given texture.
        /// </summary>
        /// <param name="Texture">The texture.</param>
        public static Polygon2 CreateConvexPolygon(Texture2D Texture)
        {
            var Key = Texture.GetHashCode();

            if (ConvexPolygonCache.ContainsKey(Key))
                return ConvexPolygonCache[Key];

            var uints = Texture.GetRawTextureData<uint>();

            var Points = new List<Vector2>();

            for (var i = 0; i < Texture.width; i++)
                for (var j = 0; j < Texture.height; j++)
                    if (uints[j * Texture.width + i] != 0)
                        Points.Add(new Vector2(i, j));

            if (Points.Count <= 2)
                throw new Exception("Can not create a convex hull from a line.");

            int n = Points.Count, k = 0;
            var h = new List<Vector2>(
                new Vector2[2 * n]
            );

            Points.Sort(
                (a, b) =>
                a.x == b.x ?
                     a.y.CompareTo(b.y)
                : (a.x > b.x ? 1 : -1)
             );

            for (var i = 0; i < n; ++i)
            {
                while (k >= 2 && CrossProduct(h[k - 2], h[k - 1], Points[i]) <= 0)
                    k--;
                h[k++] = Points[i];
            }

            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && CrossProduct(h[k - 2], h[k - 1], Points[i]) <= 0)
                    k--;
                h[k++] = Points[i];
            }

            Points = h.Take(k - 1).ToList();
            return ConvexPolygonCache[Key] = new Polygon2(Points.ToArray());
        }

        /// <summary>
        /// Returns the cross product of the given three vectors.
        /// </summary>
        /// <param name="v1">Vector 1.</param>
        /// <param name="v2">Vector 2.</param>
        /// <param name="v3">Vector 3.</param>
        /// <returns></returns>
        private static double CrossProduct(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return (v2.x - v1.x) * (v3.y - v1.y) - 
                   (v2.y - v1.y) * (v3.x - v1.x);
        }

        /// <summary>
        /// Fetches a rectangle shape with the given width, height, x and y center.
        /// </summary>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="x">The X center of the rectangle.</param>
        /// <param name="y">The Y center of the rectangle.</param>
        /// <returns>A rectangle shape with the given width, height, x and y center.</returns>
        public static Polygon2 CreateRectangle(float width, float height, float x = 0, float y = 0)
        {
            var Key = new Tuple<float, float, float, float>(width, height, x, y);

            if (RectangleCache.ContainsKey(Key))
                return RectangleCache[Key];

            return RectangleCache[Key] = new Polygon2(new[] {
                 new Vector2(x, y),
                 new Vector2(x + width, y),
                 new Vector2(x + width, y + height),
                 new Vector2(x, y + height)
            });
        }

        /// <summary>
        /// Fetches a circle shape with the given radius, center, and segments. Because of the discretization
        /// of the circle, it is not possible to perfectly get the AABB to match both the radius and the position.
        /// This will match the position.
        /// </summary>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="x">The X center of the circle.</param>
        /// <param name="y">The Y center of the circle.</param>
        /// <param name="segments">The amount of segments (more segments equals higher detailed circle)</param>
        /// <returns>A circle with the given radius, center, and segments, as a polygon2 shape.</returns>
        public static Polygon2 CreateCircle(float radius, float x = 0, float y = 0, int segments = 32)
        {
            var Key = new Tuple<float, float, float, float>(radius, x, y, segments);

            if (CircleCache.ContainsKey(Key))
                return CircleCache[Key];

            var Center = new Vector2(radius + x, radius + y);
            var increment = (Math.PI * 2.0) / segments;
            var theta = 0.0;
            var verts = new List<Vector2>(segments);

            Vector2 correction = new Vector2(radius, radius);
            for (var i = 0; i < segments; i++)
            {
                Vector2 vert = radius * new Vector2(
                    (float)Math.Cos(theta),
                    (float)Math.Sin(theta)
                );

                if (vert.x < correction.x)
                    correction.x = vert.x;
                if (vert.y < correction.y)
                    correction.y = vert.y;

                verts.Add(Center + vert);
                theta += increment;
            }

            correction.x += radius;
            correction.y += radius;

            for (var i = 0; i < segments; i++)
            {
                verts[i] -= correction;
            }

            return CircleCache[Key] = new Polygon2(verts.ToArray());
        }

        /// <summary>
        /// Convert degrees to radians. There are 2 PI radians in 360 degrees.  
        /// This assumes that the degrees passed in are between 0 and 360.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static float DegreesToRadians(float degrees)
        {
            return (degrees * Mathf.PI) / 180.0f;
        }

        /// <summary>
        /// Returns the percentage along the line from ptA to ptB 
        /// where a perpendicular from srcPnt to the line intersects. 
        /// A return value between 0 & 1 means the perpendicular 
        /// falls between ptA and ptB.
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="srcPnt"></param>
        /// <returns></returns>
        public static float LinePointIntersectionRatio(Vector2 ptA, Vector2 ptB, Vector2 srcPnt)
        {
            float ratio = 0.0f;

            // Get the difference between the two points
            Vector2 deltaAB = ptB - ptA;

            // If A and B are equal then there is no line
            if (deltaAB.x != 0 || deltaAB.y != 0)
            {
                // Get the ratio along the line
                Vector2 deltaSA = srcPnt - ptA;
                ratio = Math2.DotProduct(deltaAB, deltaSA)
                      / Math2.DotProduct(deltaAB, deltaAB);
            }

	        return ratio;
        }

        /// <summary>
        /// Calculate the point of intersection between a point and a line.
        /// If the projection would go off the line, the projected point is
        /// one of the endpoints. Puts the intersection point in "destPnt".
        /// Returns the "t" value.
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="srcPnt"></param>
        /// <param name="destPnt"></param>
        public static float ProjectPointToLineSegment(Vector2 ptA, Vector2 ptB, Vector2 srcPnt, out Vector2 destPnt)
        {
            destPnt = Vector2.positiveInfinity;

            // Get the percentage along the line described by A and 
            // B where the source intersects at a perpendicular
            float t = LinePointIntersectionRatio(ptA, ptB, srcPnt);
            if (t >= 0 && t <= 1.0)
            {
                // A ratio between 0 and 1 means that a perpendicular was found
                destPnt = (ptA + (ptB - ptA) * t);
            }
            else if (0.0 > t)
            {
                // Otherwise, see if the point is outside the A point.  
                // If it is then peg the point to the A point.
                destPnt = ptA;
            }
            else if (t > 1.0)
            {
                // Or outside the B point.   
                // If it is then peg the point to the B point.
                destPnt = ptB;
            }

	        return t;
        }

        public static float DistanceToLineSquared(Vector2 ptA, Vector2 ptB, Vector2 srcPnt, out Vector2 destPnt, out float tRatio)
        {
	        tRatio = ProjectPointToLineSegment(ptA, ptB, srcPnt, out destPnt);

            return (destPnt.y - srcPnt.y) * (destPnt.y - srcPnt.y);
        }

        /// <summary>
        /// The addtional parameter ratio provides a percentage estimate of how far along
        /// the polyline the point was projected.  (0.0 implies the point was projected 
        /// onto a line extending back from the beginning of the polyline; a ratio of 
        /// 1.0 implies that the point was projected onto a line extending out from the 
        /// end of the polyline)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="srcPnt"></param>
        /// <param name="destPnt"></param>
        /// <param name="ratio"></param>
        /// <param name="rOffsetMeters"></param>
        /// <returns></returns>
        public static float ProjectPointToLinestring(Line2 line, Vector2 srcPnt,
            out Vector2 destPnt, out float ratio, out int rOffsetMeters)
        {
            List<Vector2> pts = new List<Vector2>();
            pts.Add(line.Start);
            pts.Add(line.End);

            return ProjectPointToLinestring(pts, srcPnt, out destPnt, out ratio, out rOffsetMeters);
        }

        /// <summary>
        /// The addtional parameter ratio provides a percentage estimate of how far along
        /// the polyline the point was projected.  (0.0 implies the point was projected 
        /// onto a line extending back from the beginning of the polyline; a ratio of 
        /// 1.0 implies that the point was projected onto a line extending out from the 
        /// end of the polyline)
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="srcPnt"></param>
        /// <param name="destPnt"></param>
        /// <param name="ratio"></param>
        /// <param name="rOffsetMeters"></param>
        /// <returns>
        /// Distance between point and linestring
        /// </returns>
        public static float ProjectPointToLinestring(List<Vector2> pts, Vector2 srcPnt, 
            out Vector2 destPnt, out float ratio, out int rOffsetMeters)
        {
            // Find the point that is on the polyline described by the given
            // points when a perpendicular is dropped to the polyline.
            Vector2 curPt = Vector2.zero;
            Vector2 retPt = Vector2.zero;

            float curMag = 0f;
            float curDist = 0f;
            float cumDist = 0f;
            float pctOffset = 0f;
            float dDist = 0f;
            float t_ratio = 0f;
            float retMag = float.PositiveInfinity;
            int distClosest = int.MaxValue;
            int distOffset = 0;

            // Loop through all the points and find the nearest one.
            for (int i = 0; i < pts.Count - 1; ++i)
	        {
		        curMag = DistanceToLineSquared(pts[i], pts[i+1], srcPnt, out curPt, out t_ratio);
                dDist = Vector2.Distance(pts[i], pts[i + 1]);

		        // Now update 'closest' segment info.
		        if (curMag < retMag)
		        {
			        retMag = curMag;
			        retPt = curPt;

			        if (t_ratio > 1.0)
			        {
				        pctOffset = t_ratio - 1.0f;
				        curDist = cumDist + dDist;
				        t_ratio = 1.0f;
                    }
                    else if (t_ratio< 0.0)
			        {
				        pctOffset = 0.0f - t_ratio;
				        curDist = cumDist;
				        t_ratio = 0.0f;
                    }
                    else
			        {
				        pctOffset = 0.0f;
				        curDist = cumDist + (dDist * t_ratio);
			        }

                    // Store the closest perpendicular.  If the source point is
                    // off the end of the line then we want the distance from the
                    // end of the line to the perpendicular where it would intersect
                    // if the line was extended.
                    distOffset = Convert.ToInt32(pctOffset * dDist);
			        if (distOffset < distClosest)
                        distClosest = distOffset;
		        }

		        // Don't update the cumulative distance until here, it's used above.
		        cumDist += dDist;
	        }

	        // Return the percentage and the distance along the link.
	        if (0.0 != cumDist)
	        {
		        if (curDist >= cumDist)
			        ratio = 1.0f;
		        else if (curDist == 0 )
			        ratio = 0;
		        else
			        ratio = curDist/cumDist;

		        rOffsetMeters = distClosest;
	        }
	        else
	        {
		        ratio = 0.0f;
		        rOffsetMeters = 0;
	        }

	        destPnt = new Vector2(retPt.x, retPt.y);

	        return Mathf.Sqrt(retMag);
        }
    }
}