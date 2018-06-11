using Magnus.Strategies;
using System;

namespace Magnus
{
    class Player
    {
        public int Index;

        public int Side => Misc.GetPlayerSideByIndex(Index);

        public int Score;

        public Strategy Strategy;

        public DoublePoint Position, PrevPosition, Speed;

        public double Angle;

        public bool NeedAim;
        public Aim Aim;

        private Player()
        {
        }

        public Player(int index)
        {
            Index = index;
            Score = 0;
            Strategy = new Strategy();
            Position = PrevPosition = Speed = DoublePoint.Empty;
            Angle = 0;
            NeedAim = false;
            Aim = null;
        }

        public void Reset(double x, bool resetPosition, bool resetAngle)
        {
            if (resetPosition)
            {
                if (resetAngle)
                {
                    Angle = 180 + 90 * Side;
                }

                PrevPosition = Position = new DoublePoint(x * Side, Constants.NetHeight);
                Speed = DoublePoint.Empty;
            }

            NeedAim = false;
            Aim = null;
        }

        public Player Clone()
        {
            return new Player()
            {
                Index = Index,
                Score = Score,
                Strategy = Strategy,
                Position = Position,
                PrevPosition = PrevPosition,
                Speed = Speed,
                Angle = Angle,
                NeedAim = false,
                Aim = null
            };
        }

        public void DoStep(State s, double dt)
        {
            if (Aim != null)
            {
                bool stillMoving = Aim.UpdatePlayerPosition(s, this);

                if (!stillMoving)
                {
                    MoveToInitialPosition(s.Time);
                }
            }

            Speed = (Position - PrevPosition) / dt;
            PrevPosition = Position;
        }

        public void MoveToInitialPosition(double currentTime)
        {
            var readyPosition = Clone();
            readyPosition.Reset(Constants.HalfTableWidth + Constants.NetHeight * 2, true, false);
            NeedAim = false;
            Aim = new Aim(readyPosition, this, currentTime + 10000, currentTime);
        }

        public void RequestAim()
        {
            NeedAim = true;
        }

        public void FindHit(State state)
        {
            var initialTime = state.Time;

            if (state.GameState == GameState.Failed || Index != state.HitSide)
            {
                MoveToInitialPosition(initialTime);
                return;
            }

            var searchStartTime = DateTime.Now;

            var isServing = state.GameState == GameState.Serving;

            state = state.Clone();
            var player = state.Players[Index];

            if (isServing)
            {
                double yy = Constants.NetHeight * Misc.Rnd(1, 2);

                while (state.Speed.Y >= 0 || state.Position.Y >= yy)
                {
                    state.DoStep();
                }
            }
            else
            {
                if (!state.DoStepsUntilGameState(GameState.FlyingToBat))
                {
                    MoveToInitialPosition(initialTime);
                    return;
                }

                var timeCalcState = state.Clone();

                // Calculate time till max height
                timeCalcState.DoStepsUntilEvent(Event.MaxHeight);
                double ballMaxHeightTime = timeCalcState.Time - state.Time;

                // Calculate time till ball fall
                timeCalcState.DoStepsUntilEvent(Event.LowHit);
                double ballFallTime = timeCalcState.Time - state.Time;

                // Choose a random point to hit and move s2 to it
                double hitTime = state.Time + Strategy.GetBackHitTime(ballMaxHeightTime, ballFallTime);
                while (state.Time < hitTime)
                {
                    state.DoStep();
                }
            }

            double ballSpeedAngle = state.Speed.Angle;
            if (ballSpeedAngle == 180 && Index == Constants.LeftPlayerIndex)
            {
                ballSpeedAngle = -180;
            }

            while (NeedAim && (DateTime.Now - searchStartTime).TotalSeconds < 0.02)
            {
                double hitSpeed, attackAngle, velocityAttackAngle;
                if (isServing)
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Misc.Rnd(0.2, 0.7) * Math.Abs(state.Position.X) / Constants.HalfTableWidth;
                    attackAngle = Misc.Rnd(20, 100) * Side;
                    var rnd = Misc.Rnd(-1, 1);
                    velocityAttackAngle = (90 + 60 * rnd * rnd * rnd) * Side;
                }
                else
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Strategy.GetHitSpeed();
                    attackAngle = Strategy.GetAttackAngle() * Side;
                    velocityAttackAngle = Strategy.GetVelocityAttackAngle() * Side;
                    velocityAttackAngle = Math.Max(velocityAttackAngle, attackAngle - 70);
                    velocityAttackAngle = Math.Min(velocityAttackAngle, attackAngle + 70);
                }

                player.Position = state.Position + Constants.BallRadius * DoublePoint.FromAngle(ballSpeedAngle - attackAngle);
                player.Speed = -hitSpeed * DoublePoint.FromAngle(ballSpeedAngle - velocityAttackAngle);
                player.Angle = 180 + ballSpeedAngle - attackAngle;

                var newAim = new Aim(player, this, state.Time, initialTime);
                if (newAim.HasTimeToReact)
                {
                    var simulation = new Simulation(state.Clone());
                    if (simulation.Success)
                    {
                        NeedAim = false;
                        Aim = newAim;
                    }
                }
            }
        }
    }
}
