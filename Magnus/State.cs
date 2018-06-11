using System;

namespace Magnus
{
    class State
    {
        public DoublePoint pos, speed;
        public double a, w;

        public double t;

        public Player[] p;

        public int HitSide;
        public GameState GameState;
        public DateTime NextServeTime = DateTime.MaxValue;

        public void Reset(bool resetPosition, bool resetAngle)
        {
            GameState = GameState.Serving;
            NextServeTime = DateTime.MaxValue;
            pos = new DoublePoint((Constants.tw + Misc.Rnd(100, 400)) * Misc.GetSideByIndex(HitSide), Constants.nh);
            speed = new DoublePoint(0, Misc.Rnd(150, 400));
            w = 0;

            foreach (var player in p)
            {
                player.Reset(Math.Abs(pos.X) + Constants.nh, resetPosition, resetAngle);
            }

            p[HitSide].RequestAim();
        }

        private Event doStep(State s0, double dt, bool useBat, bool updateBat = false)
        {
            var events = Event.NONE;

            var prev = new State()
            {
                pos = pos,
                speed = speed
            };

            if (useBat)
            {
                events |= doPlayerStep(dt, updateBat);
            }

            doBallAirStep(s0, dt);

            events |= checkForHits();

            if (pos.X * speed.X >= 0 && prev.pos.X * speed.X < 0)
            {
                events |= Event.NET_CROSS;
            }

            var lowCrossY = -Constants.nh * 2;
            if (pos.Y <= lowCrossY && prev.pos.Y > lowCrossY)
            {
                events |= Event.LOW_CROSS;
            }

            if (speed.Y <= 0 && prev.speed.Y > 0 && events == 0)
            {
                events = Event.MAX_HEIGHT;
            }

            if (s0 != null && Misc.EventPresent(events, Event.ANY_HIT))
            {
                s0.CopyFrom(this);
            }

            return events;
        }

        private void doBallAirStep(State s0, double dt)
        {
            var simplifiedSpeed = s0 == null ? speed : new DoublePoint(speed.X, 0);
            DoublePoint aa = Constants.cl * w * simplifiedSpeed.RotateRight90() - Constants.cd * simplifiedSpeed.Length * speed - new DoublePoint(0, Constants.g);
            double aw = Constants.cw * w * Math.Sqrt(Math.Abs(w));

            pos += speed * dt + aa * (dt * dt / 2);
            a += w * dt;
            t += dt;
            speed += aa * dt;
            w -= aw * dt;
            if (s0 != null)
            {
                double t0 = t - s0.t;
                double fwv = fw(s0.w, t0);
                w = Math.Sign(s0.w) / (fwv * fwv);
                var vx = 1 / (Constants.cd * Math.Sign(s0.speed.X) * t0 + 1 / s0.speed.X);
                speed = new DoublePoint(
                    vx,
                    (s0.speed.Y / s0.speed.X - Constants.cl * (ifw(s0.w, t0) - ifw(s0.w, 0)) - Constants.g * (Constants.cd * Math.Sign(s0.speed.X) * t0 * t0 / 2 + t0 / s0.speed.X)) * vx
                );
            }
        }

        private Event doPlayerStep(double dt, bool updateBat = false)
        {
            var events = Event.NONE;

            for (var i = 0; i <= 1; i++)
            {
                var pi = p[i];
                if (updateBat)
                {
                    pi.DoStep(this, dt);
                }

                // project to bat CS
                var n = DoublePoint.FromAngle(pi.a);
                var pdp = (pos - pi.pos).ProjectToNormalVector(n);
                var pdv = (speed - pi.speed).ProjectToNormalVector(n);
                if (Math.Abs(pdp.X) < Constants.nh / 2 + Constants.br && Math.Abs(pdp.Y) <= Constants.br && pdv.Y <= 0)
                {
                    events |= Event.BAT_HIT;
                    events |= Misc.ChooseRL(i, Event.RIGHT_BAT_HIT, Event.LEFT_BAT_HIT);

                    processBallHit(ref pdv, Constants.bhkx, Constants.bhky);
                    speed = pi.speed + pdv.ProjectFromNormalVector(n);

                    if (GameState != GameState.Failed)
                    {
                        if (i != HitSide || GameState.NotReadyToHit.HasFlag(GameState))
                        {
                            endSet(false);
                        }
                        else
                        {
                            HitSide = Misc.GetOtherSide(i);
                            switch (GameState)
                            {
                                case GameState.Serving:
                                    GameState = GameState.Served;
                                    break;
                                case GameState.FlyingToBat:
                                    GameState = GameState.FlyingToTable;
                                    break;
                            }
                        }
                    }
                }
            }

            return events;
        }

        public void EndSet()
        {
            if (GameState != GameState.Failed)
            {
                GameState = GameState.Failed;
                NextServeTime = DateTime.Now.AddSeconds(2);
            }
        }

        private void endSet(bool hitSideIsWinner)
        {
            if (GameState != GameState.Failed)
            {
                if (!hitSideIsWinner)
                {
                    HitSide = Misc.GetOtherSide(HitSide);
                }
                ++p[HitSide].score;
                EndSet();
            }
        }

        private void processBallHit(ref DoublePoint relativeSpeed, double kx, double ky)
        {
            const double kw = Constants.br * Math.PI / 180;
            relativeSpeed.Y *= -ky;
            double rollSpeedAtPoint = -w * kw;
            double force = -kx * (rollSpeedAtPoint + relativeSpeed.X);
            rollSpeedAtPoint += force;
            relativeSpeed.X += force;
            w = -rollSpeedAtPoint / kw;
        }

        private double fw(double w, double t)
        {
            return Constants.cw * t / 2 + 1 / Math.Sqrt(Math.Abs(w));
        }

        private double ifw(double w, double t)
        {
            return -Math.Sign(w) * 2 / Constants.cw / fw(w, t);
        }

        private Event checkForHits()
        {
            Event events = 0;

            var floorHitY = Constants.br - Constants.th;
            if (pos.Y < floorHitY)
            {
                events |= Event.FLOOR_HIT;

                pos.Y = 2 * floorHitY - pos.Y;
                speed.Y = Constants.thky * Math.Abs(speed.Y);

                endSet(GameState.NotReadyToHit.HasFlag(GameState));
            }

            var tableHitY = Constants.br;
            var tableEndX = Constants.tw + Constants.br;
            if (pos.Y < tableHitY && Math.Abs(pos.X) < tableEndX)
            {
                if (speed.Y < 0 && pos.Y - tableHitY > Math.Abs(pos.X) - tableEndX)
                {
                    events |= Event.TABLE_HIT;

                    pos.Y = 2 * tableHitY - pos.Y;
                    processBallHit(ref speed, Constants.thkx, Constants.thky);

                    var isHitSide = Math.Sign(pos.X) == Misc.GetSideByIndex(HitSide);
                    switch (GameState)
                    {
                        case GameState.Serving:
                        case GameState.FlyingToBat:
                            endSet(false);
                            break;
                        case GameState.Served:
                            if (isHitSide)
                            {
                                endSet(true);
                            }
                            else
                            {
                                GameState = GameState.FlyingToTable;
                            }
                            break;
                        case GameState.FlyingToTable:
                            if (isHitSide)
                            {
                                GameState = GameState.FlyingToBat;
                            }
                            else
                            {
                                endSet(true);
                            }
                            break;
                    }
                }
                else
                {
                    events |= Event.FLOOR_HIT;

                    double k = Math.Sign(pos.X);
                    pos.X = 2 * tableEndX * k - pos.X;
                    speed.X = Math.Abs(speed.X) * k;

                    endSet(GameState.NotReadyToHit.HasFlag(GameState));
                }
            }

            return events;
        }

        public Event DoStep(State s0 = null, double dt = Constants.sdt, bool useBat = false)
        {
            return doStep(s0, dt, useBat);
        }

        public Event DoStepWithBatUpdate(State s0, double dt)
        {
            return doStep(s0, dt, true, true);
        }

        public void CopyFrom(State s)
        {
            HitSide = s.HitSide;
            GameState = s.GameState;
            NextServeTime = s.NextServeTime;
            pos = s.pos;
            a = s.a;
            speed = s.speed;
            w = s.w;
            t = s.t;
            p = new Player[] { s.p[0].Clone(), s.p[1].Clone() };
        }

        public State Clone()
        {
            var s = new State();
            s.CopyFrom(this);
            return s;
        }
    }
}
