
namespace Core.Spatial
{
    using Core.Geom;

    public interface ISpatial
    {
        Rect2 BoundingBox { get; }
    }
}
