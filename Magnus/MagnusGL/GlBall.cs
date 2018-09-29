using Mathematics.Math3D;
using System;
using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlBall : GlMesh
    {
        public GlBall(Color color, Point3D center, double radius, int circlesCount = 10)
        {
            var circlePointsCount = circlesCount * 2;
            GlIndexedVertex[] circle1 = null;
            for (var i = 0; i <= circlesCount; i++)
            {
                var a = Math.PI * i / circlesCount;
                GlIndexedVertex[] circle2 = getCirclePoints(radius * Math.Sin(a), circlePointsCount);
                foreach (var v in circle2)
                {
                    v.Position += center + radius * Math.Cos(a) * Point3D.XAxis;
                }
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
