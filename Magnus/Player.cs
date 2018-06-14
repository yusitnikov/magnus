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

        public DoublePoint Position, Speed;

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
            Position = Speed = DoublePoint.Empty;
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
                    Angle = Misc.FromDegrees(180 + 90 * Side);
                }

                Position = new DoublePoint(x * Side, Constants.BatWaitY);
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
                Speed = Speed,
                Angle = Angle,
                NeedAim = false,
                Aim = null
            };
        }

        public void DoStep(State state, double dt)
        {
            var prevPosition = Position;

            if (Aim != null)
            {
                bool stillMoving = Aim.UpdatePlayerPosition(state, this);

                if (!stillMoving)
                {
                    MoveToInitialPosition(state.Time);
                }
            }

            Speed = (Position - prevPosition) / dt;
        }

        public void MoveToInitialPosition(double currentTime)
        {
            var readyPosition = Clone();
            readyPosition.Reset(Constants.BatWaitX, true, false);
            NeedAim = false;
            Aim = new Aim(readyPosition, this, double.PositiveInfinity, currentTime);
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

            state = state.Clone(true);

            Action<State> doStepsTillHitTime;

            if (isServing)
            {
                while (state.Ball.Speed.Y >= 0 || state.Ball.Position.Y >= Constants.MaxBallServeY)
                {
                    state.DoStep();
                }

                doStepsTillHitTime = attemptState =>
                {
                    double serveY = Misc.Rnd(Constants.MinBallServeY, Constants.MaxBallServeY);

                    while (state.Ball.Speed.Y >= 0 || attemptState.Ball.Position.Y >= serveY)
                    {
                        attemptState.DoStep();
                    }
                };
            }
            else
            {
                if (!state.DoStepsUntilGameState(GameState.FlyingToBat))
                {
                    MoveToInitialPosition(initialTime);
                    return;
                }

                var timeCalcState = state.Clone(false);

                // Calculate time till max height
                timeCalcState.DoStepsUntilEvent(Event.MaxHeight);
                double ballMaxHeightTime = timeCalcState.Time - state.Time;

                // Calculate time till ball fall
                timeCalcState.DoStepsUntilEvent(Event.LowHit);
                double ballFallTime = timeCalcState.Time - state.Time;

                doStepsTillHitTime = attemptState =>
                {
                    double hitTime = attemptState.Time + Strategy.GetBackHitTime(ballMaxHeightTime, ballFallTime);

                    while (attemptState.Time < hitTime)
                    {
                        attemptState.DoStep();
                    }
                };
            }

            while (NeedAim && (DateTime.Now - searchStartTime).TotalSeconds < Constants.MaxThinkTimePerFrame)
            {
                var attemptState = state.Clone(true);
                doStepsTillHitTime(attemptState);

                var player = attemptState.Players[Index];

                double ballSpeedAngle = attemptState.Ball.Speed.Angle;
                if (ballSpeedAngle == Math.PI && Index == Constants.LeftPlayerIndex)
                {
                    ballSpeedAngle = -Math.PI;
                }

                double hitSpeed, attackAngle, velocityAttackAngle;
                if (isServing)
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Strategy.GetServeHitSpeed(Math.Abs(attemptState.Ball.Position.X));
                    attackAngle = Strategy.GetServeAttackAngle() * Side;
                    velocityAttackAngle = Strategy.GetServeVelocityAttackAngle() * Side;
                }
                else
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Strategy.GetHitSpeed();
                    attackAngle = Strategy.GetAttackAngle() * Side;
                    velocityAttackAngle = Strategy.GetVelocityAttackAngle() * Side;
                    velocityAttackAngle = Math.Max(velocityAttackAngle, attackAngle - Constants.MaxAttackAngleDifference);
                    velocityAttackAngle = Math.Min(velocityAttackAngle, attackAngle + Constants.MaxAttackAngleDifference);
                }

                player.Position = attemptState.Ball.Position + Constants.BallRadius * DoublePoint.FromAngle(ballSpeedAngle - attackAngle);
                player.Speed = -hitSpeed * DoublePoint.FromAngle(ballSpeedAngle - velocityAttackAngle);
                player.Angle = Math.PI + ballSpeedAngle - attackAngle;

                var newAim = new Aim(player, this, attemptState.Time, initialTime);
                if (newAim.HasTimeToReact)
                {
                    var simulation = new Simulation(attemptState);
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
