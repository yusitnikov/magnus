using Magnus.Strategies;
using Mathematics.Expressions;
using Mathematics.Math3D;
using System;
using System.Collections.Generic;

namespace Magnus
{
    class Player : ASurface
    {
        public static double LastSearchTime = 0;

        public int Index;

        public int Side => Misc.GetPlayerSideByIndex(Index);

        public int Score;

        public Strategy Strategy;

        public double AnglePitch, AngleYaw;

        public override Point3D Normal
        {
            get => TranslateVectorFromBatCoords(Point3D.YAxis);
            set
            {
                AnglePitch = value.Pitch;
                AngleYaw = value.Yaw;
            }
        }

        public Point3D DefaultNormal => new Point3D(-Side, 0, 0);

        public bool NeedAim;
        public Aim Aim;

        internal Player()
        {
        }

        public Player(int index)
        {
            Index = index;
            Score = 0;
            Strategy = new Strategy();
            Position = Speed = Point3D.Empty;
            Normal = DefaultNormal;
            NeedAim = false;
            Aim = null;
        }

        public Point3D TranslateVectorFromBatCoords(Point3D point)
        {
            return point.RotatePitch(AnglePitch).RotateYaw(AngleYaw);
        }

        public Point3D TranslatePointFromBatCoords(Point3D point)
        {
            return Position + TranslateVectorFromBatCoords(point);
        }

        public void ResetPosition(double x, bool resetAngle)
        {
            if (resetAngle)
            {
                Normal = DefaultNormal;
            }

            Position = new Point3D(x * Side, Constants.BatWaitY, 0);
            Speed = Point3D.Empty;
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

                if (!stillMoving || state.GameState == GameState.Failed && Math.Abs(Aim.AimPlayer.Position.X) < getWaitX(state, false))
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

            double minHitTime, maxHitTime;
            var states = new List<State>();

            if (isServing)
            {
                while (state.Ball.Speed.Y >= 0 || state.Ball.Position.Y >= Constants.MaxBallServeY)
                {
                    state.DoStep();
                }

                var attemptState = state.Clone(false);
                while (attemptState.Ball.Position.Y >= Constants.MinBallServeY)
                {
                    states.Add(attemptState.Clone(false));
                    attemptState.DoStep();
                }

                states.Add(attemptState.Clone(false));

                minHitTime = state.Time;
                maxHitTime = attemptState.Time;
            }
            else
            {
                if (!state.DoStepsUntilGameState(GameState.FlyingToBat))
                {
                    MoveToInitialPosition(initialState, true);
                    return;
                }

                var attemptState = state.Clone(false);

                // Calculate time till max height
                while (attemptState.GameState != GameState.Failed)
                {
                    states.Add(attemptState.Clone(false));
                    if (attemptState.DoStep().HasOneOfEvents(Event.MaxHeight))
                    {
                        break;
                    }
                }
                double ballMaxHeightTime = attemptState.Time - state.Time;

                // Calculate time till ball fall
                while (attemptState.GameState != GameState.Failed)
                {
                    states.Add(attemptState.Clone(false));
                    if (attemptState.DoStep().HasOneOfEvents(Event.LowHit))
                    {
                        break;
                    }
                }
                double ballFallTime = attemptState.Time - state.Time;

                states.Add(attemptState.Clone(false));

                minHitTime = state.Time + Strategy.GetMinBackHitTime(ballMaxHeightTime, ballFallTime);
                maxHitTime = state.Time + Strategy.GetMaxBackHitTime(ballMaxHeightTime, ballFallTime);
            }

            var statesCount = states.Count;
            if (statesCount < 2)
            {
                return;
            }

            var ballPositions = new Point3D[statesCount];
            var ballSpeeds = new Point3D[statesCount];
            var ballForces = new Point3D[statesCount];
            var ballAngularSpeeds = new Point3D[statesCount];
            var ballAngularForces = new Point3D[statesCount];
            for (var index = 0; index < statesCount; index++)
            {
                var ball = states[index].Ball;
                ballPositions[index] = ball.Position;
                ballSpeeds[index] = ball.Speed;
                ballForces[index] = ball.Force;
                ballAngularSpeeds[index] = ball.AngularSpeed;
                ballAngularForces[index] = ball.AngularForce;
            }
            var hitTimeVar = new Variable("FT");
            var ballForceExpression = FunctionByPoints.Create3DFunctionByPoints("BallForce", hitTimeVar, ballForces, state.Time, Constants.SimulationFrameTime);
            var ballSpeedExpression = FunctionByPoints.Create3DFunctionByPoints("BallSpeed", hitTimeVar, ballSpeeds, state.Time, Constants.SimulationFrameTime, ballForceExpression);
            var ballPositionExpression = FunctionByPoints.Create3DFunctionByPoints("BallPosition", hitTimeVar, ballPositions, state.Time, Constants.SimulationFrameTime, ballSpeedExpression);
            var ballAngularForceExpression = FunctionByPoints.Create3DFunctionByPoints("BallAngularForce", hitTimeVar, ballAngularForces, state.Time, Constants.SimulationFrameTime);
            var ballAngularSpeedExpression = FunctionByPoints.Create3DFunctionByPoints("BallAngularSpeed", hitTimeVar, ballAngularSpeeds, state.Time, Constants.SimulationFrameTime, ballAngularForceExpression);
            var hitTimeBallExpression = new BallExpression(ballPositionExpression, ballSpeedExpression, ballAngularSpeedExpression);

            var iterations = 0;
            PlayerExpression playerExpression = new PlayerExpression()
            {
                Index = Index
            };
            while (NeedAim && (DateTime.Now - searchStartTime).TotalSeconds < Constants.MaxThinkTimePerFrame)
            {
                ++iterations;

                var attemptState = state.Clone(false);
                attemptState.Time = hitTimeVar.Value = Misc.Rnd(minHitTime, maxHitTime);
                attemptState.Ball = hitTimeBallExpression.Evaluate();

                var ballReverseSpeed = -hitTimeBallExpression.Speed;
                var reverseBallSpeedPitch = ballReverseSpeed.Pitch;
                var reverseBallSpeedYaw = ballReverseSpeed.Yaw;
                if (isServing)
                {
                    // Yaw for vertical vector is undefined, so use default bat direction instead
                    reverseBallSpeedYaw = DefaultNormal.Yaw;
                }

                Expression minVelocityAttackPitch, maxVelocityAttackPitch;
                double minHitSpeed, maxHitSpeed, minAttackPitch, maxAttackPitch, maxAttackYaw, maxVelocityAttackYaw;
                if (isServing)
                {
                    var ballX = Math.Abs(state.Ball.Position.X);
                    minHitSpeed = Constants.MaxPlayerSpeed * Strategy.GetMinServeHitSpeed(ballX);
                    maxHitSpeed = Constants.MaxPlayerSpeed * Strategy.GetMaxServeHitSpeed(ballX);
                    minAttackPitch = Strategy.GetMinServeAttackAngle();
                    maxAttackPitch = Strategy.GetMaxServeAttackAngle();
                    minVelocityAttackPitch = Strategy.GetMinServeVelocityAttackAngle();
                    maxVelocityAttackPitch = Strategy.GetMaxServeVelocityAttackAngle();
                    maxAttackYaw = Misc.FromDegrees(40);
                    maxVelocityAttackYaw = Misc.FromDegrees(70);
                }
                else
                {
                    minHitSpeed = Constants.MaxPlayerSpeed * Strategy.GetMinHitSpeed();
                    maxHitSpeed = Constants.MaxPlayerSpeed * Strategy.GetMaxHitSpeed();
                    minAttackPitch = Strategy.GetMinAttackAngle();
                    maxAttackPitch = Strategy.GetMaxAttackAngle();
                    minVelocityAttackPitch = Strategy.GetMinVelocityAttackAngle();
                    maxVelocityAttackPitch = Strategy.GetMaxVelocityAttackAngle();
                    maxAttackYaw = Misc.FromDegrees(50);
                    maxVelocityAttackYaw = Misc.FromDegrees(60);
                }
                //maxAttackYaw = maxVelocityAttackYaw = 0.0001;

                var hitSpeedVar = new Variable("HS", Misc.Rnd(0, 1));
                var hitSpeed = minHitSpeed + hitSpeedVar * (maxHitSpeed - minHitSpeed);
                var attackPitchVar = new Variable("AP", Misc.Rnd(0, 1));
                var attackPitch = minAttackPitch + attackPitchVar * (maxAttackPitch - minAttackPitch);
                if (!isServing)
                {
                    minVelocityAttackPitch = Expression.Max(minVelocityAttackPitch, attackPitch - Constants.MaxAttackAngleDifference);
                    maxVelocityAttackPitch = Expression.Min(maxVelocityAttackPitch, attackPitch + Constants.MaxAttackAngleDifference);
                }
                var velocityAttackPitchVar = new Variable("VAP", Misc.Rnd(0, 1));
                var velocityAttackPitch = minVelocityAttackPitch + velocityAttackPitchVar * (maxVelocityAttackPitch - minVelocityAttackPitch);
                var attackYawVar = new Variable("AY", Misc.Rnd(-1, 1));
                var attackYaw = attackYawVar * maxAttackYaw;
                var velocityAttackYawVar = new Variable("VAY", Misc.Rnd(-1, 1));
                var velocityAttackYaw = velocityAttackYawVar * maxVelocityAttackYaw;

                var optimizationVariables = new Variable[] { hitTimeVar, hitSpeedVar, attackPitchVar, attackYawVar, velocityAttackPitchVar, velocityAttackYawVar };
                var optimizationLimitations = new VariableLimitation[]
                {
                    new VariableLimitation() { Variable = hitTimeVar, Limit = minHitTime, Sign = -1 },
                    new VariableLimitation() { Variable = hitTimeVar, Limit = maxHitTime, Sign = 1 },
                    new VariableLimitation() { Variable = hitSpeedVar, Limit = 0, Sign = -1 },
                    new VariableLimitation() { Variable = hitSpeedVar, Limit = 1, Sign = 1 },
                    new VariableLimitation() { Variable = attackPitchVar, Limit = 0, Sign = -1 },
                    new VariableLimitation() { Variable = attackPitchVar, Limit = 1, Sign = 1 },
                    new VariableLimitation() { Variable = velocityAttackPitchVar, Limit = 0, Sign = -1 },
                    new VariableLimitation() { Variable = velocityAttackPitchVar, Limit = 1, Sign = 1 },
                    new VariableLimitation() { Variable = attackYawVar, Limit = -1, Sign = -1 },
                    new VariableLimitation() { Variable = attackYawVar, Limit = 1, Sign = 1 },
                    new VariableLimitation() { Variable = velocityAttackYawVar, Limit = -1, Sign = -1 },
                    new VariableLimitation() { Variable = velocityAttackYawVar, Limit = 1, Sign = 1 },
                };

                playerExpression.AnglePitch = reverseBallSpeedPitch + attackPitch;
                playerExpression.AngleYaw = reverseBallSpeedYaw + attackYaw;
                playerExpression.Position = hitTimeBallExpression.Position - Constants.BallRadius * playerExpression.Normal;
                playerExpression.Speed = hitSpeed * Point3DExpression.FromAngles(reverseBallSpeedPitch + velocityAttackPitch, reverseBallSpeedYaw + attackYaw + velocityAttackYaw);

                var newAim = new Aim(playerExpression.Evaluate(), this, hitTimeVar.Value, initialState.Time);
                if (newAim.HasTimeToReact)
                {
                    var ballExpression = hitTimeBallExpression.Clone();
                    ballExpression.ProcessHit(playerExpression, Constants.BallHitHorizontalCoeff, Constants.BallHitVerticalCoeff);
                    var hitSearcher = new HitSearcher(isServing, Side, ballExpression, optimizationVariables, optimizationLimitations);
                    if (hitSearcher.Search())
                    {
                        NeedAim = false;
                        Aim = new Aim(playerExpression.Evaluate(), this, hitTimeVar.Value, initialState.Time);
                    }
                }
            }
            if (!NeedAim)
            {
                var player = Aim.AimPlayer;
                if (player.Position.Z * player.Side > 0)
                {
                    player.AnglePitch += Math.PI;
                }
                else
                {
                    player.AngleYaw += Math.PI;
                    player.AnglePitch = -player.AnglePitch;
                }
                Aim = new Aim(player, this, Aim.AimT, Aim.AimT0);
            }
            LastSearchTime = (DateTime.Now - searchStartTime).TotalSeconds * 1000000 / iterations;
        }
    }
}
