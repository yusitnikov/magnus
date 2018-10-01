using Mathematics;
using Mathematics.Expressions;
using Mathematics.Math3D;
using System;
using System.Collections.Generic;

namespace Magnus
{
    class HitSearcher
    {
        const double tableHitXAimMedian = Constants.HalfTableLength / 2;
        const double tableHitXAimCoeff = Constants.HalfTableLength / 2 - Constants.SimulationBordersMargin;
        const double tableHitZAimCoeff = Constants.HalfTableWidth - Constants.SimulationBordersMargin;
        const double netCrossYAimMedian = (Constants.MaxBallMaxHeight + Constants.MinNetCrossY) / 2;
        const double netCrossYAimCoeff = (Constants.MaxBallMaxHeight - Constants.MinNetCrossY) / 2;
        const double maxHeightAimMedian = netCrossYAimMedian;
        const double maxHeightAimCoeff = netCrossYAimCoeff;

        private State initialState, state;
        private Player player;
        private List<State> states;
        private double minHitTime, maxHitTime;

        private Variable hitTimeVar = new Variable("FT");
        private Variable hitSpeedVar = new Variable("HS");
        private Variable attackPitchVar = new Variable("AP");
        private Variable velocityAttackPitchVar = new Variable("VAP");
        private Variable attackYawVar = new Variable("AY");
        private Variable velocityAttackYawVar = new Variable("VAY");

        private bool isServing;
        private int playerSide;
        private BallExpression initialBallExpression;
        private BallExpression[] initialBallExpressionDerivatives;
        private Player aimPlayer;
        private PlayerExpression playerExpression;
        private PlayerExpression[] playerExpressionDerivatives;
        private Player[] playerDerivatives;
        private Variable[] optimizationVariables;
        private int optimizationVariablesCount;
        private VariableLimitation[] optimizationLimitations;

        private Ball initialBallState, ballAtTableHitTime;
        private Ball.SimplifiedStepDerivatives initialBallDerivatives, ballDerivativesAtTableHitTime;
        private double serveTableHitTime, tableHitTime, netCrossTime, maxHeightTime;
        private double serveTableHitX, serveTableHitZ, tableHitX, tableHitZ, netCrossY, maxHeight;
        private Point3D aimCriteria1, aimCriteria2;

        private bool hasTimeToReact;

        public HitSearcher(State state, Player player)
        {
            initialState = state;
            this.state = state.Clone(true);
            this.player = player;
        }

        public bool Initialize()
        {
            isServing = state.GameState == GameState.Serving;
            playerSide = player.Side;

            if (state.GameState == GameState.Failed || player.Index != state.HitSide)
            {
                player.MoveToInitialPosition(initialState, false);
                return false;
            }

            states = new List<State>();

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
                    player.MoveToInitialPosition(initialState, true);
                    return false;
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

                minHitTime = state.Time + player.Strategy.GetMinBackHitTime(ballMaxHeightTime, ballFallTime);
                maxHitTime = state.Time + player.Strategy.GetMaxBackHitTime(ballMaxHeightTime, ballFallTime);
            }

            var statesCount = states.Count;
            if (statesCount < 2)
            {
                return false;
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

            optimizationVariables = new Variable[] { hitTimeVar, hitSpeedVar, attackPitchVar, attackYawVar, velocityAttackPitchVar, velocityAttackYawVar };
            optimizationVariablesCount = optimizationVariables.Length;
            optimizationLimitations = new VariableLimitation[]
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

            var ballForceExpression = FunctionByPoints.Create3DFunctionByPoints("BallForce", hitTimeVar, ballForces, state.Time, Constants.SimulationFrameTime);
            var ballSpeedExpression = FunctionByPoints.Create3DFunctionByPoints("BallSpeed", hitTimeVar, ballSpeeds, state.Time, Constants.SimulationFrameTime, ballForceExpression);
            var ballPositionExpression = FunctionByPoints.Create3DFunctionByPoints("BallPosition", hitTimeVar, ballPositions, state.Time, Constants.SimulationFrameTime, ballSpeedExpression);
            var ballAngularForceExpression = FunctionByPoints.Create3DFunctionByPoints("BallAngularForce", hitTimeVar, ballAngularForces, state.Time, Constants.SimulationFrameTime);
            var ballAngularSpeedExpression = FunctionByPoints.Create3DFunctionByPoints("BallAngularSpeed", hitTimeVar, ballAngularSpeeds, state.Time, Constants.SimulationFrameTime, ballAngularForceExpression);
            initialBallExpression = new BallExpression(ballPositionExpression, ballSpeedExpression, ballAngularSpeedExpression);

            var ballReverseSpeed = -initialBallExpression.Speed;
            var reverseBallSpeedPitch = ballReverseSpeed.Pitch;
            var reverseBallSpeedYaw = ballReverseSpeed.Yaw;
            if (isServing)
            {
                // Yaw for vertical vector is undefined, so use default bat direction instead
                reverseBallSpeedYaw = player.DefaultNormal.Yaw;
            }

            Expression minVelocityAttackPitch, maxVelocityAttackPitch;
            double minHitSpeed, maxHitSpeed, minAttackPitch, maxAttackPitch, maxAttackYaw, maxVelocityAttackYaw;
            if (isServing)
            {
                var ballX = Math.Abs(state.Ball.Position.X);
                minHitSpeed = Constants.MaxPlayerSpeed * player.Strategy.GetMinServeHitSpeed(ballX);
                maxHitSpeed = Constants.MaxPlayerSpeed * player.Strategy.GetMaxServeHitSpeed(ballX);
                minAttackPitch = player.Strategy.GetMinServeAttackAngle();
                maxAttackPitch = player.Strategy.GetMaxServeAttackAngle();
                minVelocityAttackPitch = player.Strategy.GetMinServeVelocityAttackAngle();
                maxVelocityAttackPitch = player.Strategy.GetMaxServeVelocityAttackAngle();
                maxAttackYaw = Misc.FromDegrees(40);
                maxVelocityAttackYaw = Misc.FromDegrees(70);
            }
            else
            {
                minHitSpeed = Constants.MaxPlayerSpeed * player.Strategy.GetMinHitSpeed();
                maxHitSpeed = Constants.MaxPlayerSpeed * player.Strategy.GetMaxHitSpeed();
                minAttackPitch = player.Strategy.GetMinAttackAngle();
                maxAttackPitch = player.Strategy.GetMaxAttackAngle();
                minVelocityAttackPitch = player.Strategy.GetMinVelocityAttackAngle();
                maxVelocityAttackPitch = player.Strategy.GetMaxVelocityAttackAngle();
                maxAttackYaw = Misc.FromDegrees(50);
                maxVelocityAttackYaw = Misc.FromDegrees(60);
            }
            //maxAttackYaw = maxVelocityAttackYaw = 0.0001;

            var hitSpeed = minHitSpeed + hitSpeedVar * (maxHitSpeed - minHitSpeed);
            var attackPitch = minAttackPitch + attackPitchVar * (maxAttackPitch - minAttackPitch);
            if (!isServing)
            {
                minVelocityAttackPitch = Expression.Max(minVelocityAttackPitch, attackPitch - Constants.MaxAttackAngleDifference);
                maxVelocityAttackPitch = Expression.Min(maxVelocityAttackPitch, attackPitch + Constants.MaxAttackAngleDifference);
            }
            var velocityAttackPitch = minVelocityAttackPitch + velocityAttackPitchVar * (maxVelocityAttackPitch - minVelocityAttackPitch);
            var attackYaw = attackYawVar * maxAttackYaw;
            var velocityAttackYaw = velocityAttackYawVar * maxVelocityAttackYaw;

            playerExpression = new PlayerExpression()
            {
                Index = player.Index,
                AnglePitch = reverseBallSpeedPitch + attackPitch,
                AngleYaw = reverseBallSpeedYaw + attackYaw,
                Speed = hitSpeed * Point3DExpression.FromAngles(reverseBallSpeedPitch + velocityAttackPitch, reverseBallSpeedYaw + attackYaw + velocityAttackYaw)
            };
            playerExpression.Position = initialBallExpression.Position - Constants.BallRadius * playerExpression.Normal;

            initialBallExpression.ProcessHit(playerExpression, Constants.BallHitHorizontalCoeff, Constants.BallHitVerticalCoeff);

            initialBallExpressionDerivatives = new BallExpression[optimizationVariablesCount];
            playerExpressionDerivatives = new PlayerExpression[optimizationVariablesCount];
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                initialBallExpressionDerivatives[i] = initialBallExpression.Derivate(optimizationVariables[i]);
                playerExpressionDerivatives[i] = playerExpression.Derivate(optimizationVariables[i]);
            }

            return true;
        }

        public Aim Search()
        {
            hitTimeVar.Value = Misc.Rnd(minHitTime, maxHitTime);
            hitSpeedVar.Value = Misc.Rnd(0, 1);
            attackPitchVar.Value = Misc.Rnd(0, 1);
            velocityAttackPitchVar.Value = Misc.Rnd(0, 1);
            attackYawVar.Value = Misc.Rnd(-1, 1);
            velocityAttackYawVar.Value = Misc.Rnd(-1, 1);

            var it = 0;
            while (true)
            {
                ++it;
                if (it > 100)
                {
                    return null;
                }

                GradientSearch.EvalResult? cachedResult = evaluate();
                if (double.IsNaN(tableHitTime))
                {
                    // It shouldn't happen
                    return null;
                }

                var done = hasTimeToReact;
                if (tableHitX <= 0 || isServing && serveTableHitX <= 0)
                {
                    done = false;
                }
                else
                {
                    done = done && (!isServing || serveTableHitX <= Constants.HalfTableLength - Constants.SimulationBordersMargin / 3);

                    done = done && (
                        tableHitX <= Constants.HalfTableLength - Constants.SimulationBordersMargin &&
                        tableHitX >= Constants.NetHeight &&
                        (tableHitX >= Constants.SimulationNetMargin || Point3D.VectorMult(initialBallState.Speed, initialBallState.AngularSpeed).Y < 0)
                    );

                    if (double.IsNaN(netCrossTime))
                    {
                        // It shouldn't happen
                        return null;
                    }
                    done = done && netCrossY >= Constants.MinNetCrossY;
                }

                done = done && (!isServing || Math.Abs(serveTableHitZ) <= Constants.HalfTableWidth - Constants.SimulationBordersMargin);

                done = done && Math.Abs(tableHitZ) <= Constants.HalfTableWidth - Constants.SimulationBordersMargin;

                done = done && (double.IsNaN(maxHeightTime) || maxHeight <= Constants.MaxBallMaxHeight);

                if (done)
                {
                    var aimPlayer = playerExpression.Evaluate();
                    if (aimPlayer.Position.Z * aimPlayer.Side > 0)
                    {
                        aimPlayer.AnglePitch += Math.PI;
                    }
                    else
                    {
                        aimPlayer.AngleYaw += Math.PI;
                        aimPlayer.AnglePitch = -aimPlayer.AnglePitch;
                    }

                    Aim aim = new AimByGracefulMovement(aimPlayer, player, hitTimeVar.Value, initialState.Time);
                    if (!aim.HasTimeToReact)
                    {
                        aim = new AimByCoords(aimPlayer, player, hitTimeVar.Value, initialState.Time);
                    }

                    if (!aim.HasTimeToReact)
                    {
                        // It shoudn't happen
                        return null;
                    }

                    return aim;
                }

                var success = GradientSearch.Step(
                    () =>
                    {
                        // cached result for the first evaluation only
                        var result = cachedResult ?? evaluate();
                        cachedResult = null;
                        return result;
                    },
                    optimizationVariables, optimizationLimitations, 0.3, 0.3
                );
                if (!success)
                {
                    // It shouldn't happen
                    return null;
                }
            }
        }

        private GradientSearch.EvalResult evaluate()
        {
            #region Initialization
            var result = new GradientSearch.EvalResult()
            {
                Value = 0,
                Derivatives = new double[optimizationVariablesCount]
            };

            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                result.Derivatives[i] = 0;
            }

            var cacheGeneration = Expression.NextAutoIncrementId;
            initialBallState = initialBallExpression.Evaluate(cacheGeneration);
            initialBallDerivatives = new Ball.SimplifiedStepDerivatives()
            {
                VarsCount = optimizationVariablesCount,
                RelativeBallStateDv = new Ball[optimizationVariablesCount]
            };
            aimPlayer = playerExpression.Evaluate(cacheGeneration);
            playerDerivatives = new Player[optimizationVariablesCount];
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                initialBallDerivatives.RelativeBallStateDv[i] = initialBallExpressionDerivatives[i].Evaluate(cacheGeneration);
                playerDerivatives[i] = playerExpressionDerivatives[i].Evaluate(cacheGeneration);
            }
            #endregion

            #region hasTimeToReact
            hasTimeToReact = true;
            evaluateHasTimeToReact(ref result, p => p.X, (p, v) => { p.X = v; return p; });
            evaluateHasTimeToReact(ref result, p => p.Y, (p, v) => { p.Y = v; return p; });
            evaluateHasTimeToReact(ref result, p => p.Z, (p, v) => { p.Z = v; return p; });
            #endregion

            #region tableHitPos
            if (!evaluateTableHitPos(ref result, isServing ? 1 : -1))
            {
                return result;
            }

            if (isServing)
            {
                serveTableHitTime = tableHitTime;
                serveTableHitX = tableHitX;
                serveTableHitZ = tableHitZ;
                initialBallState = ballAtTableHitTime;
                initialBallDerivatives = ballDerivativesAtTableHitTime;
                initialBallState.ProcessTableHit(1, initialBallDerivatives.RelativeBallStateDv);

                if (!evaluateTableHitPos(ref result, -1))
                {
                    return result;
                }
            }
            #endregion

            #region netCrossY
            if (tableHitX <= 0 || isServing && serveTableHitX <= 0)
            {
                netCrossTime = netCrossY = 0;
            }
            else
            {
                Func<double, double> netCrossCriteriaCalculator = time => playerSide * Ball.DoStepSimplified(initialBallState, time, Ball.Component.HorizontalPosition).Position.X;
                netCrossTime = BinarySearch.Search(0, tableHitTime, 0.1 * Constants.TimeUnit, time => netCrossCriteriaCalculator(time) < 0, netCrossCriteriaCalculator);
                initialBallDerivatives.TDv = null;
                var ballAtNetCrossTime = Ball.DoStepSimplified(initialBallState, netCrossTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives simpleBallDerivativesAtNetCrossTime, Ball.Component.HorizontalSpeed | Ball.Component.Position);
                initialBallDerivatives.TDv = new double[optimizationVariablesCount];
                for (var i = 0; i < optimizationVariablesCount; i++)
                {
                    var bdv = simpleBallDerivativesAtNetCrossTime.RelativeBallStateDv[i];
                    initialBallDerivatives.TDv[i] = -bdv.Position.X / ballAtNetCrossTime.Speed.X;
                }
                Ball.DoStepSimplified(initialBallState, netCrossTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives ballDerivativesAtNetCrossTime, Ball.Component.VerticalPosition);
                netCrossY = ballAtNetCrossTime.Position.Y;

                var criteriaValue = (netCrossY - netCrossYAimMedian) / netCrossYAimCoeff;
                result.Value += criteriaValue * criteriaValue;
                var derivativeCoeff = 2 * criteriaValue / netCrossYAimCoeff;
                for (var i = 0; i < optimizationVariablesCount; i++)
                {
                    result.Derivatives[i] += derivativeCoeff * ballDerivativesAtNetCrossTime.RelativeBallStateDv[i].Position.Y;
                }
            }
            #endregion

            #region maxHeight
            Func<double, double> maxHeightCriteriaCalculator = time => Ball.DoStepSimplified(initialBallState, time, Ball.Component.VerticalSpeed).Speed.Y;
            maxHeightTime = BinarySearch.Search(0, Constants.TimeUnit, 0.1 * Constants.TimeUnit, time => maxHeightCriteriaCalculator(time) < 0, maxHeightCriteriaCalculator);
            if (!double.IsNaN(maxHeightTime))
            {
                initialBallDerivatives.TDv = null;
                var ballAtMaxHeightTime = Ball.DoStepSimplified(
                    initialBallState, maxHeightTime,
                    initialBallDerivatives, out Ball.SimplifiedStepDerivatives simpleBallDerivativesAtMaxHeightTime,
                    Ball.Component.AngularSpeed | Ball.Component.Speed | Ball.Component.VerticalPosition
                );
                initialBallDerivatives.TDv = new double[optimizationVariablesCount];
                for (var i = 0; i < optimizationVariablesCount; i++)
                {
                    var bdv = simpleBallDerivativesAtMaxHeightTime.RelativeBallStateDv[i];
                    initialBallDerivatives.TDv[i] = -bdv.Speed.Y / ballAtMaxHeightTime.Force.Y;
                }
                Ball.DoStepSimplified(initialBallState, maxHeightTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives ballDerivativesAtMaxHeightTime, Ball.Component.VerticalPosition);
                maxHeight = ballAtMaxHeightTime.Position.Y;

                var criteriaValue = (maxHeight - maxHeightAimMedian) / maxHeightAimCoeff;
                result.Value += criteriaValue * criteriaValue;
                var derivativeCoeff = 2 * criteriaValue / maxHeightAimCoeff;
                for (var i = 0; i < optimizationVariablesCount; i++)
                {
                    result.Derivatives[i] += derivativeCoeff * ballDerivativesAtMaxHeightTime.RelativeBallStateDv[i].Position.Y;
                }
            }
            else
            {
                maxHeight = 0;
            }
            #endregion

            return result;
        }

        private void evaluateHasTimeToReact(ref GradientSearch.EvalResult result, Func<Point3D, double> coordPicker, Func<Point3D, double, Point3D> coordSetter)
        {
            const double a = Constants.MaxPlayerForce;

            var t = hitTimeVar.Value;
            var x1 = coordPicker(player.Position);
            var x2 = coordPicker(aimPlayer.Position);
            var v1 = coordPicker(player.Speed);
            var v2 = coordPicker(aimPlayer.Speed);

            var z1 = a * t / 2;
            var z2 = (v2 - v1) / 2;
            var z3 = z1 * z1 - z2 - z2;
            var z4 = a * (x1 - x2) + (v1 + v2) * z1;

            if (z3 < Math.Abs(z4))
            {
                hasTimeToReact = false;
            }

            var criteriaValue1 = z2 / z1;
            var criteriaValue2 = z4 / z3;
            aimCriteria1 = coordSetter(aimCriteria1, criteriaValue1);
            aimCriteria2 = coordSetter(aimCriteria2, criteriaValue2);
            result.Value += criteriaValue1 * criteriaValue1 + criteriaValue2 * criteriaValue2;
            var derivativeCoeff1 = 2 * criteriaValue1 / z1;
            var derivativeCoeff2 = 2 * criteriaValue2 / z3;

            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                var dt = hitTimeVar == optimizationVariables[i] ? 1 : 0;
                var dx2 = coordPicker(playerDerivatives[i].Position);
                var dv2 = coordPicker(playerDerivatives[i].Speed);

                var dz1 = a * dt / 2;
                var dz2 = dv2 / 2;
                var dz3 = 2 * (z1 * dz1 - z2 - dz2);
                var dz4 = -a * dx2 + dv2 * z1 + (v1 + v2) * dz1;

                result.Derivatives[i] += derivativeCoeff1 * (dz2 - criteriaValue1 * dz1) + derivativeCoeff2 * (dz4 - criteriaValue2 * dz3);
            }
        }

        private bool evaluateTableHitPos(ref GradientSearch.EvalResult result, int sideCoeff)
        {
            tableHitTime = BinarySearch.Search(
                0, Constants.TimeUnit, Constants.SimplifiedSimulationFrameTime,
                time =>
                {
                    var ball = Ball.DoStepSimplified(initialBallState, time, Ball.Component.VerticalSpeed | Ball.Component.VerticalPosition);
                    return ball.Speed.Y < 0 && ball.Position.Y - Constants.BallRadius < 0;
                },
                time => Ball.DoStepSimplified(initialBallState, time, Ball.Component.VerticalPosition).Position.Y - Constants.BallRadius
            );
            if (double.IsNaN(tableHitTime))
            {
                return false;
            }
            initialBallDerivatives.TDv = null;
            ballAtTableHitTime = Ball.DoStepSimplified(initialBallState, tableHitTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives simpleBallDerivativesAtTableHitTime, Ball.Component.All);
            initialBallDerivatives.TDv = new double[optimizationVariablesCount];
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                var bdv = simpleBallDerivativesAtTableHitTime.RelativeBallStateDv[i];
                initialBallDerivatives.TDv[i] = -bdv.Position.Y / ballAtTableHitTime.Speed.Y;
            }
            Ball.DoStepSimplified(initialBallState, tableHitTime, initialBallDerivatives, out ballDerivativesAtTableHitTime, Ball.Component.All);

            double criteriaValue, derivativeCoeff;

            #region tableHitX
            tableHitX = sideCoeff * playerSide * ballAtTableHitTime.Position.X;

            criteriaValue = (tableHitX - tableHitXAimMedian) / tableHitXAimCoeff;
            result.Value += criteriaValue * criteriaValue;
            derivativeCoeff = 2 * sideCoeff * playerSide * criteriaValue / tableHitXAimCoeff;
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                result.Derivatives[i] += derivativeCoeff * ballDerivativesAtTableHitTime.RelativeBallStateDv[i].Position.X;
            }
            #endregion

            #region tableHitZ
            tableHitZ = ballAtTableHitTime.Position.Z;

            criteriaValue = tableHitZ / tableHitZAimCoeff;
            result.Value += criteriaValue * criteriaValue;
            derivativeCoeff = 2 * criteriaValue / tableHitZAimCoeff;
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                result.Derivatives[i] += derivativeCoeff * ballDerivativesAtTableHitTime.RelativeBallStateDv[i].Position.Z;
            }
            #endregion

            return true;
        }
    }
}
