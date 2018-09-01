using Mathematics.Math3D;

namespace Magnus.MagnusGL
{
    class GlIndexedVertex
    {
        public Point3D Position;
        public int Index;

        public GlIndexedVertex(Point3D position, int index)
        {
            Position = position;
            Index = index;
        }
    }
}
