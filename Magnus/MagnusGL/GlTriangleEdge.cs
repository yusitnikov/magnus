namespace Magnus.MagnusGL
{
    class GlTriangleEdge
    {
        public GlNormalizedVertex V1, V2;
        public int Hash;

        public GlTriangleEdge(GlNormalizedVertex v1, GlNormalizedVertex v2)
        {
            V1 = v1;
            V2 = v2;
            Hash = v1.Vertex.Index << 16 | v2.Vertex.Index;
        }
    }
}
