using Magnus.Strategies;
using Mathematics.Math3D;
using System;

namespace Magnus
{
    class Player : ASurface
    {
        public Point3D Force;

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

        public Point3D LowestPoint
        {
            get
            {
                var vector = TranslateVectorFromBatCoords(new Point3D(Constants.BatRadius, 0, 0));
                return Position - vector * Math.Sign(vector.Y);
            }
        }

        private HitSearcherThread hitSearcherThread;
        public bool NeedAim { get; private set; }
        private Aim aim;

        internal Player()
        {
        }

        public Player(int index)
        {
            Index = index;
            Score = 0;
            Strategy = new Strategy();
            Position = Speed = Force = Point3D.Empty;
            Normal = DefaultNormal;
            NeedAim = false;
            aim = null;
        }

        public void InitHitSearchThread()
        {
            hitSearcherThread = new HitSearcherThread();
        }

        // X - shorter side, Y - normal, Z - longer side
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
            aim = null;
            if (hitSearcherThread != null)
            {
                hitSearcherThread.StopSearching();
            }
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
                aim = null
            };
        }

        public void DoStep(State state, double dt)
        {
            if (aim == null)
            {
                MoveToInitialPosition(state, false);
            }

            aim.TrySetCurrentState(this, state.Time - dt);

            if (state.GameState == GameState.Failed && (!(aim is AimByWaitPosition) || Math.Abs(aim.AimPlayer.Position.X) < getWaitX(state, false)))
            {
                ResetAim();
                MoveToInitialPosition(state, false);
            }

            aim.UpdatePlayer(state, this);

            Position += Speed * dt + Force * (dt * dt / 2);
            Speed += Force * dt;
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
            aim = GetInitialPositionAim(state, prepareToServe);
        }

        public Aim GetInitialPositionAim(State state, bool prepareToServe)
        {
            var readyPosition = Clone();
            readyPosition.ResetPosition(getWaitX(state, prepareToServe), false);
            return new AimByWaitPosition(readyPosition, this, state.Time);
        }

        public void RequestAim(State state)
        {
            NeedAim = true;
            hitSearcherThread.StartSearching(state, this, true);
        }

        public void FindHit(State state)
        {
            var result = hitSearcherThread.GetResult();
            if (result == null)
            {
                hitSearcherThread.StartSearching(state, this, false);
            }
            else
            {
                NeedAim = false;
                aim = result;
            }
        }
    }
}
