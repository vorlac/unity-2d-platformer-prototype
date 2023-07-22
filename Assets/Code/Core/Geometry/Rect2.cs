using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Geom
{
    using Core.Spatial;
    
    /// <summary>
    /// Represents an axis aligned rectangle.
    /// 
    /// Example Rect (expected axis directions):
    ///    (0,1)       (1,1)
    ///      o-----------o
    ///      |           |
    ///      |           |
    ///      |           |
    ///      o-----------o
    ///    (0,0)       (1,0)
    /// </summary>

    public struct Rect2 : ISpatial
    {
        public static readonly Rect2 Empty = new Rect2();

        public enum Location
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center,
            LeftCenter,
            RightCenter,
            TopCenter,
            BottomCenter,
        }

        public enum Face
        {
            Top,
            Bottom,
            Left,
            Right
        }

        public struct Size
        {
            public Size(float width, float height)
            {
                this.Width = width;
                this.Height = height;
            }

            public float Width 
            { 
                get; 
                set; 
            }
            
            public float Height 
            { 
                get; 
                set; 
            }

            public static implicit operator Vector2(Size rhs)
            {
                return new Vector2(rhs.Width, rhs.Height);
            }

            public static implicit operator Vector3(Size rhs)
            {
                return new Vector3(rhs.Width, rhs.Height);
            }
        }

        public Rect2(Rect2 other)
        {
            this.X = other.X;
            this.Y = other.Y;
            this.Width = other.Width;
            this.Height = other.Height;
            this.Normalize();
        }

        public Rect2(Vector2 location, Size size)
        {
            this.X = location.x;
            this.Y = location.y;
            this.Width = size.Width;
            this.Height = size.Height;
            this.Normalize();
        }

        public Rect2(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Normalize();
        }

        public Rect2(Vector2 ptA, Vector2 ptB)
        {
            float left = Math.Min(ptA.x, ptB.x);
            float right = Math.Max(ptA.x, ptB.x);
            float top = Math.Max(ptA.y, ptB.y);
            float bottom = Math.Min(ptA.y, ptB.y);

            this.X = left;
            this.Y = top;
            this.Width = right - left;
            this.Height = top - bottom;

            this.Normalize();
        }

        public Rect2(List<Vector2> points)
        {
            this = Rect2.Empty;
            foreach (var pt in points)
            {
                this.ExpandTo(pt);
            }
            this.Normalize();
        }

        public Rect2 Normalize()
        {
            if (Width < 0)
            {
                this.X += this.Width;
                this.Width = -this.Width;
            }

            if (Height < 0)
            {
                this.Y -= this.Height;
                this.Height = -this.Height;
            }

            return this;
        }

        public Rect2 Normalized()
        {
            Rect2 ret = new Rect2(this);
            ret.Normalize();
            return ret;
        }

        public static Rect2 FromLTRB(float left, float top, float right, float bottom)
        {
            // first make sure the proper values are assigned to each.
            // if some end up being backwards, just reassign them to 
            // ensure the rectangle is created properly and normalized
            float l = Math.Min(left, right);
            float r = Math.Max(left, right);
            float b = Math.Min(top, bottom);
            float t = Math.Max(top, bottom);

            float width = r - l;
            float height = b - t;

            return new Rect2(l, t, width, height);
        }

        public static implicit operator UnityEngine.Rect(Rect2 rhs)
        {
            return rhs.AsUnityRect();
        }

        public static implicit operator Rect2(UnityEngine.Bounds rhs)
        {
            return Rect2.FromBounds(rhs);
        }

        public UnityEngine.Rect AsUnityRect()
        {
            return new UnityEngine.Rect(X, Y, Width, Height);
        }

        public static Rect2 FromBounds(UnityEngine.Bounds bounds)
        {
            UnityEngine.Vector3 center = bounds.center;
            UnityEngine.Vector3 extent = bounds.extents;

            float top = center.y + extent.y;
            float bottom = center.y - extent.y;
            float left = center.x - extent.x;
            float right = center.x + extent.x;
            float width = right - left;
            float height = top - bottom;

            return new Rect2(left, top, width, height);
        }

        public Rect2 Clear()
        {
            this = Rect2.Empty;
            return this;
        }

        public override int GetHashCode()
        {
            return (int) (
                  (UInt32) X ^
                (((UInt32) Y      << 13) | ((UInt32) Y      >> 19)) ^
                (((UInt32) Width  << 26) | ((UInt32) Width  >>  6)) ^
                (((UInt32) Height <<  7) | ((UInt32) Height >> 25))
            );
        }

        public override string ToString()
        {
            return $"[TL=({X},{Y}) W={Width} H={Height}]";
        }

        public Vector2 TopLeft
        {
            get
            {
                return new Vector2(X, Y);
            }
            private set
            {
                X = value.x;
                Y = value.y;
            }
        }

        public Size Dimensions
        {
            get
            {
                return new Size(Width, Height);
            }
            private set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        public float X 
        { 
            get;
            private set; 
        }

        public float Y 
        { 
            get;
            private set; 
        }

        public float Width 
        { 
            get;
            private set; 
        }

        public float Height
        {
            get;
            private set;
        }

        public float Left
        {
            get => X;
        }

        public float Top
        {
            get => Y;
        }

        public float Right
        {
            get => X + Width;
        }

        public float Bottom
        {
            get => Y - Height;
        }

        public Vector2 Center
        {
            get => GetPoint(Location.Center);
        }

        public float Area
        {
            get => Width * Height;
        }

        public float Perimeter
        {
            get => (2 * Width) + (2 * Height);
        }

        public bool IsEmpty
        {
            get => Width == 0 &&
                   Height == 0 &&
                   X == 0 && Y == 0;
        }

        public Vector2 Min
        {
            get => GetPoint(Location.BottomLeft);
        }

        public Vector2 Max
        {
            get => GetPoint(Location.TopRight);
        }

        public Vector2 UpperLeft
        {
            get => GetPoint(Location.TopLeft);
        }

        public Vector2 UpperRight
        {
            get => GetPoint(Location.TopRight);
        }

        public Vector2 LowerLeft
        {
            get => GetPoint(Location.BottomLeft);
        }

        public Vector2 LowerRight
        {
            get => GetPoint(Location.BottomRight);
        }

        // implementing IGeometry
        public Rect2 BoundingBox 
        {
            get => this;
        }

        public override bool Equals(object obj)
        {
            if (obj is Rect2)
            {
                Rect2 comp = (Rect2) obj;
                return comp.X == this.X
                    && comp.Y == this.Y
                    && comp.Width == this.Width
                    && comp.Height == this.Height;
            }

            return false;
        }

        public static bool operator ==(Rect2 left, Rect2 right)
        {
            return left.X == right.X
                && left.Y == right.Y
                && left.Width == right.Width
                && left.Height == right.Height;
        }

        public static bool operator !=(Rect2 left, Rect2 right)
        {
            return !(left == right);
        }


        public float AxisMinimum(Axis axis)
        {
            switch (axis)
            {
                case Axis.Vertical:
                    return Bottom;
                case Axis.Horizontal:
                    return Left;
            }

            return float.PositiveInfinity;
        }

        public float AxisMaximum(Axis axis)
        {
            switch (axis)
            {
                case Axis.Vertical:
                    return Top;
                case Axis.Horizontal:
                    return Right;
            }

            return float.PositiveInfinity;
        }

        public float Distance(Vector3 pt)
        {
            var ldist = Math.Abs(pt.x - this.Left);
            var rdist = Math.Abs(pt.x - this.Right);
            var tdist = Math.Abs(pt.y - this.Top);
            var bdist = Math.Abs(pt.y - this.Bottom);

            var closestX = rdist < ldist ? rdist : ldist;
            var closestY = tdist < bdist ? tdist : bdist;

            return Vector3.Distance(pt, new Vector3 { x = closestX, y = closestY });
        }

        public bool Contains(float x, float y)
        {
            return x <= this.Right
                && x >= this.Left
                && y <= this.Top
                && y >= this.Bottom;
        }

        public bool Contains(Vector2 pt)
        {
            return Contains(pt.x, pt.y);
        }

        public static bool Contains(Rect2 box, Vector2 pos, Vector2 point, bool strict)
        {
            return AxisAlignedLine2.Contains(box.Min.x + pos.x, box.Max.x + pos.x, point.x, strict, false)
                && AxisAlignedLine2.Contains(box.Min.y + pos.y, box.Max.y + pos.y, point.y, strict, false);
        }

        public bool Contains(Rect2 rect)
        {
            if (rect.Right > this.Right)
                return false;
            if (rect.Left < this.Left)
                return false;
            if (rect.Top > this.Top)
                return false;
            if (rect.Bottom < this.Bottom)
                return false;
            return true;
        }

        public static bool RotatedContains(Rect2 rect, float rectAngle, Vector2 point)
        {
            // rotate around rectangle center by -rectAngle
            var s = Mathf.Sin(-rectAngle);
            var c = Mathf.Cos(-rectAngle);

            // set origin to rect center
            var newPoint = point - rect.Center;

            // rotate
            newPoint = new Vector2(newPoint.x * c - newPoint.y * s, 
                                  newPoint.x * s + newPoint.y * c);
            
            // put origin back
            newPoint = newPoint + rect.Center;

            // check if the transformed point is in the rectangle, 
            // which is no longer rotated relative to the point
            return rect.Contains(newPoint);
        }

        public Rect2 Inflate(float width, float height)
        {
            this.X -= width;
            this.Y += height;
            this.Width += 2 * width;
            this.Height += 2 * height;
            return this;
        }

        public Rect2 Inflate(Size size)
        {
            return Inflate(size.Width, size.Height);
        }

        public static Rect2 Inflated(Rect2 rect, float width, float height)
        {
            Rect2 r = rect;
            r.Inflate(width, height);
            return r;
        }

        public Rect2 Intersect(Rect2 rect)
        {
            Rect2 result = Intersection(rect, this);

            this.X = result.X;
            this.Y = result.Y;
            this.Width = result.Width;
            this.Height = result.Height;

            return this;
        }

        public static Rect2 Intersection(Rect2 a, Rect2 b)
        {
            float right = Math.Min(a.Right, b.Right);
            float left = Math.Max(a.Left, b.Left);
            float top = Math.Min(a.Top, b.Top);
            float bottom = Math.Max(a.Bottom, b.Bottom);

            System.Diagnostics.Debug.Assert(right >= left && bottom >= top);

            if (right >= left && bottom >= top)
            {
                return new Rect2(left, top, right - left, top - bottom);
            }

            return Rect2.Empty;
        }

        public bool IntersectsWith(Rect2 rect)
        {
            return rect.Right > this.Left
                && rect.Left < this.Right
                && rect.Top > this.Bottom
                && rect.Bottom < this.Top;
        }

        public Rect2 ExpandTo(Rect2 rect)
        {
            // get the union of this rect with the inputted rect paramater
            Rect2 union = Rect2.Merge(rect, this);

            // assign the new union result to this polygon
            this.X = union.X;
            this.Y = union.Y;
            this.Width = union.Width;
            this.Height = union.Height;

            return this;
        }

        public static Rect2 Merge(Rect2 a, Rect2 b)
        {
            if (a.IsEmpty)
                return new Rect2(b);
            else if (b.IsEmpty)
                return new Rect2(a);
            else
            {
                float top = Math.Max(a.Top, b.Top);
                float right = Math.Max(a.Right, b.Right);
                float bottom = Math.Min(a.Bottom, b.Bottom);
                float left = Math.Min(a.Left, b.Left);

                return new Rect2(left, top, right - left, top - bottom);
            }
        }

        public static Rect2 Merge(Rect2 rect, Vector2 point)
        {
            Rect2 ret = new Rect2(rect);
            ret.ExpandTo(point);
            return ret;
        }

        public Rect2 ExpandTo(Vector2 pt)
        {
            float left = Math.Min(Left, pt.x);
            float right = Math.Max(Right, pt.x);
            float top = Math.Max(Top, pt.y);
            float bottom = Math.Min(Bottom, pt.y);

            this.X = left;
            this.Y = top;
            this.Width = right - left;
            this.Height = top - bottom;

            return this;
        }

        public float MergeEnlargement(Rect2 rect)
        {
            // first get the union of the two rectangles
            Rect2 result = Rect2.Merge(rect, this);

            // return the difference in area between this 
            // rectangle and the result of the union operation
            return Math.Abs(result.Area - this.Area);

        }

        public Rect2 Offset(float x, float y)
        {
            this.X += x;
            this.Y += y;
            return this;
        }

        public Rect2 Offset(Vector2 position)
        {
            return this.Offset(position.x, position.y);
        }

        static public Rect2 Offset(Rect2 rect, float x, float y)
        {
            Rect2 ret = new Rect2(rect);
            return ret.Offset(x, y);
        }

        static public Rect2 Offset(Rect2 rect, Vector2 position)
        {
            Rect2 ret = new Rect2(rect);
            ret.Offset(position.x, position.y);
            return ret;
        }

        public Vector2 GetPoint(Location location = Location.TopLeft)
        {
            switch (location)
            {
                case Location.TopLeft:
                    return new Vector2(Left, Top);
                case Location.TopRight:
                    return new Vector2(Right, Top);
                case Location.BottomLeft:
                    return new Vector2(Left, Bottom);
                case Location.BottomRight:
                    return new Vector2(Right, Bottom);
                case Location.Center:
                    return new Vector2(Left + (Width * 0.5f), Top - (Height * 0.5f));
                case Location.LeftCenter:
                    return new Vector2(Left, Top + (Height * 0.5f));
                case Location.RightCenter:
                    return new Vector2(Right, Top + (Height * 0.5f));
                case Location.TopCenter:
                    return new Vector2(Left + (Width * 0.5f), Top);
                case Location.BottomCenter:
                    return new Vector2(Left + (Width * 0.5f), Bottom);
                default:
                    throw new Exception("Rectangle::GetPoint - Invalid Location");
            }
        }

        public Rect2 SetLocation(float x, float y, Location location = Location.TopLeft)
        {
            switch (location)
            {
                case Location.TopLeft:
                    this.X = x;
                    this.Y = y;
                    break;
                case Location.TopRight:
                    this.X = x - Width;
                    this.Y = y;
                    break;
                case Location.BottomLeft:
                    this.X = x;
                    this.Y = y + Height;
                    break;
                case Location.BottomRight:
                    this.X = x - Width;
                    this.Y = y + Height;
                    break;
                case Location.Center:
                    this.X = x - (Width * 0.5f);
                    this.Y = y + (Height * 0.5f);
                    break;
                case Location.LeftCenter:
                    this.X = x;
                    this.Y = y + (Height * 0.5f);
                    break;
                case Location.RightCenter:
                    this.X = x - Width;
                    this.Y = y + (Height * 0.5f);
                    break;
                case Location.TopCenter:
                    this.X = x - (Width * 0.5f);
                    this.Y = y;
                    break;
                case Location.BottomCenter:
                    this.X = x - (Width * 0.5f);
                    this.Y = y + Height;
                    break;
                default:
                    throw new Exception("Rectangle::Move - Invalid Location");
            }

            this.Normalize();
            return this;
        }

        public Rect2 SetLocation(Vector2 position, Location location = Location.TopLeft)
        {
            return SetLocation(position.x, position.y, location);
        }

        static public Rect2 SetLocation(Rect2 rect, float x, float y, Location location = Location.TopLeft)
        {
            Rect2 ret = new Rect2(rect);
            return ret.SetLocation(x, y, location);
        }

        static public Rect2 SetLocation(Rect2 rect, Vector2 position, Location location = Location.TopLeft)
        {
            Rect2 ret = new Rect2(rect);
            return ret.SetLocation(position.x, position.y, location);
        }

        public bool Above(Vector2 point)
        {
            return this.Bottom > point.y;
        }

        public bool Above(Rect2 rect)
        {
            return this.Bottom > rect.Top;
        }

        public bool Above(Line2 line)
        {
            return this.Bottom > line.MaxY;
        }

        public bool Below(Vector2 point)
        {
            return this.Top < point.y;
        }

        public bool Below(Rect2 rect)
        {
            return this.Top < rect.Bottom;
        }

        public bool Below(Line2 line)
        {
            return this.Top < line.MinY;
        }

        public bool LeftOf(Vector2 point)
        {
            return this.Right < point.x;
        }

        public bool LeftOf(Rect2 rect)
        {
            return this.Right < rect.Left;
        }

        public bool LeftOf(Line2 line)
        {
            return this.Right < line.MinX;
        }

        public bool RightOf(Vector2 point)
        {
            return this.Left > point.x;
        }

        public bool RightOf(Rect2 rect)
        {
            return this.Left > rect.Right;
        }

        public bool RightOf(Line2 line)
        {
            return this.Left > line.MaxX;
        }

        public bool OverlapsOnAxis(Rect2 rect, Axis axis)
        {
            if (axis.HasFlag(Axis.Horizontal))
            {
                if (this.RightOf(rect))
                    return false;
                if (this.LeftOf(rect))
                    return false;
            }

            if (axis.HasFlag(Axis.Vertical))
            {
                if (this.Above(rect))
                    return false;
                if (this.Below(rect))
                    return false;
            }

            return true;
        }

        public bool OverlapsOnAxis(Line2 line, Axis axis)
        {
            if (axis.HasFlag(Axis.Horizontal))
            {
                if (this.RightOf(line))
                    return false;
                if (this.LeftOf(line))
                    return false;
            }

            if (axis.HasFlag(Axis.Vertical))
            {
                if (this.Above(line))
                    return false;
                if (this.Below(line))
                    return false;
            }

            return true;
        }

        public SpatialRelation RelationTo(Rect2 rect)
        {
            SpatialRelation ret = this.RelativeLocation(rect);

            if (this.Contains(rect))
                ret |= SpatialRelation.Contains;
            if (this.IntersectsWith(rect))
                ret |= SpatialRelation.Intersects;

            return ret;
        }

        public SpatialRelation RelativeLocation(Rect2 rect)
        {
            SpatialRelation ret = SpatialRelation.None;
            
            if (this.Above(rect))
                ret |= SpatialRelation.Above;
            if (this.Below(rect))
                ret = SpatialRelation.Below;
            if (this.LeftOf(rect))
                ret |= SpatialRelation.Left;
            if (this.RightOf(rect))
                ret = SpatialRelation.Right;

            return ret;
        }
    }
}
