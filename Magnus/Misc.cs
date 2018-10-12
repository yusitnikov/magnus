using Mathematics.Math3D;
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

        public static int GetPlayerSideByIndex(int side)
        {
            return side == Constants.RightPlayerIndex ? 1 : -1;
        }

        public static int GetOtherPlayerIndex(int side)
        {
            return 1 - side;
        }

        public static double FromDegrees(double degrees)
        {
            return degrees / 180 * Math.PI;
        }

        public static double NormalizeAngle(double a)
        {
            a %= 2 * Math.PI;
            if (a >= Math.PI)
            {
                a -= 2 * Math.PI;
            }
            if (a < -Math.PI)
            {
                a += 2 * Math.PI;
            }
            return a;
        }

        #region Forced movement

        public static double GetForceBySpeedAndTime(double speed, double time)
        {
            return speed / time;
        }

        public static double GetTimeBySpeedAndForce(double speed, double force)
        {
            return speed / force;
        }

        public static double GetForceBySpeedAndDistance(double speed, double distance)
        {
            return speed * speed / 2 / distance;
        }

        public static double GetDistanceBySpeedAndForce(double speed, double force)
        {
            return speed * speed / 2 / force;
        }

        public static double GetTimeByDistanceAndForce(double distance, double force)
        {
            return Math.Sqrt(2 * distance / force);
        }

        public static double GetDistanceByForceAndTime(double force, double time)
        {
            return force * time * time / 2;
        }

        public static double GetForceByDistanceAndTime(double distance, double time)
        {
            return 2 * distance / (time * time);
        }

        public static Point3D GetForceByDistanceAndTime(Point3D distance, double time)
        {
            return distance * (2 / (time * time));
        }

        #endregion
    }
}
