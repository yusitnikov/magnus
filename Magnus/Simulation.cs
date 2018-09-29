using Mathematics.Expressions;
using Mathematics.Math3D;
using System;

namespace Magnus
{
    class Simulation
    {
        public readonly double MaxHeight, NetCrossY, TableHitX, Time;
        public readonly bool Success;

        private State state, initialState;
        private Variable t;

        public Simulation(State s)
        {
            state = s;

            MaxHeight = NetCrossY = TableHitX = Time = 0;
            Success = false;

            var serving = state.GameState == GameState.Serving;

            state = state.Clone(false);

            initialState = state.Clone(false);
            var initialTime = initialState.Time;

            t = new Variable("t", 0);

            while (!state.GameState.IsOneOf(GameState.FlyingToBat | GameState.Failed))
            {
                var events = state.DoSimplifiedStep(initialState, t, Constants.SimplifiedSimulationFrameTime);
                if (events.HasOneOfEvents(Event.AnyHit) && state.GameState == GameState.FlyingToTable)
                {
                    initialState.CopyFrom(state, false);
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

            if (state.GameState == GameState.Failed)
            {
                return;
            }

            if (NetCrossY < Constants.MinNetCrossY)
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
    }
}
