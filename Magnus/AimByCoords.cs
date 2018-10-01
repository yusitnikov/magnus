using Mathematics.Math3D;
using System;

namespace Magnus
{
    class AimByCoords : Aim
    {
        private readonly AimCoord aimX, aimY, aimZ;

        public AimByCoords(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0) : base(aimPlayer, aimPlayer0, aimT, aimT0)
        {
            aimX = new AimCoord(AimPlayer.Position.X, AimPlayer0.Position.X, AimPlayer.Speed.X, AimPlayer0.Speed.X, AimT, AimT0);
            aimY = new AimCoord(AimPlayer.Position.Y, AimPlayer0.Position.Y, AimPlayer.Speed.Y, AimPlayer0.Speed.Y, AimT, AimT0);
            aimZ = new AimCoord(AimPlayer.Position.Z, AimPlayer0.Position.Z, AimPlayer.Speed.Z, AimPlayer0.Speed.Z, AimT, AimT0);

            timeToMove = Math.Max(aimX.timeToMove, Math.Max(aimY.timeToMove, aimZ.timeToMove));

            HasTimeToReact = aimX.HasTimeToReact && aimY.HasTimeToReact && aimZ.HasTimeToReact;
        }

        protected override void updatePlayerPosition(State s, Player p)
        {
            p.Position = new Point3D(
                aimX.GetX(s),
                aimY.GetX(s),
                aimZ.GetX(s)
            );
        }
    }
}
