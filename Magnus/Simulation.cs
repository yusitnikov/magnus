using Mathematics.Expressions;
using Mathematics.Math3D;
using System;

namespace Magnus
{
    class Simulation
    {
        public const bool UseBinarySearch = true;

        public readonly double MaxHeight, NetCrossY, TableHitX, Time;
        public readonly bool Success;

        private State state, initialState;
        private Variable t;
        private BallExpression simplifiedTrajectory;

        public Simulation(State s)
        {
            state = s;

            MaxHeight = NetCrossY = TableHitX = Time = 0;
            Success = false;

            var serving = state.GameState == GameState.Serving;

            state = state.Clone(true);
            if (!state.DoStep(true).HasFlag(Event.BatHit))
            {
                return;
            }

            initialState = state.Clone(false);
            var initialTime = initialState.Time;

            t = new Variable("t", 0);
            simplifiedTrajectory = ((BallExpression)initialState.Ball).GetSimplifiedTrajectory(t);

            BinarySearchResult time;
            Event events;

            var isServing = initialState.GameState == GameState.Served;
            var useBinarySearch = UseBinarySearch && !isServing;

            if (false && useBinarySearch && isServing)
            {
                time = calcTableHitTime();
                if (time == null)
                {
                    return;
                }
                doStep(time.Min);
                if (state.GameState != GameState.Served)
                {
                    return;
                }
                events = doStep(time.Max - time.Min);
                if (!events.HasFlag(Event.TableHit) || state.GameState != GameState.FlyingToTable)
                {
                    return;
                }
                initialState.CopyFrom(state);
                simplifiedTrajectory = ((BallExpression)initialState.Ball).GetSimplifiedTrajectory(t);
            }

            if (useBinarySearch)
            {
                time = calcTableHitTime();
                if (time == null)
                {
                    return;
                }
                doStep(time.Min);
                if (state.GameState != GameState.FlyingToTable)
                {
                    return;
                }
                events = doStep(time.Max - time.Min);
                if (!events.HasFlag(Event.TableHit) || state.GameState != GameState.FlyingToBat)
                {
                    return;
                }
                var tableHitTime = time.Exact;

                time = calcNetCrossTime(tableHitTime);
                if (time == null)
                {
                    return;
                }
                NetCrossY = getSimplifiedY(time.Exact);

                time = calcMaxHeight(tableHitTime);
                if (time != null)
                {
                    MaxHeight = getSimplifiedY(time.Min);
                }
            }
            else
            {
                while (!state.GameState.IsOneOf(GameState.FlyingToBat | GameState.Failed))
                {
                    events = doStep(Constants.SimplifiedSimulationFrameTime);
                    if (events.HasOneOfEvents(Event.AnyHit) && state.GameState == GameState.FlyingToTable)
                    {
                        initialState.CopyFrom(state);
                        simplifiedTrajectory = ((BallExpression)initialState.Ball).GetSimplifiedTrajectory(t);
                    }
                    if (state.Ball.Speed.Y > 0)
                    {
                        MaxHeight = Math.Max(MaxHeight, state.Ball.Position.Y);
                    }
                    if (events.HasFlag(Event.NetCross))
                    {
                        NetCrossY = state.Ball.Position.Y;
                    }
                }
            }

            if (state.GameState == GameState.Failed)
            {
                return;
            }

            if (NetCrossY < Constants.MaxNetCrossY)
            {
                return;
            }

            TableHitX = state.Ball.Position.X;
            if (Math.Abs(TableHitX) < Constants.SimulationNetMargin && Point3D.VectorMult(state.Ball.Speed, state.Ball.AngularSpeed).Y > 0)
            {
                return;
            }

            if (MaxHeight > Constants.MaxBallMaxHeight)
            {
                return;
            }

            Time = state.Time - initialTime;
            Success = true;

            return;
        }

        private Event doStep(double dt)
        {
            return state.DoStep(false, initialState, simplifiedTrajectory, t, dt);
        }

        private double getSimplifiedX(double time)
        {
            t.Value = time;
            return simplifiedTrajectory.Position.X.Evaluate();
        }

        private double getSimplifiedY(double time)
        {
            t.Value = time;
            return simplifiedTrajectory.Position.Y.Evaluate();
        }

        private double getSimplifiedSpeedY(double time)
        {
            t.Value = time;
            return simplifiedTrajectory.Speed.Y.Evaluate();
        }

        private BinarySearchResult calcTableHitTime()
        {
            return binarySearch(
                0, Constants.TimeUnit, Constants.SimplifiedSimulationFrameTime,
                time => getSimplifiedSpeedY(time) < 0 && getSimplifiedY(time) < Constants.BallRadius,
                time => getSimplifiedY(time) - Constants.BallRadius
            );
        }

        private BinarySearchResult calcNetCrossTime(double tableHitTime)
        {
            return binarySearch(
                0, tableHitTime, 0.1 * Constants.TimeUnit,
                time => getSimplifiedX(time) * initialState.Ball.Position.X < 0,
                getSimplifiedX
            );
        }

        private BinarySearchResult calcMaxHeight(double tableHitTime)
        {
            return binarySearch(
                0, tableHitTime, 0.1 * Constants.TimeUnit,
                time => getSimplifiedSpeedY(time) < 0,
                getSimplifiedSpeedY
            );
        }

        class BinarySearchResult
        {
            public double Min, Max, Exact;
            public int Iterations;
        }

        private BinarySearchResult binarySearch(double min, double max, double precision, Func<double, bool> comparer, Func<double, double> calculator)
        {
            if (comparer(min))
            {
                return null;
            }

            var iterations = 0;
            while (!comparer(max))
            {
                min = max;
                max *= 2;
                if (++iterations >= 10)
                {
                    return null;
                }
            }

            while (max - min > precision)
            {
                var mid = (min + max) / 2;
                if (comparer(mid))
                {
                    max = mid;
                }
                else
                {
                    min = mid;
                }
                ++iterations;
            }

            double v1 = calculator(min), v2 = calculator(max);
            var exact = v1 == v2 ? (min + max) / 2 : min - (max - min) * v1 / (v2 - v1);
            return new BinarySearchResult()
            {
                Min = min,
                Max = max,
                Exact = exact,
                Iterations = iterations
            };
        }
    }
}
