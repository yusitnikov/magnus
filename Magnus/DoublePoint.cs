using System;

namespace Magnus
{
    struct DoublePoint
    {
        public static readonly DoublePoint Empty = new DoublePoint(0, 0);

        public double X, Y;

        public double Length => Misc.Hypot(X, Y);

        public DoublePoint Normal
        {
            get
            {
                var len = Length;
                return len == 0 ? Empty : this / len;
            }
        }

        public double Angle => Math.Atan2(X, Y);

        public DoublePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static DoublePoint FromAngle(double a)
        {
            return new DoublePoint(Math.Sin(a), Math.Cos(a));
        }

        public DoublePoint RotateRight90()
        {
            return new DoublePoint(Y, -X);
        }

        public DoublePoint RotateLeft90()
        {
            return new DoublePoint(-Y, X);
        }

        public DoublePoint RotateRightByNormalVector(DoublePoint normal)
        {
            return new DoublePoint(X * normal.Y + Y * normal.X, Y * normal.Y - X * normal.X);
        }

        public DoublePoint RotateLeftByNormalVector(DoublePoint normal)
        {
            return new DoublePoint(X * normal.Y - Y * normal.X, Y * normal.Y + X * normal.X);
        }

        public DoublePoint ProjectToNormalVector(DoublePoint normal)
        {
            return RotateLeftByNormalVector(normal);
        }

        public DoublePoint ProjectFromNormalVector(DoublePoint normal)
        {
            return RotateRightByNormalVector(normal);
        }

        public static DoublePoint operator +(DoublePoint p1, DoublePoint p2)
        {
            return new DoublePoint(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static DoublePoint operator -(DoublePoint p1, DoublePoint p2)
        {
            return new DoublePoint(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static DoublePoint operator -(DoublePoint p)
        {
            return new DoublePoint(-p.X, -p.Y);
        }

        public static DoublePoint operator *(DoublePoint p, double k)
        {
            return new DoublePoint(p.X * k, p.Y * k);
        }

        public static DoublePoint operator *(double k, DoublePoint p)
        {
            return new DoublePoint(k * p.X, k * p.Y);
        }

        public static DoublePoint operator /(DoublePoint p, double k)
        {
            return new DoublePoint(p.X / k, p.Y / k);
        }
    }
}
