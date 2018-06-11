using System;

namespace Magnus
{
    class Aim
    {
        private readonly Player aimPlayer, aimPlayer0;
        private readonly double aimT, aimT0;

        private readonly double hitSpeed, forceToHit, timeToHit;

        private readonly DoublePoint moveVector;
        private readonly double moveLength, forceMoveLength, timeToForceMove, timeToSpeedMove, timeToMove;

        public readonly bool HasTimeToReact;

        public Aim(Player aimPlayer, Player aimPlayer0, double aimT, double aimT0)
        {
            this.aimPlayer = aimPlayer.Clone();
            this.aimPlayer0 = aimPlayer0.Clone();
            this.aimT = aimT;
            this.aimT0 = aimT0;

            hitSpeed = aimPlayer.Speed.Length;
            if (hitSpeed == 0)
            {
                forceToHit = timeToHit = 0;
            }
            else
            {
                forceToHit = Math.Min(Math.Max(hitSpeed / (0.2 * 7), hitSpeed * hitSpeed / 2 / 200), Constants.MaxPlayerForce);
                timeToHit = hitSpeed / forceToHit;
            }

            moveVector = getHitPosition(-timeToHit) - aimPlayer0.Position;
            moveLength = moveVector.Length;
            forceMoveLength = Math.Min(moveLength / 2, Constants.MaxPlayerSpeed * Constants.MaxPlayerSpeed / 2 / Constants.MaxPlayerForce);
            timeToForceMove = Math.Sqrt(2 * forceMoveLength / Constants.MaxPlayerForce);
            timeToSpeedMove = (moveLength - 2 * forceMoveLength) / Constants.MaxPlayerSpeed;
            timeToMove = timeToForceMove * 2 + timeToSpeedMove;

            HasTimeToReact = timeToMove + timeToHit <= aimT - aimT0;
        }

        private DoublePoint getHitPosition(double t)
        {
            return aimPlayer.Position + aimPlayer.Speed * t - aimPlayer.Speed.Normal * (forceToHit * t * Math.Abs(t) / 2);
        }

        public bool UpdatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.Time - aimT;

            if (timeFromState > timeToHit)
            {
                return false;
            }

            if (timeFromState > -timeToHit)
            {
                p.Position = getHitPosition(timeFromState);
                p.Angle = aimPlayer.Angle;
            }
            else
            {
                double timeFromState0 = s.Time - aimT0;

                double currentMoveLength;
                if (timeFromState0 <= timeToForceMove)
                {
                    currentMoveLength = Constants.MaxPlayerForce * timeFromState0 * timeFromState0 / 2;
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
                            currentMoveLength = moveLength - Constants.MaxPlayerForce * timeFromMoveEnd * timeFromMoveEnd / 2;
                        }
                        else
                        {
                            currentMoveLength = moveLength;
                        }
                    }
                }

                p.Position = aimPlayer0.Position + currentMoveLength * moveVector.Normal;
                p.Angle = aimPlayer0.Angle + (aimPlayer.Angle - aimPlayer0.Angle) * Math.Min((s.Time - aimT0) / (timeToForceMove * 2 + timeToSpeedMove), 1);
            }

            return true;
        }
    }
}
