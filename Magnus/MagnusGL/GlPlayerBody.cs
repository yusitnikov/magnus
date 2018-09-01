using Mathematics.Math3D;
using System.Drawing;
using System.Linq;

namespace Magnus.MagnusGL
{
    class GlPlayerBody : GlPlayer
    {
        public GlPlayerBody(Player player)
        {
            var blackPoints = getBatPoints(player, 1);
            var redPoints = getBatPoints(player, -1);

            addPolygon(Color.Black, blackPoints);
            addPolygon(Color.Red, redPoints.Reverse().ToArray());
            addCylinder(getWoodColor(player), redPoints, blackPoints);

            addShadow();
        }

        private GlIndexedVertex[] getBatPoints(Player player, int sign)
        {
            return getCirclePoints(1).Select(point => new GlIndexedVertex(player.TranslatePointFromBatCoords(new Point3D(point.Y * Constants.BatRadius, sign * Constants.BatThickness / 2, point.Z * Constants.BatBiggerRadius)), nextVertexIndex)).ToArray();
        }
    }
}
