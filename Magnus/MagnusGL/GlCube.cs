using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlCube : GlMesh
    {
        public GlCube(Color color, double x1, double y1, double z1, double x2, double y2, double z2, bool withShadow = true)
        {
            GlIndexedVertex
                v0 = new GlIndexedVertex(new DoublePoint3D(x1, y1, z1), nextVertexIndex),
                v1 = new GlIndexedVertex(new DoublePoint3D(x1, y1, z2), nextVertexIndex),
                v2 = new GlIndexedVertex(new DoublePoint3D(x2, y1, z2), nextVertexIndex),
                v3 = new GlIndexedVertex(new DoublePoint3D(x2, y1, z1), nextVertexIndex),
                v4 = new GlIndexedVertex(new DoublePoint3D(x1, y2, z1), nextVertexIndex),
                v5 = new GlIndexedVertex(new DoublePoint3D(x1, y2, z2), nextVertexIndex),
                v6 = new GlIndexedVertex(new DoublePoint3D(x2, y2, z2), nextVertexIndex),
                v7 = new GlIndexedVertex(new DoublePoint3D(x2, y2, z1), nextVertexIndex);

            addPolygon(color, v0, v1, v2, v3);
            addPolygon(color, v0, v4, v5, v1);
            addPolygon(color, v1, v5, v6, v2);
            addPolygon(color, v2, v6, v7, v3);
            addPolygon(color, v3, v7, v4, v0);
            addPolygon(color, v7, v6, v5, v4);

            if (withShadow)
            {
                addShadow();
            }
        }
    }
}
