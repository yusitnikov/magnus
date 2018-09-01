using Mathematics.Math3D;
using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlTriangle
    {
        public Color Color;
        public GlNormalizedVertex V0, V1, V2;

        public GlTriangle(Color color, GlNormalizedVertex v0, GlNormalizedVertex v1, GlNormalizedVertex v2)
        {
            Color = color;
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }

        public GlTriangle(Color color, GlIndexedVertex v0, GlIndexedVertex v1, GlIndexedVertex v2)
        {
            Color = color;
            var normal = Point3D.VectorMult(v1.Position - v0.Position, v2.Position - v0.Position).Normal;
            V0 = new GlNormalizedVertex(v0, normal);
            V1 = new GlNormalizedVertex(v1, normal);
            V2 = new GlNormalizedVertex(v2, normal);
        }
    }
}
