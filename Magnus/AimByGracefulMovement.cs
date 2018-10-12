using Mathematics.Math3D;
using System;

namespace Magnus
{
    class AimByGracefulMovement : Aim
    {
        private double forceToHit, timeToHit;

        private AimByWaitPosition hitWaitPositionAim;

        public AimByGracefulMovement(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0) : base(aimPlayer, aimPlayer0, aimT, aimT0)
        {
        }

        protected override void init()
        {
            base.init();

            var hitSpeed = AimPlayer.Speed.Length;
            if (hitSpeed == 0)
            {
                forceToHit = timeToHit = 0;
            }
            else
            {
                var forceToHaveEnoughTime = Misc.GetForceBySpeedAndTime(hitSpeed, Constants.MinTimeToMoveBeforeHit);
                var forceToHaveEnoughDistance = Misc.GetForceBySpeedAndDistance(hitSpeed, Constants.MinDistanceToMoveBeforeHit);
                forceToHit = Math.Min(Math.Max(forceToHaveEnoughTime, forceToHaveEnoughDistance), Constants.MaxPlayerForce);
                timeToHit = Misc.GetTimeBySpeedAndForce(hitSpeed, forceToHit);
            }

            var hitWaitPlayer = AimPlayer.Clone();
            hitWaitPlayer.Position = getHitPosition(timeToHit, forceToHit);
            hitWaitPlayer.Speed = Point3D.Empty;
            hitWaitPositionAim = new AimByWaitPosition(hitWaitPlayer, aimPlayer0, aimT0);
        }

        public override void SetCurrentState(Player player, double t)
        {
            base.SetCurrentState(player, t);
            hitWaitPositionAim.SetCurrentState(player, t);

            TimeToMove = hitWaitPositionAim.TimeToMove;
            HasTimeToReact = hitWaitPositionAim.HasTimeToReact && TimeToMove + timeToHit <= AimT - aimT0;
        }

        protected override Point3D getPlayerForce(State s, Player p)
        {
            double dt = AimT - s.Time;

            if (dt < timeToHit)
            {
                // This force should be used ideally
                var force = getHitForce(forceToHit);
                // But we know that there is some misaccuracy, so let's fix it
                var speedError = AimPlayer.Speed - (p.Speed + force * dt);
                force += speedError / timeToHit;
                var positionError = AimPlayer.Position - (p.Position + p.Speed * dt + force * (dt * dt / 2));
                force += Misc.GetForceByDistanceAndTime(positionError / 2, dt / 2);
                return force;
            }
            else
            {
                return hitWaitPositionAim.GetPlayerForce(s, p);
            }
        }
    }
}
