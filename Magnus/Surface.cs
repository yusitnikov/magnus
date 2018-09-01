using Mathematics.Math3D;

namespace Magnus
{
    class Surface : ASurface
    {
        public static readonly Surface Horizontal = new Surface(Point3D.Empty, Point3D.Empty, Point3D.YAxis);
        public static readonly Surface HorizontalReverted = new Surface(Point3D.Empty, Point3D.Empty, -Point3D.YAxis);

        public override Point3D Normal { get; set; }

        public Surface(Point3D position, Point3D speed, Point3D normal)
        {
            Position = position;
            Speed = speed;
            Normal = normal;
        }
    }
}
