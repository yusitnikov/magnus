using System;
using System.Drawing;
using System.Linq;

namespace Magnus.MagnusGL
{
    class GlBall : GlMesh
    {
        public GlBall(Color color, DoublePoint3D center, double radius, int circlesCount = 10)
        {
            var circlePointsCount = circlesCount * 2;
            GlIndexedVertex[] circle1 = null;
            for (var i = 0; i <= circlesCount; i++)
            {
                var a = Math.PI * i / circlesCount;
                GlIndexedVertex[] circle2 = getCirclePoints(radius * Math.Sin(a), circlePointsCount).Select(p => new GlIndexedVertex(center + radius * Math.Cos(a) * DoublePoint3D.XAxis + p, nextVertexIndex)).ToArray();
                if (circle1 != null)
                {
                    addCylinder(color, circle1, circle2);
                }
                circle1 = circle2;
            }

            addShadow();
        }
    }
}
