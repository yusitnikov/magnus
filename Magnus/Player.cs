using Magnus.Strategies;
using System;

namespace Magnus
{
    class Player
    {
        public int index;

        public int side;

        public Strategy strategy;

        public DoublePoint pos, prevPos, speed;

        public double a;

        public bool needAim;
        public Aim aim;

        private Player()
        {
        }

        public Player(int index, int side, Strategy strategy)
        {
            this.index = index;
            this.side = side;
            this.strategy = strategy;
            pos = prevPos = speed = DoublePoint.Empty;
            a = 0;
            needAim = false;
            aim = null;
        }

        public Player Clone()
        {
            return new Player()
            {
                index = index,
                side = side,
                strategy = strategy,
                pos = pos,
                prevPos = prevPos,
                speed = speed,
                a = a,
                needAim = false,
                aim = null
            };
        }

        public void DoStep(State s, double dt)
        {
            if (aim != null)
            {
                bool stillMoving = aim.UpdatePlayerPosition(s, this);

                if (!stillMoving)
                {
                    var readyState = s.Clone();
                    readyState.Reset(true, false);
                    readyState.t = s.t + 10000;

                    aim = new Aim(readyState, s.Clone(), index);
                }
            }

            speed = (pos - prevPos) / dt;
            prevPos = pos;
        }

        public void RequestAim()
        {
            needAim = true;
            aim = null;
        }

        public void FindHit(State s)
        {
            var t0 = DateTime.Now;

            var serving = s.speed.X == 0 && Math.Abs(s.pos.X) > Constants.tw;

            State s2 = s.Clone();
            Player p2 = s2.p[index];

            Event events;

            if (!serving)
            {
                do
                {
                    events = s2.DoStep();
                }
                while (!Misc.EventPresent(events, Event.LOW_HIT));

                if (Math.Sign(s2.pos.X) != side)
                {
                    do
                    {
                        events = s2.DoStep();
                    }
                    while (!Misc.EventPresent(events, Event.LOW_HIT));
                }
            }

            if (serving)
            {
                double yy = Constants.nh * Misc.Rnd(1, 2);

                while (s2.speed.Y >= 0 || s2.pos.Y >= yy)
                {
                    events = s2.DoStep();
                }
            }
            else
            {
                var s3 = s2.Clone();

                do
                {
                    events = s3.DoStep();
                }
                while (!Misc.EventPresent(events, Event.MAX_HEIGHT));
                double t1 = s3.t - s2.t;

                do
                {
                    events = s3.DoStep();
                }
                while (!Misc.EventPresent(events, Event.LOW_HIT));
                double t2 = s3.t - s2.t;

                double tt = s2.t + strategy.GetBackHitTime(t1, t2);
                while (s2.t < tt)
                {
                    s2.DoStep();
                }
            }

            double va = s2.speed.Angle;
            if (va == 180 && index == Constants.LEFT_SIDE)
            {
                va = -180;
            }

            while (aim == null && (DateTime.Now - t0).TotalSeconds < 0.02)
            {
                double v, a1, a2;
                if (serving)
                {
                    v = Constants.mv * Misc.Rnd(0.2, 0.7) * Math.Abs(s2.pos.X) / Constants.tw;
                    a1 = Misc.Rnd(20, 100) * side;
                    var rnd = Misc.Rnd(-1, 1);
                    a2 = (90 + 60 * rnd * rnd * rnd) * side;
                }
                else
                {
                    v = Constants.mv * strategy.GetHitSpeed();
                    a1 = strategy.GetAttackAngle() * side;
                    a2 = strategy.GetVelocityAttackAngle() * side;
                    a2 = Math.Max(a2, a1 - 70);
                    a2 = Math.Min(a2, a1 + 70);
                }

                p2.pos = s2.pos + Constants.br * DoublePoint.FromAngle(va - a1);
                p2.speed = -v * DoublePoint.FromAngle(va - a2);
                p2.a = 180 + va - a1;

                var simulation = new Simulation(s2.Clone(), serving);
                if (simulation.Run())
                {
                    aim = new Aim(s2, s.Clone(), index);
                    if (aim.HasTimeToReact)
                    {
                        needAim = false;
                    }
                    else
                    {
                        aim = null;
                    }
                }
            }
        }
    }
}
