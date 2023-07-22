using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    using Core.Geom;

    static class Object
    {
        /// <summary>
        /// Gets the object's box collider rectangle
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Rect2 GetBoundingRect(GameObject obj)
        {
            var bc = obj.GetComponent<BoxCollider2D>();
            Rect2 rect = (bc != null) ? Rect2.FromBounds(bc.bounds) 
                                      : Rect2.Empty;
            return rect;
        }

        /// <summary>
        /// Gets the top face of the GameObject's box collider.
        /// Only returns a valid Line2 if the top face is also
        /// one of the two long faces of the box collider.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Line2 GetTopFace(GameObject obj)
        {
            return Utils.Object.GetTopBoxColliderLongFace(obj);
        }

        /// <summary>
        /// Converts the object's collider rect (with 
        /// local rotation applied) to world coords
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Vector3[] GetBoxColliderVertices(GameObject obj)
        {

            var bc = obj.GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                Vector3[] vertices = new Vector3[4];
                vertices[0] = obj.transform.TransformPoint(
                    bc.offset + new Vector2(bc.size.x, bc.size.y) * 0.5f);
                vertices[1] = obj.transform.TransformPoint(
                    bc.offset + new Vector2(-bc.size.x, bc.size.y) * 0.5f);
                vertices[2] = obj.transform.TransformPoint(
                    bc.offset + new Vector2(-bc.size.x, -bc.size.y) * 0.5f);
                vertices[3] = obj.transform.TransformPoint(
                    bc.offset + new Vector2(bc.size.x, -bc.size.y) * 0.5f);
                
                return vertices;
            }

            return null;
        }

        /// <summary>
        /// Returns the top face of the obect's box collider rect 
        /// if the top face is also the longer of the two rectangle
        /// sides/dimensions. If the top face happens to be one of 
        /// the shorter sides, this function will return null.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Line2 GetTopBoxColliderLongFace(GameObject obj)
        {
            var points = Utils.Object.GetBoxColliderVertices(obj);
            if (points != null)
            {
                UnityEngine.Debug.Assert(points.Length == 4);
                
                List<Line2> faces = new List<Line2>();
                for (int i = 1; i <= points.Length; ++i)
                {
                    var startIdx = (i - 1) % points.Length;
                    var endIdx = i % points.Length;
                    faces.Add(new Line2(points[startIdx], points[endIdx]));
                }

                var faceA1 = faces[0];
                var faceB1 = faces[1];
                var faceA2 = faces[2];
                var faceB2 = faces[3];

                // Does this matter?
                UnityEngine.Debug.Assert(faceA1.Length != faceB1.Length);

                // TODO: add slope checks?
                if (faceA1.Length > faceB1.Length)
                {
                    UnityEngine.Debug.Assert(Line2.Parallel(faceA1, faceA2));
                    if (!faceA1.Vertical)
                    {
                        if (faceA1.Above(faceA2))
                            return faceA1;
                        else
                            return faceA2;
                    }
                }
                else
                {
                    UnityEngine.Debug.Assert(Line2.Parallel(faceB1, faceB2));
                    if (!faceB1.Vertical)
                    {
                        if (faceB1.Above(faceB2))
                            return faceB1;
                        else
                            return faceB2;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the object's box collider rectangle without any local tranformation applied.
        /// This is accomplished by applying the inverse quarternion to the object's transform, 
        /// then retruning the rectangle in world space.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Rect2 GetBoxColliderRectWithoutLocalRotation(GameObject obj)
        {
            BoxCollider2D bc = obj.GetComponent<BoxCollider2D>();

            if (bc != null)
            {
                var tl = Quaternion.Inverse(bc.transform.rotation) * obj.transform.TransformPoint((Vector3)bc.offset + new Vector3(bc.size.x, bc.size.y) * 0.5f);
                var tr = Quaternion.Inverse(bc.transform.rotation) * obj.transform.TransformPoint((Vector3)bc.offset + new Vector3(-bc.size.x, bc.size.y) * 0.5f);
                var br = Quaternion.Inverse(bc.transform.rotation) * obj.transform.TransformPoint((Vector3)bc.offset + new Vector3(-bc.size.x, -bc.size.y) * 0.5f);
                var bl = Quaternion.Inverse(bc.transform.rotation) * obj.transform.TransformPoint((Vector3)bc.offset + new Vector3(bc.size.x, -bc.size.y) * 0.5f);
                return new Rect2(tl.x, tl.y, tr.x - tl.x, tr.y - br.y);
            }

            return Rect2.Empty;
        }

        /// <summary>
        /// Checks to see if a point intersects an object's box collider rect.
        /// This check will internally handle any local <c>&lt;=&gt;</c> world tranforms
        /// by reversing any local object transforms, then applying the same
        /// transformation to the point before checking for the intersection.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool PointIntersectsObject(GameObject obj, Vector3 point)
        {
            var transformedObjRect = GetBoxColliderRectWithoutLocalRotation(obj);
            if (!transformedObjRect.IsEmpty)
            {
                BoxCollider2D bc = obj.GetComponent<BoxCollider2D>();
                if (bc != null)
                {
                    var inverseObjectRotationQuaternion = Quaternion.Inverse(bc.transform.rotation);
                    var pointInLocalObjectTranformSpace = obj.transform.TransformPoint((Vector3)bc.offset + obj.transform.InverseTransformPoint(point));
                    var transformedPointInObjectSpace = inverseObjectRotationQuaternion * pointInLocalObjectTranformSpace;
                    return transformedObjRect.Contains(transformedPointInObjectSpace);
                }
            }

            return false;
        }

        /// <summary>
        /// Useful utility for GetHashCode object functions by 
        /// shifting and wrapping an integer value by n positions
        /// </summary>
        /// <param name="value"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static int ShiftAndWrap(int value, int positions)
        {
            positions = (positions & 0x1F);

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);

            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);

            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
    }
}
