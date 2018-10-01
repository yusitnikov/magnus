using System;

namespace Magnus
{
    class AimCoord
    {
        const double a = Constants.MaxPlayerForce;

        public readonly double AimX, AimX0;
        public readonly double AimV, AimV0;
        public readonly double AimT, AimT0;

        private double t => AimT - AimT0;
        private double x1 => AimX0;
        private double x2 => AimX;
        private double v1 => AimV0;
        private double v2 => AimV;
        private double dv => v2 - v1;

        private int c1, c2;
        private double t1, t2;

        internal double timeToMove { get; private set; }

        public readonly bool HasTimeToReact;

        public AimCoord(double aimX, double aimX0, double aimV, double aimV0, double aimT, double aimT0)
        {
            AimX = aimX;
            AimX0 = aimX0;
            AimV = aimV;
            AimV0 = aimV0;
            AimT = aimT;
            AimT0 = aimT0;

            // try all possible variations of c1 and c2
            var dvSign = dv > 0 ? 1 : -1;
            HasTimeToReact = tryC(dvSign, dvSign) || tryC(1, -1) || tryC(-1, 1);

            timeToMove = HasTimeToReact ? (t1 + t - t2) / 2 : t;
        }

        private bool tryC(int c1, int c2)
        {
            this.c1 = c1;
            this.c2 = c2;

            var b = c1 * dv - a * t;
            var c = dv * dv / (2 * a) - c1 * (x1 - x2 + v2 * t);

            if (c1 == c2)
            {
                if (b == 0)
                {
                    return false;
                }

                t2 = c / b;
            }
            else
            {
                var d = b * b - 4 * a * c;
                if (d < 0)
                {
                    return false;
                }

                t2 = (-b - Math.Sqrt(d)) / (2 * a);
            }

            t1 = (dv / a - c2 * t2) / c1;

            return t1 >= 0 && t2 >= 0 && t1 + t2 <= t;
        }

        private double getF1(double dt)
        {
            return AimX0 + AimV0 * dt + c1 * a * dt * dt / 2;
        }
        private double getF2(double dt)
        {
            return getF1(t1) + (AimV0 + c1 * a * t1) * (dt - t1);
        }
        private double getF3(double dt)
        {
            dt -= t;
            return AimX + AimV * dt + c2 * a * dt * dt / 2;
        }

        public double GetX(State s)
        {
            double dt = s.Time - AimT0;

            if (dt < t1)
            {
                return getF1(dt);
            }
            else if (dt > t - t2)
            {
                return getF3(dt);
            }
            else
            {
                return getF2(dt);
            }
        }
    }
}
