using System;

namespace Magnus
{
    struct DoublePoint3D : IFormattable
    {
        public static readonly DoublePoint3D Empty = new DoublePoint3D(0, 0, 0);
        public static readonly DoublePoint3D XAxis = new DoublePoint3D(1, 0, 0);
        public static readonly DoublePoint3D YAxis = new DoublePoint3D(0, 1, 0);
        public static readonly DoublePoint3D ZAxis = new DoublePoint3D(0, 0, 1);

        public double X, Y, Z;

        public double Length => Misc.Hypot(X, Y, Z);

        public DoublePoint3D Normal
        {
            get
            {
                var len = Length;
                return len == 0 ? Empty : this / len;
            }
        }

        public double Pitch => Math.Acos(Normal.Y);
        public double Yaw => Math.Atan2(Z, X);

        public DoublePoint3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static DoublePoint3D FromAngles(double pitch, double yaw)
        {
            return YAxis.RotatePitch(pitch).RotateYaw(yaw);
        }

        public struct ProjectionToNormal
        {
            public DoublePoint3D Vertical, Horizontal;

            public DoublePoint3D Full => Vertical + Horizontal;
        }

        public double GetProjectionToNormalVectorLength(DoublePoint3D normal)
        {
            return ScalarMult(this, normal);
        }

        public ProjectionToNormal ProjectToNormalVector(DoublePoint3D normal)
        {
            var vertical = GetProjectionToNormalVectorLength(normal) * normal;
            return new ProjectionToNormal()
            {
                Vertical = vertical,
                Horizontal = this - vertical
            };
        }

        public DoublePoint3D RotateByAngle3D(DoublePoint3D angle3D)
        {
            var angleDirection = angle3D.Normal;
            var projection = ProjectToNormalVector(angleDirection);
            var angleLength = angle3D.Length;
            return projection.Vertical + projection.Horizontal * Math.Cos(angleLength) + VectorMult(this, angleDirection) * Math.Sin(angleLength);
        }

        // Rotate around OZ: from OY to OX
        public DoublePoint3D RotatePitch(double angle)
        {
            return RotateByAngle3D(new DoublePoint3D(0, 0, angle));
        }

        // Rotate around OY: from OX to OZ
        public DoublePoint3D RotateYaw(double angle)
        {
            return RotateByAngle3D(new DoublePoint3D(0, angle, 0));
        }

        // Rotate around OX: from OZ to OY
        public DoublePoint3D RotateRoll(double angle)
        {
            return RotateByAngle3D(new DoublePoint3D(angle, 0, 0));
        }

        public static DoublePoint3D VectorMult(DoublePoint3D p1, DoublePoint3D p2)
        {
            return new DoublePoint3D(
                p1.Y * p2.Z - p1.Z * p2.Y,
                p1.Z * p2.X - p1.X * p2.Z,
                p1.X * p2.Y - p1.Y * p2.X
            );
        }

        public static double ScalarMult(DoublePoint3D p1, DoublePoint3D p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;
        }

        #region Operators

        public static DoublePoint3D operator +(DoublePoint3D p1, DoublePoint3D p2)
        {
            return new DoublePoint3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static DoublePoint3D operator -(DoublePoint3D p1, DoublePoint3D p2)
        {
            return new DoublePoint3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static DoublePoint3D operator -(DoublePoint3D p)
        {
            return new DoublePoint3D(-p.X, -p.Y, -p.Z);
        }

        public static DoublePoint3D operator *(DoublePoint3D p, double k)
        {
            return new DoublePoint3D(p.X * k, p.Y * k, p.Z * k);
        }

        public static DoublePoint3D operator *(double k, DoublePoint3D p)
        {
            return new DoublePoint3D(k * p.X, k * p.Y, k * p.Z);
        }

        public static DoublePoint3D operator /(DoublePoint3D p, double k)
        {
            return new DoublePoint3D(p.X / k, p.Y / k, p.Z / k);
        }

        #endregion

        #region ToString

        private string toString(string xString, string yString, string zString)
        {
            return "(" + xString + ";" + yString + ";" + zString + ")";
        }

        public override string ToString()
        {
            return toString(X.ToString(), Y.ToString(), Z.ToString());
        }

        public string ToString(string format)
        {
            return toString(X.ToString(format), Y.ToString(format), Z.ToString(format));
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return toString(X.ToString(formatProvider), Y.ToString(formatProvider), Z.ToString(formatProvider));
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return toString(X.ToString(format, formatProvider), Y.ToString(format, formatProvider), Z.ToString(format, formatProvider));
        }

        #endregion
    }
}
