using Magnus.Strategies;
using System;

namespace Magnus
{
    class World
    {
        public State s;

        public double TimeCoeff;

        public DateTime t;
        private double tnMod;

        public World()
        {
            s = new State()
            {
                p = new Player[2],
                t = 0
            };
            s.p[Constants.LEFT_SIDE] = new Player(Constants.LEFT_SIDE, -1, new Passive());
            s.p[Constants.RIGHT_SIDE] = new Player(Constants.RIGHT_SIDE, 1, new Blocker());
            s.Reset(true, true);

            TimeCoeff = 4;

            t = DateTime.Now;
            tnMod = 0;
        }

        public void DoStep()
        {
            var dt = doTimeStep();

            findPlayerHits();

            doStateSteps(dt);
        }

        private double doTimeStep()
        {
            var t2 = DateTime.Now;
            var dt = (t2 - t).TotalSeconds * TimeCoeff;
            t = t2;
            return dt;
        }

        private void findPlayerHits()
        {
            for (var i = 0; i <= 1; i++)
            {
                var p = s.p[i];
                if (p.needAim && p.aim == null)
                {
                    p.FindHit(s);
                }
            }
        }

        private void doStateSteps(double dt)
        {
            double tnFloat = dt / Constants.sdt + tnMod;
            int tnInt = (int)Math.Floor(tnFloat);
            tnMod = tnFloat - tnInt;
            for (var i = 0; i < tnInt; i++)
            {
                var events = s.DoStepWithBatUpdate(null, Constants.sdt);

                if (Misc.EventPresent(events, Event.BAT_HIT))
                {
                    s.p[Misc.EventPresent(events, Event.LEFT_BAT_HIT) ? Constants.RIGHT_SIDE : Constants.LEFT_SIDE].RequestAim();
                }
            }
        }

        public void Serve()
        {
            s.Reset(true, true);
        }
    }
}
