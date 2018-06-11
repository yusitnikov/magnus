using Magnus.Strategies;
using System;

namespace Magnus
{
    class Player
    {
        public int index;

        public int side;

        public int score;

        public Strategy strategy;

        public DoublePoint pos, prevPos, speed;

        public double a;

        public bool needAim;
        public Aim aim;

        private Player()
        {
        }

        public Player(int index)
        {
            this.index = index;
            side = Misc.GetSideByIndex(index);
            score = 0;
            strategy = new Strategy();
            pos = prevPos = speed = DoublePoint.Empty;
            a = 0;
            needAim = false;
            aim = null;
        }

        public void Reset(double x, bool resetPosition, bool resetAngle)
        {
            if (resetPosition)
            {
                if (resetAngle)
                {
                    a = 180 + 90 * side;
                }

                prevPos = pos = new DoublePoint(x * side, Constants.nh);
                speed = DoublePoint.Empty;
            }

            needAim = false;
            aim = null;
        }

        public Player Clone()
        {
            return new Player()
            {
                index = index,
                side = side,
                score = score,
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
                    MoveToInitialPosition(s.t);
                }
            }

            speed = (pos - prevPos) / dt;
            prevPos = pos;
        }

        public void MoveToInitialPosition(double currentTime)
        {
            var readyPosition = Clone();
            readyPosition.Reset(Constants.tw + Constants.nh * 2, true, false);
            needAim = false;
            aim = new Aim(readyPosition, this, currentTime + 10000, currentTime);
        }

        public void RequestAim()
        {
            needAim = true;
        }

        public void FindHit(State s)
        {
            if (s.GameState == GameState.Failed || index != s.HitSide)
            {
                MoveToInitialPosition(s.t);
                return;
            }

            var t0 = DateTime.Now;

            State s2 = s.Clone();
            Player p2 = s2.p[index];

            Event events;

            if (s.GameState == GameState.Serving)
            {
                double yy = Constants.nh * Misc.Rnd(1, 2);

                while (s2.speed.Y >= 0 || s2.pos.Y >= yy)
                {
                    s2.DoStep();
                }
            }
            else
            {
                while (!(GameState.FlyingToBat | GameState.Failed).HasFlag(s2.GameState))
                {
                    s2.DoStep();
                }
                if (s2.GameState == GameState.Failed)
                {
                    MoveToInitialPosition(s.t);
                    return;
                }

                var s3 = s2.Clone();

                // Calculate time till max height
                do
                {
                    events = s3.DoStep();
                }
                while (!Misc.EventPresent(events, Event.MAX_HEIGHT));
                double t1 = s3.t - s2.t;

                // Calculate time till ball fall
                do
                {
                    events = s3.DoStep();
                }
                while (!Misc.EventPresent(events, Event.LOW_HIT));
                double t2 = s3.t - s2.t;

                // Choose a random point to hit and move s2 to it
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

            Aim newAim = null;
            while (newAim == null && (DateTime.Now - t0).TotalSeconds < 0.02)
            {
                double v, a1, a2;
                if (s.GameState == GameState.Serving)
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

                var simulation = new Simulation(s2.Clone());
                if (simulation.Run())
                {
                    newAim = new Aim(p2, this, s2.t, s.t);
                    if (newAim.HasTimeToReact)
                    {
                        needAim = false;
                        aim = newAim;
                    }
                    else
                    {
                        newAim = null;
                    }
                }
            }
        }
    }
}
