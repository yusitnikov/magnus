using System;

namespace Magnus
{
    static class Misc
    {
        private static readonly Random random = new Random();

        public static double Rnd(double v1, double v2, bool closerToV1 = false)
        {
            double rnd = random.NextDouble();
            if (closerToV1)
            {
                rnd = rnd * rnd;
            }
            return v1 + (v2 - v1) * rnd;
        }

        public static T ChooseRL<T>(int side, T rightValue, T leftValue)
        {
            return side == Constants.RIGHT_SIDE ? rightValue : leftValue;
        }

        public static bool EventPresent(Event events, Event mask)
        {
            return (events & mask) != 0;
        }

        public static double Hypot(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public static double Sin(double a)
        {
            return Math.Sin(a * Math.PI / 180);
        }

        public static double Cos(double a)
        {
            return Math.Cos(a * Math.PI / 180);
        }
    }
}
