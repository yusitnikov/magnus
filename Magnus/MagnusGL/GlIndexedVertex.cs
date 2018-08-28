namespace Magnus.MagnusGL
{
    class GlIndexedVertex
    {
        public DoublePoint3D Position;
        public int Index;

        public GlIndexedVertex(DoublePoint3D position, int index)
        {
            Position = position;
            Index = index;
        }
    }
}
