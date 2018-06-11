﻿using System;

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

            hitSpeed = aimPlayer.speed.Length;
            if (hitSpeed == 0)
            {
                forceToHit = timeToHit = 0;
            }
            else
            {
                forceToHit = Math.Min(Math.Max(hitSpeed / (0.2 * 7), hitSpeed * hitSpeed / 2 / 200), Constants.ma);
                timeToHit = hitSpeed / forceToHit;
            }

            moveVector = getHitPosition(-timeToHit) - aimPlayer0.pos;
            moveLength = moveVector.Length;
            forceMoveLength = Math.Min(moveLength / 2, Constants.mv * Constants.mv / 2 / Constants.ma);
            timeToForceMove = Math.Sqrt(2 * forceMoveLength / Constants.ma);
            timeToSpeedMove = (moveLength - 2 * forceMoveLength) / Constants.mv;
            timeToMove = timeToForceMove * 2 + timeToSpeedMove;

            HasTimeToReact = timeToMove + timeToHit <= aimT - aimT0;
        }

        private DoublePoint getHitPosition(double t)
        {
            return aimPlayer.pos + aimPlayer.speed * t - aimPlayer.speed.Normal * (forceToHit * t * Math.Abs(t) / 2);
        }

        public bool UpdatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.t - aimT;

            if (timeFromState > timeToHit)
            {
                return false;
            }

            if (timeFromState > -timeToHit)
            {
                p.pos = getHitPosition(timeFromState);
                p.a = aimPlayer.a;
            }
            else
            {
                double timeFromState0 = s.t - aimT0;

                double currentMoveLength;
                if (timeFromState0 <= timeToForceMove)
                {
                    currentMoveLength = Constants.ma * timeFromState0 * timeFromState0 / 2;
                }
                else
                {
                    var timeFromMaxSpeedStart = timeFromState0 - timeToForceMove;

                    if (timeFromMaxSpeedStart <= timeToSpeedMove)
                    {
                        currentMoveLength = forceMoveLength + Constants.mv * timeFromMaxSpeedStart;
                    }
                    else
                    {
                        var timeFromMoveEnd = timeFromState0 - timeToMove;

                        if (timeFromMoveEnd < 0)
                        {
                            currentMoveLength = moveLength - Constants.ma * timeFromMoveEnd * timeFromMoveEnd / 2;
                        }
                        else
                        {
                            currentMoveLength = moveLength;
                        }
                    }
                }

                p.pos = aimPlayer0.pos + currentMoveLength * moveVector.Normal;
                p.a = aimPlayer0.a + (aimPlayer.a - aimPlayer0.a) * Math.Min((s.t - aimT0) / (timeToForceMove * 2 + timeToSpeedMove), 1);
            }

            return true;
        }
    }
}
