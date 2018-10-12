namespace Magnus
{
    abstract class AimCoord
    {
        protected const double a = Constants.MaxPlayerForce;

        protected double aimX, aimX0, aimDX;
        protected double aimV, aimV0, aimDV;
        protected double aimT, aimT0, aimDT;

        protected int forceCoeff1, forceCoeff2;
        protected double t1, t2;

        public double TimeToMove { get; protected set; }

        public bool HasTimeToReact { get; protected set; }

        public virtual void SetAimState(double x, double v, double t)
        {
            aimX = x;
            aimV = v;
            aimT = t;
        }

        public virtual void SetCurrentState(double x, double v, double t)
        {
            aimX0 = x;
            aimDX = aimX - aimX0;
            aimV0 = v;
            aimDV = aimV - aimV0;
            aimT0 = t;
            aimDT = aimT - aimT0;

            // Implementation class must set forceCoeff1, forceCoeff2, t1, t2, HasTimeToReact and TimeToMove variables' values
        }

        public double GetForce(State s)
        {
            double dt = s.Time - aimT0;

            if (dt > aimDT)
            {
                return 0;
            }
            else if (dt < t1)
            {
                return forceCoeff1 * a;
            }
            else if (dt > aimDT - t2)
            {
                return forceCoeff2 * a;
            }
            else
            {
                return 0;
            }
        }
    }
}
