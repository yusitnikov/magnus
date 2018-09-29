using Mathematics.Math3D;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Magnus.MagnusGL
{
    abstract class GlMesh
    {
        public const int DefaultCirclePointsCount = 30;

        private int autoIncrementVertexIndex = 0;
        protected int nextVertexIndex => autoIncrementVertexIndex++;

        public readonly List<GlTriangle> Triangles = new List<GlTriangle>();
        public readonly List<GlTriangle> ShadowTriangles = new List<GlTriangle>();

        protected void addShadow()
        {
            Profiler.Instance.LogEvent("mesh: init");

            var edges = new Dictionary<int, GlTriangleEdge>();
            foreach (var triangle in Triangles)
            {
                GlTriangleEdge edge;
                edge = new GlTriangleEdge(triangle.V0, triangle.V1);
                edges[edge.Hash] = edge;
                edge = new GlTriangleEdge(triangle.V1, triangle.V2);
                edges[edge.Hash] = edge;
                edge = new GlTriangleEdge(triangle.V2, triangle.V0);
                edges[edge.Hash] = edge;
            }
            Profiler.Instance.LogEvent("mesh: add edges");

            var color = Color.Black;
            foreach (var edge in edges.Values)
            {
                GlNormalizedVertex v1 = edge.V1, v2 = edge.V2;
                if (edges.TryGetValue(new GlTriangleEdge(v2, v1).Hash, out GlTriangleEdge neighbor))
                {
                    GlNormalizedVertex v3 = neighbor.V1, v4 = neighbor.V2;
                    if (!v3.Normal.Equals(v2.Normal))
                    {
                        ShadowTriangles.Add(new GlTriangle(color, v4, v3, v2));
                        ShadowTriangles.Add(new GlTriangle(color, v4, v2, v1));
                    }
                }
            }
            Profiler.Instance.LogEvent("mesh: add shadow");
        }

        protected GlIndexedVertex[] getCirclePoints(double radius, int circlePointsCount = DefaultCirclePointsCount)
        {
            var points = new GlIndexedVertex[circlePointsCount];
            for (var i = 0; i < circlePointsCount; i++)
            {
                points[i] = new GlIndexedVertex(radius * Point3D.YAxis.RotateRoll(2 * Math.PI * i / circlePointsCount), nextVertexIndex);
            }
            return points;
        }

        protected void addPolygon(Color color, params GlIndexedVertex[] points)
        {
            var trianglesCount = points.Length - 2;
            for (var i = 0; i < trianglesCount; i++)
            {
                var triangle = new GlTriangle(color, points[0], points[i + 1], points[i + 2]);
                if (triangle.V0.Normal.Length != 0)
                {
                    Triangles.Add(triangle);
                }
            }
        }

        protected void addCylinder(Color color, GlIndexedVertex[] circle1, GlIndexedVertex[] circle2)
        {
            for (var i = 1; i <= circle1.Length; i++)
            {
                int i1 = i - 1, i2 = i % circle1.Length;
                GlIndexedVertex p1 = circle1[i1], p2 = circle1[i2], p3 = circle2[i2], p4 = circle2[i1];
                addPolygon(color, p1, p2, p3, p4);
            }
        }
    }
}
