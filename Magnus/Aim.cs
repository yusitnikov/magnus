using System;

namespace Magnus
{
    class Aim
    {
        private readonly State state, state0;
        private readonly Player player, player0;

        private readonly double hitSpeed, forceToHit, timeToHit;

        private readonly DoublePoint moveVector;
        private readonly double moveLength, forceMoveLength, timeToForceMove, timeToSpeedMove, timeToMove;

        public readonly bool HasTimeToReact;

        public Aim(State state, State state0, int playerIndex)
        {
            this.state = state;
            this.state0 = state0;

            player = state.p[playerIndex];
            player0 = state0.p[playerIndex];

            hitSpeed = player.speed.Length;
            if (hitSpeed == 0)
            {
                forceToHit = timeToHit = 0;
            }
            else
            {
                forceToHit = Math.Min(Math.Max(hitSpeed / (0.2 * 7), hitSpeed * hitSpeed / 2 / 200), Constants.ma);
                timeToHit = hitSpeed / forceToHit;
            }

            moveVector = getHitPosition(-timeToHit) - player0.pos;
            moveLength = moveVector.Length;
            forceMoveLength = Math.Min(moveLength / 2, Constants.mv * Constants.mv / 2 / Constants.ma);
            timeToForceMove = Math.Sqrt(2 * forceMoveLength / Constants.ma);
            timeToSpeedMove = (moveLength - 2 * forceMoveLength) / Constants.mv;
            timeToMove = timeToForceMove * 2 + timeToSpeedMove;

            HasTimeToReact = timeToMove + timeToHit <= state.t - state0.t;
        }

        private DoublePoint getHitPosition(double t)
        {
            return player.pos + player.speed * t - player.speed.Normal * (forceToHit * t * Math.Abs(t) / 2);
        }

        public bool UpdatePlayerPosition(State s, Player p)
        {
            double timeFromState = s.t - state.t;

            if (timeFromState > timeToHit)
            {
                return false;
            }

            if (timeFromState > -timeToHit)
            {
                p.pos = getHitPosition(timeFromState);
                p.a = player.a;
            }
            else
            {
                double timeFromState0 = s.t - state0.t;

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

                p.pos = player0.pos + currentMoveLength * moveVector.Normal;
                p.a = player0.a + (player.a - player0.a) * Math.Min((s.t - state0.t) / (timeToForceMove * 2 + timeToSpeedMove), 1);
            }

            return true;
        }
    }
}
