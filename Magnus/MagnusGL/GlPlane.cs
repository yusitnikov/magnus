using Mathematics.Math3D;
using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlPlane : GlMesh
    {
        public GlPlane(Color color, double y, double size)
        {
            addPolygon(
                color,
                new GlIndexedVertex(new Point3D(0, y, size), nextVertexIndex),
                new GlIndexedVertex(new Point3D(size, y, 0), nextVertexIndex),
                new GlIndexedVertex(new Point3D(0, y, -size), nextVertexIndex),
                new GlIndexedVertex(new Point3D(-size, y, 0), nextVertexIndex)
            );

            // don't add a shadow!
        }
    }
}
