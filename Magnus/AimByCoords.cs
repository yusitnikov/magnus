using Mathematics.Math3D;
using System;

namespace Magnus
{
    class AimByCoords<AimCoordType> : Aim where AimCoordType : AimCoord
    {
        private AimCoordType aimX, aimY, aimZ;

        private Point3D xAxis, yAxis, zAxis;

        public AimByCoords(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0) : base(aimPlayer, aimPlayer0, aimT, aimT0)
        {
        }

        protected override void init()
        {
            base.init();

            var constructor = typeof(AimCoordType).GetConstructors()[0];
            var args = new object[0];
            aimX = constructor.Invoke(args) as AimCoordType;
            aimY = constructor.Invoke(args) as AimCoordType;
            aimZ = constructor.Invoke(args) as AimCoordType;

            // Set axises at init() to keep them constant per SetCurrentState calls
            var dp = AimPlayer.Position - aimPlayer0.Position;
            var pitch = dp.Pitch;
            var yaw = dp.Yaw;
            // same as Point3D.FromAngles(pitch, yaw), so should be equal to dp.Normal
            yAxis = Point3D.YAxis.RotatePitch(pitch).RotateYaw(yaw);
            xAxis = (AimPlayer.Speed - aimPlayer0.Speed).ProjectToNormalVector(yAxis).Horizontal.Normal;
            if (Math.Abs(Point3D.ScalarMult(xAxis, yAxis) - 1) < 1e-3)
            {
                zAxis = Point3D.VectorMult(xAxis, yAxis);
            }
            else
            {
                xAxis = Point3D.XAxis.RotatePitch(pitch).RotateYaw(yaw);
                zAxis = Point3D.ZAxis.RotatePitch(pitch).RotateYaw(yaw);
            }
        }

        public override void SetCurrentState(Player player, double t)
        {
            base.SetCurrentState(player, t);

            setCoordState(aimX, xAxis);
            setCoordState(aimY, yAxis);
            setCoordState(aimZ, zAxis);

            TimeToMove = Math.Max(aimX.TimeToMove, Math.Max(aimY.TimeToMove, aimZ.TimeToMove));

            HasTimeToReact = aimX.HasTimeToReact && aimY.HasTimeToReact && aimZ.HasTimeToReact;
        }

        private void setCoordState(AimCoordType aimCoord, Point3D axis)
        {
            aimCoord.SetAimState(Point3D.ScalarMult(AimPlayer.Position, axis), Point3D.ScalarMult(AimPlayer.Speed, axis), AimT);
            aimCoord.SetCurrentState(Point3D.ScalarMult(aimPlayer0.Position, axis), Point3D.ScalarMult(aimPlayer0.Speed, axis), aimT0);
        }

        protected override Point3D getPlayerForce(State s, Player p)
        {
            return xAxis * aimX.GetForce(s) + yAxis * aimY.GetForce(s) + zAxis * aimZ.GetForce(s);
        }
    }
}
