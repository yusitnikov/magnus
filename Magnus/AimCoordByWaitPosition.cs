using System;

namespace Magnus
{
    class AimCoordByWaitPosition : AimCoord
    {
        public override void SetCurrentState(double x, double v, double t)
        {
            base.SetCurrentState(x, v, t);

            forceCoeff1 = Math.Sign(aimDX - v * Math.Abs(v) / (2 * a));
            if (forceCoeff1 == 0)
            {
                forceCoeff1 = v > 0 ? -1 : 1;
            }
            forceCoeff2 = -forceCoeff1;

            var t0 = forceCoeff2 * v / a;
            t2 = Math.Sqrt(forceCoeff1 * aimDX / a + t0 * t0 / 2);
            t1 = t0 + t2;
            TimeToMove = aimDT = t1 + t2;

            HasTimeToReact = true;
        }
    }
}
