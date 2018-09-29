using Mathematics;
using Mathematics.Expressions;
using Mathematics.Math3D;
using System;

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

        private int playerSide;
        private BallExpression initialBallExpression;
        private BallExpression[] initialBallExpressionDerivatives;
        private Variable[] optimizationVariables;
        private int optimizationVariablesCount;
        private VariableLimitation[] optimizationLimitations;

        private Ball initialBallState;
        private double tableHitTime, netCrossTime, maxHeightTime;
        private double tableHitX, tableHitZ, netCrossY, maxHeight;

        public HitSearcher(int playerSide, BallExpression initialBallExpression, Variable[] optimizationVariables, VariableLimitation[] optimizationLimitations)
        {
            this.playerSide = playerSide;
            this.initialBallExpression = initialBallExpression;
            optimizationVariablesCount = optimizationVariables.Length;
            initialBallExpressionDerivatives = new BallExpression[optimizationVariablesCount];
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                initialBallExpressionDerivatives[i] = initialBallExpression.Derivate(optimizationVariables[i]);
            }
            this.optimizationVariables = optimizationVariables;
            this.optimizationLimitations = optimizationLimitations;
        }

        public bool Search()
        {
            var it = 0;
            while (true)
            {
                ++it;
                if (it > 100)
                {
                    return false;
                }

                GradientSearch.EvalResult? cachedResult = evaluate();
                if (double.IsNaN(tableHitTime))
                {
                    // It shouldn't happen
                    return false;
                }

                var done = true;
                if (tableHitX <= 0)
                {
                    done = false;
                }
                else
                {
                    var verticalLiftForce = Point3D.VectorMult(initialBallState.Speed, initialBallState.AngularSpeed).Y;
                    if (
                        tableHitX > Constants.HalfTableLength - Constants.SimulationBordersMargin ||
                        tableHitX < Constants.NetHeight ||
                        tableHitX < Constants.SimulationNetMargin && verticalLiftForce > 0
                    )
                    {
                        done = false;
                    }

                    if (double.IsNaN(netCrossTime))
                    {
                        // It shouldn't happen
                        return false;
                    }
                    if (netCrossY < Constants.MinNetCrossY)
                    {
                        done = false;
                    }
                }

                if (Math.Abs(tableHitZ) > Constants.HalfTableWidth - Constants.SimulationBordersMargin)
                {
                    done = false;
                }

                if (!double.IsNaN(maxHeightTime) && maxHeight > Constants.MaxBallMaxHeight)
                {
                    done = false;
                }

                if (done)
                {
                    return true;
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
                    return false;
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
            var initialBallDerivatives = new Ball.SimplifiedStepDerivatives()
            {
                VarsCount = optimizationVariablesCount,
                RelativeBallStateDv = new Ball[optimizationVariablesCount]
            };
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                initialBallDerivatives.RelativeBallStateDv[i] = initialBallExpressionDerivatives[i].Evaluate(cacheGeneration);
            }
            #endregion

            double criteriaValue, derivativeCoeff;

            #region tableHitPos
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
                return result;
            }
            initialBallDerivatives.TDv = null;
            var ballAtTableHitTime = Ball.DoStepSimplified(initialBallState, tableHitTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives simpleBallDerivativesAtTableHitTime, Ball.Component.VerticalSpeed | Ball.Component.Position);
            initialBallDerivatives.TDv = new double[optimizationVariablesCount];
            for (var i = 0; i < optimizationVariablesCount; i++)
            {
                var bdv = simpleBallDerivativesAtTableHitTime.RelativeBallStateDv[i];
                initialBallDerivatives.TDv[i] = -bdv.Position.Y / ballAtTableHitTime.Speed.Y;
            }
            Ball.DoStepSimplified(initialBallState, tableHitTime, initialBallDerivatives, out Ball.SimplifiedStepDerivatives ballDerivativesAtTableHitTime, Ball.Component.HorizontalPosition);

            #region tableHitX
            tableHitX = -playerSide * ballAtTableHitTime.Position.X;

            criteriaValue = (tableHitX - tableHitXAimMedian) / tableHitXAimCoeff;
            result.Value += criteriaValue * criteriaValue;
            derivativeCoeff = -2 * playerSide * criteriaValue / tableHitXAimCoeff;
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
            #endregion

            #region netCrossY
            if (tableHitX <= 0)
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

                criteriaValue = (netCrossY - netCrossYAimMedian) / netCrossYAimCoeff;
                result.Value += criteriaValue * criteriaValue;
                derivativeCoeff = 2 * criteriaValue / netCrossYAimCoeff;
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

                criteriaValue = (maxHeight - maxHeightAimMedian) / maxHeightAimCoeff;
                result.Value += criteriaValue * criteriaValue;
                derivativeCoeff = 2 * criteriaValue / maxHeightAimCoeff;
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
    }
}
