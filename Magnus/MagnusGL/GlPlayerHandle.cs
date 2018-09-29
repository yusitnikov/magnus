using Mathematics.Math3D;

namespace Magnus.MagnusGL
{
    class GlPlayerHandle : GlPlayer
    {
        public GlPlayerHandle(Player player)
        {
            var woodColor = getWoodColor(player);

            var nearestLinePoints = getBatPoints(player, Constants.BatBiggerRadius - Constants.BallRadius, Constants.BatThickness / 2);
            var nearestLinePointsReversed = new GlIndexedVertex[nearestLinePoints.Length];
            for (var i = 0; i < nearestLinePoints.Length; i++)
            {
                nearestLinePointsReversed[i] = nearestLinePoints[nearestLinePoints.Length - 1 - i];
            }
            var nearLinePoints = getBatPoints(player, Constants.BatBiggerRadius, Constants.BallRadius / 2);
            var farLinePoints = getBatPoints(player, Constants.BatBiggerRadius * 1.5, Constants.BallRadius / 2);

            addPolygon(woodColor, nearestLinePointsReversed);
            addCylinder(woodColor, nearestLinePoints, nearLinePoints);
            addCylinder(woodColor, nearLinePoints, farLinePoints);
            addPolygon(woodColor, farLinePoints);

            addShadow();
        }

        private GlIndexedVertex[] getBatPoints(Player player, double x, double r)
        {
            var result = getCirclePoints(r);
            foreach (var v in result)
            {
                v.Position = player.TranslatePointFromBatCoords(new Point3D(v.Position.Z * 2, v.Position.Y, x));
            }
            return result;
        }
    }
}
