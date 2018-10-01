using Mathematics.Math3D;
using System;

namespace Magnus
{
    class AimByGracefulMovement : Aim
    {
        private readonly double hitSpeed, forceToHit, timeToHit;

        private readonly Point3D moveVector;
        private readonly double moveLength;

        public AimByGracefulMovement(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0) : base(aimPlayer, aimPlayer0, aimT, aimT0)
        {
            hitSpeed = aimPlayer.Speed.Length;
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

            moveVector = getHitPosition(-timeToHit) - aimPlayer0.Position;
            moveLength = moveVector.Length;
            timeToMove = Misc.GetTimeByDistanceAndForce(moveLength / 2, Constants.MaxPlayerForce) * 2;

            HasTimeToReact = timeToMove + timeToHit <= aimT - aimT0;
        }

        private Point3D getHitPosition(double t)
        {
            return getHitPosition(t, forceToHit);
        }

        protected override void updatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.Time - AimT;

            if (timeFromState > -timeToHit)
            {
                p.Position = getHitPosition(timeFromState);
            }
            else
            {
                double timeFromState0 = s.Time - AimT0;

                double currentMoveLength;
                if (timeFromState0 <= timeToMove / 2)
                {
                    currentMoveLength = Misc.GetDistanceByForceAndTime(Constants.MaxPlayerForce, timeFromState0);
                }
                else
                {
                    var timeFromMoveEnd = timeFromState0 - timeToMove;

                    currentMoveLength = moveLength;

                    if (timeFromMoveEnd < 0)
                    {
                        currentMoveLength -= Misc.GetDistanceByForceAndTime(Constants.MaxPlayerForce, timeFromMoveEnd);
                    }
                }

                p.Position = AimPlayer0.Position + currentMoveLength * moveVector.Normal;
            }
        }
    }
}
