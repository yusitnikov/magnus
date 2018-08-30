using Magnus.Strategies;
using System;

namespace Magnus
{
    class Player : ASurface
    {
        public int Index;

        public int Side => Misc.GetPlayerSideByIndex(Index);

        public int Score;

        public Strategy Strategy;

        public double AnglePitch, AngleYaw;

        public override DoublePoint3D Normal
        {
            get => TranslateVectorFromBatCoords(DoublePoint3D.YAxis);
            set
            {
                AnglePitch = value.Pitch;
                AngleYaw = value.Yaw;
            }
        }

        public DoublePoint3D DefaultNormal => new DoublePoint3D(-Side, 0, 0);

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
            Position = Speed = DoublePoint3D.Empty;
            Normal = DefaultNormal;
            NeedAim = false;
            Aim = null;
        }

        public DoublePoint3D TranslateVectorFromBatCoords(DoublePoint3D point)
        {
            return point.RotatePitch(AnglePitch).RotateYaw(AngleYaw);
        }

        public DoublePoint3D TranslatePointFromBatCoords(DoublePoint3D point)
        {
            return Position + TranslateVectorFromBatCoords(point);
        }

        public void ResetPosition(double x, bool resetAngle)
        {
            if (resetAngle)
            {
                Normal = DefaultNormal;
            }

            Position = new DoublePoint3D(x * Side, Constants.BatWaitY, 0);
            Speed = DoublePoint3D.Empty;
        }

        public void ResetAim()
        {
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
                AnglePitch = AnglePitch,
                AngleYaw = AngleYaw,
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

                if (!stillMoving || state.GameState == GameState.Failed && Math.Abs(Aim.AimX) < getWaitX(state, false))
                {
                    MoveToInitialPosition(state, false);
                }
            }

            Speed = (Position - prevPosition) / dt;
        }

        private double getWaitX(State state, bool prepareToServe)
        {
            if (state.GameState == GameState.Failed)
            {
                prepareToServe = true;
            }
            return prepareToServe ? state.NextServeX + Constants.BatLength : Constants.BatWaitX;
        }

        public void MoveToInitialPosition(State state, bool prepareToServe)
        {
            var readyPosition = Clone();
            readyPosition.ResetPosition(getWaitX(state, prepareToServe), false);
            readyPosition.ResetAim();
            NeedAim = false;
            Aim = new Aim(readyPosition, this, double.PositiveInfinity, state.Time);
        }

        public void RequestAim()
        {
            NeedAim = true;
        }

        public void FindHit(State state)
        {
            var initialState = state;

            if (state.GameState == GameState.Failed || Index != state.HitSide)
            {
                MoveToInitialPosition(initialState, false);
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
                    MoveToInitialPosition(initialState, true);
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

                var ballReverseSpeed = -attemptState.Ball.Speed;
                double reverseBallSpeedPitch = ballReverseSpeed.Pitch;
                double reverseBallSpeedYaw = ballReverseSpeed.Yaw;
                if (attemptState.Ball.Speed.X == 0 && attemptState.Ball.Speed.Z == 0)
                {
                    // Yaw for vertical vector is undefined, so use default bat direction instead
                    reverseBallSpeedYaw = DefaultNormal.Yaw;
                }

                double hitSpeed, attackPitch, attackYaw, velocityAttackPitch, velocityAttackYaw;
                if (isServing)
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Strategy.GetServeHitSpeed(Math.Abs(attemptState.Ball.Position.X));
                    attackPitch = Strategy.GetServeAttackAngle();
                    velocityAttackPitch = Strategy.GetServeVelocityAttackAngle();
                    attackYaw = Misc.FromDegrees(Misc.Rnd(-40, 40));
                    velocityAttackYaw = Math.Sign(Misc.Rnd(-1, 1)) * Misc.FromDegrees(Misc.Rnd(30, 70));
                }
                else
                {
                    hitSpeed = Constants.MaxPlayerSpeed * Strategy.GetHitSpeed();
                    attackPitch = Strategy.GetAttackAngle();
                    velocityAttackPitch = Strategy.GetVelocityAttackAngle();
                    velocityAttackPitch = Math.Max(velocityAttackPitch, attackPitch - Constants.MaxAttackAngleDifference);
                    velocityAttackPitch = Math.Min(velocityAttackPitch, attackPitch + Constants.MaxAttackAngleDifference);
                    attackYaw = Misc.FromDegrees(Misc.Rnd(-50, 50));
                    velocityAttackYaw = Misc.FromDegrees(Misc.Rnd(-60, 60));
                }
                //attackYaw = velocityAttackYaw = 0;

                player.AngleYaw = reverseBallSpeedYaw + attackYaw;
                player.AnglePitch = reverseBallSpeedPitch + attackPitch;
                player.Position = attemptState.Ball.Position - Constants.BallRadius * player.Normal;
                player.Speed = hitSpeed * DoublePoint3D.FromAngles(reverseBallSpeedPitch + velocityAttackPitch, reverseBallSpeedYaw + attackYaw + velocityAttackYaw);
                if (player.Position.Z * player.Side > 0)
                {
                    player.AnglePitch += Math.PI;
                }
                else
                {
                    player.AngleYaw += Math.PI;
                    player.AnglePitch = -player.AnglePitch;
                }

                var newAim = new Aim(player, this, attemptState.Time, initialState.Time);
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
