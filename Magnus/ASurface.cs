using Mathematics.Math3D;

namespace Magnus
{
    abstract class ASurface
    {
        public Point3D Position, Speed;

        public abstract Point3D Normal { get; set; }
    }
}
