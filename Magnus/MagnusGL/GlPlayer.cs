using System.Drawing;

namespace Magnus.MagnusGL
{
    abstract class GlPlayer : GlMesh
    {
        public Color getWoodColor(Player player)
        {
            return player.NeedAim ? Color.DarkRed : Color.BurlyWood;
        }
    }
}
