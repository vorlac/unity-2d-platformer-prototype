using System;

namespace Core.Spatial
{
    [Flags]
    public enum SpatialRelation
    {
        None       = 0x000,
        Contains   = 0x001, // Feature A completely encloses feature B
        Within     = 0x002, // Feature B completely encloses feature A
        Intersects = 0x004, // Any part of feature A comes into contact with any part of feature B
        Overlaps   = 0x008, // The interior of feature A partly covers feature B AND one does not fully comtain the other.
        Touches    = 0x010, // A part of feature A comes into contact with feature B but their interiors don't intersect

        Left       = 0x020, // Every point in Feature A is to the left of Feature B
        Right      = 0x040, // Every point in Feature A is to the right of Feature B
        Above      = 0x080, // Every point in Feature A is above Feature B
        Below      = 0x100  // Every point in Feature A is below Feature B
    }
}

