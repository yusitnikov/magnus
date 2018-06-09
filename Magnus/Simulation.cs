using System;

namespace Magnus
{
    class Simulation
    {
        public State s;
        public bool serving;

        public double h, y, x, t;

        public Simulation(State s, bool serving)
        {
            this.s = s;
            this.serving = serving;
        }

        public bool Run()
        {
            var tt = s.t;
            h = s.pos.Y;
            var dt = Constants.sdt * 10;

            var events = s.DoStepsTillEvent(null, Constants.sdt, true);
            if (!Misc.EventPresent(events, Event.BAT_HIT))
            {
                return false;
            }

            var s0 = s.Clone();

            if (serving)
            {
                if (!expectEvent(s0, dt, Event.TABLE_HIT))
                {
                    return false;
                }

                if (Math.Abs(s.pos.X) > Constants.tw * 0.8)
                {
                    return false;
                }
            }

            if (!expectEvent(s0, dt, Event.NET_CROSS))
            {
                return false;
            }

            y = s.pos.Y;
            if (y < Constants.nh + Constants.br * 2)
            {
                return false;
            }
            if (serving && y < Constants.nh * 1.5)
            {
                return false;
            }

            if (!expectEvent(s0, dt, Event.TABLE_HIT))
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

        private bool expectEvent(State s0, double dt, Event ev)
        {
            Event events = s.DoStepsTillEvent(s0, dt, false);

            if (Misc.EventPresent(events, Event.MAX_HEIGHT))
            {
                h = Math.Max(h, s.pos.Y);

                if (events == Event.MAX_HEIGHT)
                {
                    events = s.DoStepsTillEvent(s0, dt, false);
                }
            }

            return Misc.EventPresent(events, ev);
        }
    }
}
