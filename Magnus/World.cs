using System;

namespace Magnus
{
    class World
    {
        public const double DefaultTimeCoeff = 7;

        public State State;

        public double TimeCoeff;

        private DateTime time;
        private double framesFloatPart;

        public DateTime NextServeTime;

        public World()
        {
            State = new State()
            {
                Ball = new Ball(),
                Players = new Player[2],
                Time = 0,
                HitSide = Misc.Rnd(0, 1) < 0.5 ? Constants.LeftPlayerIndex : Constants.RightPlayerIndex
            };
            State.UpdateNextServeX();
            for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
            {
                var player = State.Players[playerIndex] = new Player(playerIndex);
                player.ResetPosition(State.NextServeX + Constants.NetHeight, true);
                player.InitHitSearchThread();
            }
            State.Reset();

            TimeCoeff = 4;

            time = DateTime.Now;
            framesFloatPart = 0;

            NextServeTime = DateTime.MaxValue;
        }

        public void DoStep()
        {
            if (DateTime.Now > NextServeTime)
            {
                State.Reset();
                NextServeTime = DateTime.MaxValue;
            }

            var dt = doTimeStep();

            findPlayerHits();

            doStateSteps(dt);

            if (State.GameState == GameState.Failed && NextServeTime == DateTime.MaxValue)
            {
                NextServeTime = DateTime.Now.AddSeconds(2);
            }
        }

        private double doTimeStep()
        {
            var newTime = DateTime.Now;
            var dt = (newTime - time).TotalSeconds;
            if (dt > 3)
            {
                // Too long frame (probably because of debugging) - skip
                dt = 0;
            }
            time = newTime;
            return dt * Constants.TimeUnit * TimeCoeff / DefaultTimeCoeff;
        }

        private void findPlayerHits()
        {
            foreach (var player in State.Players)
            {
                if (player.NeedAim)
                {
                    player.FindHit(State);
                }
            }
        }

        private void doStateSteps(double dt)
        {
            double floatFramesTime = dt / Constants.SimulationFrameTime + framesFloatPart;
            int intFramesTime = (int)Math.Floor(floatFramesTime);
            framesFloatPart = floatFramesTime - intFramesTime;

            for (var frame = 0; frame < intFramesTime; frame++)
            {
                var events = State.DoStepWithBatUpdate();

                if (events.HasFlag(Event.BatHit))
                {
                    var hitPlayerIndex = events.HasFlag(Event.LeftBatHit) ? Constants.RightPlayerIndex : Constants.LeftPlayerIndex;
                    State.Players[hitPlayerIndex].RequestAim(State);
                }
            }
        }
    }
}
