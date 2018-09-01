using Mathematics.Math3D;
using System.Linq;

namespace Magnus.MagnusGL
{
    class GlPlayerHandle : GlPlayer
    {
        public GlPlayerHandle(Player player)
        {
            var woodColor = getWoodColor(player);

            var nearestLinePoints = getBatPoints(player, Constants.BatBiggerRadius - Constants.BallRadius, Constants.BatThickness / 2);
            var nearLinePoints = getBatPoints(player, Constants.BatBiggerRadius, Constants.BallRadius / 2);
            var farLinePoints = getBatPoints(player, Constants.BatBiggerRadius * 1.5, Constants.BallRadius / 2);

            addPolygon(woodColor, nearestLinePoints.Reverse().ToArray());
            addCylinder(woodColor, nearestLinePoints, nearLinePoints);
            addCylinder(woodColor, nearLinePoints, farLinePoints);
            addPolygon(woodColor, farLinePoints);

            addShadow();
        }

        private GlIndexedVertex[] getBatPoints(Player player, double x, double r)
        {
            return getCirclePoints(r).Select(point => new GlIndexedVertex(player.TranslatePointFromBatCoords(new Point3D(point.Z * 2, point.Y, x)), nextVertexIndex)).ToArray();
        }
    }
}
