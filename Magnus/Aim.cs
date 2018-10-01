using Mathematics.Math3D;
using System;

namespace Magnus
{
    abstract class Aim
    {
        public readonly Player AimPlayer, AimPlayer0;
        public readonly double AimT, AimT0;

        private readonly double timeToStop;

        protected double timeToMove;

        public bool HasTimeToReact { get; protected set; }

        protected Aim(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0)
        {
            AimPlayer = aimPlayer.Clone();
            AimPlayer0 = aimPlayer0.Clone();
            AimT = aimT;
            AimT0 = aimT0;

            timeToStop = Misc.GetTimeBySpeedAndForce(aimPlayer.Speed.Length, Constants.MaxPlayerForce);
        }

        protected Point3D getHitPosition(double t, double forceToHit)
        {
            return AimPlayer.Position + AimPlayer.Speed * t - AimPlayer.Speed.Normal * (forceToHit * t * Math.Abs(t) / 2);
        }

        protected abstract void updatePlayerPosition(State s, Player p);

        public virtual bool UpdatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.Time - AimT;

            if (timeFromState > timeToStop)
            {
                return false;
            }

            if (timeFromState > 0)
            {
                p.Position = getHitPosition(timeFromState, Constants.MaxPlayerForce);
            }
            else
            {
                updatePlayerPosition(s, p);
            }

            var angleCoeff = Math.Min((s.Time - AimT0) / timeToMove, 1);
            p.AnglePitch = AimPlayer0.AnglePitch + Misc.NormalizeAngle(AimPlayer.AnglePitch - AimPlayer0.AnglePitch) * angleCoeff;
            p.AngleYaw = AimPlayer0.AngleYaw + Misc.NormalizeAngle(AimPlayer.AngleYaw - AimPlayer0.AngleYaw) * angleCoeff;

            return true;
        }
    }
}
