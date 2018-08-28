namespace Magnus.MagnusGL
{
    class GlNormalizedVertex
    {
        public GlIndexedVertex Vertex;
        public DoublePoint3D Normal;

        public GlNormalizedVertex(GlIndexedVertex vertex, DoublePoint3D normal)
        {
            Vertex = vertex;
            Normal = normal;
        }
    }
}
