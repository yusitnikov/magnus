using Magnus.Strategies;
using Mathematics.Expressions;
using Mathematics.Math3D;
using System;
using System.Collections.Generic;

namespace Magnus
{
    class Player : ASurface
    {
        public static double LastSearchTime = 0;

        public int Index;

        public int Side => Misc.GetPlayerSideByIndex(Index);

        public int Score;

        public Strategy Strategy;

        public double AnglePitch, AngleYaw;

        public override Point3D Normal
        {
            get => TranslateVectorFromBatCoords(Point3D.YAxis);
            set
            {
                AnglePitch = value.Pitch;
                AngleYaw = value.Yaw;
            }
        }

        public Point3D DefaultNormal => new Point3D(-Side, 0, 0);

        public bool NeedAim;
        public Aim Aim;

        internal Player()
        {
        }

        public Player(int index)
        {
            Index = index;
            Score = 0;
            Strategy = new Strategy();
            Position = Speed = Point3D.Empty;
            Normal = DefaultNormal;
            NeedAim = false;
            Aim = null;
        }

        public Point3D TranslateVectorFromBatCoords(Point3D point)
        {
            return point.RotatePitch(AnglePitch).RotateYaw(AngleYaw);
        }

        public Point3D TranslatePointFromBatCoords(Point3D point)
        {
            return Position + TranslateVectorFromBatCoords(point);
        }

        public void ResetPosition(double x, bool resetAngle)
        {
            if (resetAngle)
            {
                Normal = DefaultNormal;
            }

            Position = new Point3D(x * Side, Constants.BatWaitY, 0);
            Speed = Point3D.Empty;
        }

        public void ResetAim()
        {
            NeedAim = false;
            Aim = null;
        }

        public Player Clone()
        {
            return new Player()
            {
                Index = Index,
                Score = Score,
                Strategy = Strategy,
                Position = Position,
                Speed = Speed,
                AnglePitch = AnglePitch,
                AngleYaw = AngleYaw,
                NeedAim = false,
                Aim = null
            };
        }

        public void DoStep(State state, double dt)
        {
            var prevPosition = Position;

            if (Aim != null)
            {
                bool stillMoving = Aim.UpdatePlayerPosition(state, this);

                if (!stillMoving || state.GameState == GameState.Failed && Math.Abs(Aim.AimPlayer.Position.X) < getWaitX(state, false))
                {
                    MoveToInitialPosition(state, false);
                }
            }

            Speed = (Position - prevPosition) / dt;
        }

        private double getWaitX(State state, bool prepareToServe)
        {
            if (state.GameState == GameState.Failed)
            {
                prepareToServe = true;
            }
            return prepareToServe ? state.NextServeX + Constants.BatLength : Constants.BatWaitX;
        }

        public void MoveToInitialPosition(State state, bool prepareToServe)
        {
            var readyPosition = Clone();
            readyPosition.ResetPosition(getWaitX(state, prepareToServe), false);
            readyPosition.ResetAim();
            NeedAim = false;
            Aim = new Aim(readyPosition, this, double.PositiveInfinity, state.Time);
        }

        public void RequestAim()
        {
            NeedAim = true;
        }

        public void FindHit(State state)
        {
            var searchStartTime = DateTime.Now;

            var hitSearcher = new HitSearcher(state, this);
            if (!hitSearcher.Initialize())
            {
                return;
            }

            var iterations = 0;
            while ((DateTime.Now - searchStartTime).TotalSeconds < Constants.MaxThinkTimePerFrame)
            {
                ++iterations;

                if (hitSearcher.Search())
                {
                    NeedAim = false;
                    Aim = hitSearcher.GetAim();
                    break;
                }
            }
            LastSearchTime = (DateTime.Now - searchStartTime).TotalSeconds * 1000000 / iterations;
        }
    }
}
