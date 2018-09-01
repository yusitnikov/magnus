using Mathematics.Math3D;

namespace Magnus.MagnusGL
{
    class GlNormalizedVertex
    {
        public GlIndexedVertex Vertex;
        public Point3D Normal;

        public GlNormalizedVertex(GlIndexedVertex vertex, Point3D normal)
        {
            Vertex = vertex;
            Normal = normal;
        }
    }
}
