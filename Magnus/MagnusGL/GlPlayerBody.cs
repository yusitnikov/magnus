using Mathematics.Math3D;
using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlPlayerBody : GlPlayer
    {
        public GlPlayerBody(Player player)
        {
            var blackPoints = getBatPoints(player, 1);
            var redPoints = getBatPoints(player, -1);
            var redPointsReversed = new GlIndexedVertex[redPoints.Length];
            for (var i = 0; i < redPoints.Length; i++)
            {
                redPointsReversed[i] = redPoints[redPoints.Length - 1 - i];
            }

            addPolygon(Color.Black, blackPoints);
            addPolygon(Color.Red, redPointsReversed);
            addCylinder(getWoodColor(player), redPoints, blackPoints);

            addShadow();
        }

        private GlIndexedVertex[] getBatPoints(Player player, int sign)
        {
            var result = getCirclePoints(1);
            foreach (var v in result)
            {
                v.Position = player.TranslatePointFromBatCoords(new Point3D(v.Position.Y * Constants.BatRadius, sign * Constants.BatThickness / 2, v.Position.Z * Constants.BatBiggerRadius));
            }
            return result;
        }
    }
}
