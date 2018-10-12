using System;

namespace Magnus
{
    class AimCoordByOptimalPath : AimCoord
    {
        public override void SetCurrentState(double x, double v, double t)
        {
            base.SetCurrentState(x, v, t);

            // try all possible variations of forceCoeff1 and forceCoeff2
            var dvSign = aimDV > 0 ? 1 : -1;
            HasTimeToReact = tryC(1, -1) || tryC(-1, 1) || tryC(dvSign, dvSign);

            TimeToMove = HasTimeToReact ? (t1 + aimDT - t2) / 2 : aimDT;
        }

        private bool tryC(int forceCoeff1, int forceCoeff2)
        {
            this.forceCoeff1 = forceCoeff1;
            this.forceCoeff2 = forceCoeff2;

            var b = forceCoeff1 * aimDV - a * aimDT;
            var c = aimDV * aimDV / (2 * a) - forceCoeff1 * (aimV * aimDT - aimDX);

            if (forceCoeff1 == forceCoeff2)
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

            t1 = (aimDV / a - forceCoeff2 * t2) / forceCoeff1;

            return t1 >= 0 && t2 >= 0 && t1 + t2 <= aimDT;
        }
    }
}
