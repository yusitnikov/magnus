using System;

namespace Magnus
{
    class Simulation
    {
        public State s;

        public double h, y, x, t;

        public Simulation(State s)
        {
            this.s = s;
            h = y = x = t = 0;
        }

        public bool Run()
        {
            var serving = s.GameState == GameState.Serving;

            var tt = s.t;
            h = s.pos.Y;
            var dt = Constants.sdt * 10;

            if (!expectEvent(null, Constants.sdt, Event.BAT_HIT, true))
            {
                return false;
            }

            var s0 = s.Clone();

            while (!(GameState.FlyingToBat | GameState.Failed).HasFlag(s.GameState))
            {
                doStep(s0, dt, false);
            }
            if (s.GameState == GameState.Failed)
            {
                return false;
            }

            if (y < Constants.nh + Constants.br * 2)
            {
                return false;
            }
            if (serving && y < Constants.nh * 1.5)
            {
                return false;
            }

            x = s.pos.X;
            if (Math.Abs(x) < Constants.tw * 0.2 || Math.Abs(x) > Constants.tw * 0.8)
            {
                return false;
            }

            if (h > Constants.nh * 4)
            {
                return false;
            }

            t = s.t - tt;

            return true;
        }

        private Event doStep(State s0, double dt, bool useBat)
        {
            var events = s.DoStep(s0, dt, useBat);
            h = Math.Max(h, s.pos.Y);
            if (events.HasFlag(Event.NET_CROSS))
            {
                y = s.pos.Y;
            }
            return events;
        }

        private Event doStepsTillEvent(State s0, double dt, bool useBat)
        {
            Event events = 0;
            while (events == 0 || events == Event.MAX_HEIGHT)
            {
                events = doStep(s0, dt, useBat);
            }
            return events;
        }

        private bool expectEvent(State s0, double dt, Event ev, bool useBat = false)
        {
            return Misc.EventPresent(doStepsTillEvent(s0, dt, useBat), ev);
        }
    }
}
