using Mathematics.Math3D;
using System;

namespace Magnus
{
    class Aim
    {
        public readonly Player AimPlayer, AimPlayer0;
        public readonly double AimT, AimT0;

        private readonly double hitSpeed, forceToHit, timeToHit;

        private readonly Point3D moveVector;
        private readonly double moveLength, forceMoveLength, timeToForceMove, timeToSpeedMove, timeToMove;

        public readonly bool HasTimeToReact;

        public Aim(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0)
        {
            AimPlayer = aimPlayer.Clone();
            AimPlayer0 = aimPlayer0.Clone();
            AimT = aimT;
            AimT0 = aimT0;

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
            forceMoveLength = Math.Min(moveLength / 2, Misc.GetDistanceBySpeedAndForce(Constants.MaxPlayerSpeed, Constants.MaxPlayerForce));
            timeToForceMove = Misc.GetTimeByDistanceAndForce(forceMoveLength, Constants.MaxPlayerForce);
            timeToSpeedMove = (moveLength - 2 * forceMoveLength) / Constants.MaxPlayerSpeed;
            timeToMove = timeToForceMove * 2 + timeToSpeedMove;

            HasTimeToReact = timeToMove + timeToHit <= aimT - aimT0;
        }

        private Point3D getHitPosition(double t)
        {
            return AimPlayer.Position + AimPlayer.Speed * t - AimPlayer.Speed.Normal * (forceToHit * t * Math.Abs(t) / 2);
        }

        public bool UpdatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.Time - AimT;

            if (timeFromState > timeToHit)
            {
                return false;
            }

            if (timeFromState > -timeToHit)
            {
                p.Position = getHitPosition(timeFromState);
                p.AnglePitch = AimPlayer.AnglePitch;
                p.AngleYaw = AimPlayer.AngleYaw;
            }
            else
            {
                double timeFromState0 = s.Time - AimT0;

                double currentMoveLength;
                if (timeFromState0 <= timeToForceMove)
                {
                    currentMoveLength = Misc.GetDistanceByForceAndTime(Constants.MaxPlayerForce, timeFromState0);
                }
                else
                {
                    var timeFromMaxSpeedStart = timeFromState0 - timeToForceMove;

                    if (timeFromMaxSpeedStart <= timeToSpeedMove)
                    {
                        currentMoveLength = forceMoveLength + Constants.MaxPlayerSpeed * timeFromMaxSpeedStart;
                    }
                    else
                    {
                        var timeFromMoveEnd = timeFromState0 - timeToMove;

                        if (timeFromMoveEnd < 0)
                        {
                            currentMoveLength = moveLength - Misc.GetDistanceByForceAndTime(Constants.MaxPlayerForce, timeFromMoveEnd);
                        }
                        else
                        {
                            currentMoveLength = moveLength;
                        }
                    }
                }

                p.Position = AimPlayer0.Position + currentMoveLength * moveVector.Normal;
                var angleCoeff = Math.Min((s.Time - AimT0) / timeToMove, 1);
                p.AnglePitch = AimPlayer0.AnglePitch + Misc.NormalizeAngle(AimPlayer.AnglePitch - AimPlayer0.AnglePitch) * angleCoeff;
                p.AngleYaw = AimPlayer0.AngleYaw + Misc.NormalizeAngle(AimPlayer.AngleYaw - AimPlayer0.AngleYaw) * angleCoeff;
            }

            return true;
        }
    }
}
