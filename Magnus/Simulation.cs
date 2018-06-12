using System;

namespace Magnus
{
    class Simulation
    {
        public readonly double MaxHeight, NetCrossY, TableHitX, Time;
        public readonly bool Success;

        public Simulation(State state)
        {
            MaxHeight = NetCrossY = TableHitX = Time = 0;
            Success = false;

            var serving = state.GameState == GameState.Serving;

            state = state.Clone(true);
            if (!state.DoStep(null, Constants.SimulationFrameTime, true).HasFlag(Event.BatHit))
            {
                return;
            }

            var initialState = state.Clone(false);
            var initialTime = state.Time;
            MaxHeight = state.Ball.Position.Y;

            while (!state.GameState.IsOneOf(GameState.FlyingToBat | GameState.Failed))
            {
                var events = state.DoStep(initialState, Constants.SimulationFrameTime * 10);
                MaxHeight = Math.Max(MaxHeight, state.Ball.Position.Y);
                if (events.HasFlag(Event.NetCross))
                {
                    NetCrossY = state.Ball.Position.Y;
                }
            }
            if (state.GameState == GameState.Failed)
            {
                return;
            }

            if (NetCrossY < Constants.NetHeight + Constants.BallRadius * 2)
            {
                return;
            }
            if (serving && NetCrossY < Constants.NetHeight * 1.5)
            {
                return;
            }

            TableHitX = state.Ball.Position.X;
            if (Math.Abs(TableHitX) < Constants.HalfTableWidth * 0.2 || Math.Abs(TableHitX) > Constants.HalfTableWidth * 0.8)
            {
                return;
            }

            if (MaxHeight > Constants.NetHeight * 4)
            {
                return;
            }

            Time = state.Time - initialTime;
            Success = true;

            return;
        }
    }
}
