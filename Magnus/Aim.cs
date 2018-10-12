using Mathematics.Math3D;
using System;

namespace Magnus
{
    abstract class Aim
    {
        public readonly Player AimPlayer;
        public readonly double AimT;
        protected Player aimPlayer0;
        protected double aimT0;

        private readonly double timeToStop;

        public double TimeToMove { get; protected set; }

        public bool HasTimeToReact { get; protected set; }

        protected Aim(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0)
        {
            AimPlayer = aimPlayer.Clone();
            AimT = aimT;
            this.aimPlayer0 = aimPlayer0.Clone();
            this.aimT0 = aimT0;

            timeToStop = Misc.GetTimeBySpeedAndForce(aimPlayer.Speed.Length, Constants.MaxPlayerForce);

            init();

            SetCurrentState(aimPlayer0, aimT0);
        }

        protected virtual void init()
        {
        }

        public virtual void SetCurrentState(Player player, double t)
        {
            aimPlayer0 = player.Clone();
            aimT0 = t;

            // Implementation class must set HasTimeToReact and TimeToMove variables' values
        }

        public bool TrySetCurrentState(Player player, double t)
        {
            var prevPlayer0 = aimPlayer0;
            var prevT0 = aimT0;
            SetCurrentState(player, t);
            var success = HasTimeToReact;
            if (!success)
            {
                SetCurrentState(prevPlayer0, prevT0);
            }
            return success;
        }

        protected Point3D getHitPosition(double t, double forceToHit)
        {
            return AimPlayer.Position - AimPlayer.Speed * t + getHitForce(forceToHit) * (t * t / 2);
        }

        protected Point3D getHitForce(double forceToHit)
        {
            if (AimPlayer.Speed.Length != 0)
            {
                return forceToHit * AimPlayer.Speed.Normal;
            }
            else
            {
                return Point3D.Empty;
            }
        }

        protected abstract Point3D getPlayerForce(State s, Player p);

        public Point3D GetPlayerForce(State s, Player p)
        {
            if (s.Time < AimT)
            {
                return getPlayerForce(s, p);
            }
            else
            {
                return Point3D.Empty;
            }
        }

        public void UpdatePlayer(State s, Player p)
        {
            p.Force = GetPlayerForce(s, p);

            var angleCoeff = Math.Min((s.Time - aimT0) / TimeToMove, 1);
            p.AnglePitch = aimPlayer0.AnglePitch + Misc.NormalizeAngle(AimPlayer.AnglePitch - aimPlayer0.AnglePitch) * angleCoeff;
            p.AngleYaw = aimPlayer0.AngleYaw + Misc.NormalizeAngle(AimPlayer.AngleYaw - aimPlayer0.AngleYaw) * angleCoeff;
        }
    }
}
