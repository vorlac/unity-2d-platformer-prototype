﻿using System;
using UnityEngine;

namespace Core.Geom
{
    /// <summary>
    /// Contains utility functions for doing math in two-dimensions that
    /// don't fit elsewhere. Also contains any necessary constants.
    /// </summary>
    public class Math2
    {
        /// <summary>
        /// Default epsilon
        /// </summary>
        public const float DEFAULT_EPSILON = 0.001f;

        /// <summary>
        /// Determines if v1, v2, and v3 are collinear
        /// </summary>
        /// <remarks>
        /// Calculates if the area of the triangle of v1, v2, v3 is less than or equal to epsilon.
        /// </remarks>
        /// <param name="v1">Vector 1</param>
        /// <param name="v2">Vector 2</param>
        /// <param name="v3">Vector 3</param>
        /// <param name="epsilon">How close is close enough</param>
        /// <returns>If v1, v2, v3 is collinear</returns>
        public static bool IsOnLine(Vector2 v1, Vector2 v2, Vector2 v3, float epsilon = DEFAULT_EPSILON)
        {
            return AreaOfTriangle(v1, v2, v3) <= epsilon;
        }

        /// <summary>
        /// Calculates the square of the area of the triangle made up of the specified points.
        /// </summary>
        /// <param name="v1">First point</param>
        /// <param name="v2">Second point</param>
        /// <param name="v3">Third point</param>
        /// <returns>Area of the triangle made up of the given 3 points</returns>
        public static float AreaOfTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return 0.5f * Math.Abs((v2.x - v1.x) * (v3.y - v1.y) - (v3.x - v1.x) * (v2.y - v1.y));
        }

        /// <summary>
        /// Returns a vector that is perpendicular to the specified vector.
        /// </summary>
        /// <returns>A vector perpendicular to v</returns>
        /// <param name="v">Vector</param>
        public static Vector2 Perpendicular(Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }

        /// <summary>
        /// Returns the dot product of (x1, y1) and (x2, y2)
        /// </summary>
        /// <returns>The dot.</returns>
        /// <param name="x1">The first x value.</param>
        /// <param name="y1">The first y value.</param>
        /// <param name="x2">The second x value.</param>
        /// <param name="y2">The second y value.</param>
        public static float DotProduct(float x1, float y1, float x2, float y2)
        {
            return x1 * x2 + y1 * y2;
        }

        /// <summary>
        /// Returns the dot product of v1 and v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float DotProduct(Vector2 v1, Vector2 v2)
        {
            return DotProduct(v1.x, v1.y, v2.x, v2.y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static float DotProduct(Vector2 v, float x2, float y2)
        {
            return DotProduct(v.x, v.y, x2, y2);
        }

        /// <summary>
        /// Returns the cross product of the given three vectors.
        /// </summary>
        /// <param name="v1">Vector 1.</param>
        /// <param name="v2">Vector 2.</param>
        /// <param name="v3">Vector 3.</param>
        /// <returns></returns>
        private static float CrossProduct(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return (v2.x - v1.x) * (v3.y - v1.y) -
                   (v2.y - v1.y) * (v3.x - v1.x);
        }

        /// <summary>
        /// Determines if f1 and f2 are approximately the same.
        /// </summary>
        /// <returns>The approximately.</returns>
        /// <param name="f1">F1.</param>
        /// <param name="f2">F2.</param>
        /// <param name="epsilon">Epsilon.</param>
        public static bool Approximately(float f1, float f2, float epsilon = DEFAULT_EPSILON)
        {
            return Math.Abs(f1 - f2) <= epsilon;
        }

        /// <summary>
        /// Determines if vectors v1 and v2 are approximately equal, such that
        /// both coordinates are within epsilon.
        /// </summary>
        /// <returns>If v1 and v2 are approximately equal.</returns>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <param name="epsilon">Epsilon.</param>
        public static bool Approximately(Vector2 v1, Vector2 v2, float epsilon = DEFAULT_EPSILON)
        {
            return Approximately(v1.x, v2.x, epsilon) && Approximately(v1.y, v2.y, epsilon);
        }

        /// <summary>
        /// Rotates the specified vector about the specified vector a rotation of the
        /// specified amount.
        /// </summary>
        /// <param name="vec">The vector to rotate</param>
        /// <param name="about">The point to rotate vec around</param>
        /// <param name="rotation">The rotation</param>
        /// <returns>The vector vec rotated about about rotation.Theta radians.</returns>
        public static Vector2 Rotate(Vector2 vec, Vector2 about, Rotation2 rotation)
        {
            if (rotation.Theta == 0)
                return vec;
            var tmp = vec - about;
            return new Vector2(tmp.x * rotation.CosTheta - tmp.y * rotation.SinTheta + about.x,
                               tmp.x * rotation.SinTheta + tmp.y * rotation.CosTheta + about.y);
        }

        /// <summary>
        /// Returns either the vector or -vector such that MakeStandardNormal(vec) == MakeStandardNormal(-vec)
        /// </summary>
        /// <param name="vec">The vector</param>
        /// <returns>Normal such that vec.x is positive (unless vec.x is 0, in which such that vec.y is positive)</returns>
        public static Vector2 MakeStandardNormal(Vector2 vec)
        {
            if (vec.x < -DEFAULT_EPSILON)
                return -vec;

            if (Approximately(vec.x, 0) && vec.y < 0)
                return -vec;

            return vec;
        }
    }
}
