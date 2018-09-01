using Mathematics;
using Mathematics.Expressions;
using Mathematics.Math3D;
using System;

namespace Magnus
{
    class Ball
    {
        public static readonly Point3D GravityForce = new Point3D(0, -Constants.GravityForce, 0);

        public Point3D Position, Speed;
        public Point3D AngularSpeed;
        // A point on the ball that rotates with the ball and indicates ball rotation in UI
        public Point3D MarkPoint;

        public int Side => Math.Sign(Position.X);

        public void CopyFrom(Ball ball)
        {
            Position = ball.Position;
            MarkPoint = ball.MarkPoint;
            Speed = ball.Speed;
            AngularSpeed = ball.AngularSpeed;
        }

        public Ball Clone()
        {
            var clone = new Ball();
            clone.CopyFrom(this);
            return clone;
        }

        public void DoStep(double dt, bool simplified)
        {
            var simplifiedSpeed = simplified ? new Point3D(Speed.X, 0, Speed.Z) : Speed;
            // V' = L V x W - D |V| V + G
            Point3D force = Constants.BallLiftCoeff * Point3D.VectorMult(simplifiedSpeed, AngularSpeed) - Constants.BallDumpCoeff * simplifiedSpeed.Length * Speed + GravityForce;
            // W' = -AD W sqrt |W|
            Point3D angularForce = -Constants.BallAngularDumpCoeff * AngularSpeed * Math.Sqrt(AngularSpeed.Length);

            Position += Speed * dt + force * (dt * dt / 2);
            MarkPoint = MarkPoint.RotateByAngle3D(AngularSpeed * dt);
            Speed += force * dt;
            AngularSpeed += angularForce * dt;
        }

        public void DoStepSimplified(Ball relativeBallState, double dt)
        {
            // P_v0, P_h0
            var positionProjection = relativeBallState.Position.ProjectToNormalVector(Surface.Horizontal.Normal);
            // V_v0, V_h0
            var speedProjection = relativeBallState.Speed.ProjectToNormalVector(Surface.Horizontal.Normal);
            // W_v0, W_h0
            var angularSpeedProjection = relativeBallState.AngularSpeed.ProjectToNormalVector(Surface.Horizontal.Normal);

            // W' = -AD W sqrt |W|
            // kw = AD sqrt |W_0| / 2
            var angularSpeedDumpCoeff = Constants.BallAngularDumpCoeff * Math.Sqrt(relativeBallState.AngularSpeed.Length) / 2;
            // fw = kw t + 1
            var angularSpeedDumpFunction = angularSpeedDumpCoeff * dt + 1;
            // Fw = int dt / fw^2 = (1 - 1 / fw) / kw
            // kw = 0 => Fw = t
            var angularSpeedDumpFunctionIntegral = angularSpeedDumpCoeff == 0 ? dt : (1 - 1 / angularSpeedDumpFunction) / angularSpeedDumpCoeff;
            // W = W_0 / fw^2
            AngularSpeed = relativeBallState.AngularSpeed / (angularSpeedDumpFunction * angularSpeedDumpFunction);
            // A = A_0 + int W dt = A_0 + W_0 Fw
            MarkPoint = relativeBallState.MarkPoint.RotateByAngle3D(relativeBallState.AngularSpeed * angularSpeedDumpFunctionIntegral);

            // V_h' = L V_h x W_v - D |V_h| V_h
            // kh = D |V_h0|
            var horizontalSpeedDumpCoeff = Constants.BallDumpCoeff * speedProjection.Horizontal.Length;
            // fh = kh t + 1
            var horizontalSpeedDumpFunction = horizontalSpeedDumpCoeff * dt + 1;
            // int fh dt = kh t^2/2 + t
            var horizontalSpeedDumpFunctionIntegral = horizontalSpeedDumpCoeff * dt * dt / 2 + dt;
            // |V_h| = |V_h0| / fh
            // A_h = L W_v0 Fw
            var horizontalRotateAngle = Constants.BallLiftCoeff* angularSpeedDumpFunctionIntegral *angularSpeedProjection.Vertical;
            // V_h = rotate(V_h0 / fh, A_h)
            var horizontalSpeed = (speedProjection.Horizontal / horizontalSpeedDumpFunction).RotateByAngle3D(horizontalRotateAngle);

            // V_v' = L V_h0 x W_h / fh - D |V_h| V_v + G
            // L V_h0 x W_h0
            var verticalLiftForce = Constants.BallLiftCoeff * Point3D.VectorMult(speedProjection.Horizontal, angularSpeedProjection.Horizontal);
            // V_v = (V_v0 + L V_h0 x W_h0 (int dt / fw^2) + G (int fh dt)) / fh
            var verticalSpeed = (speedProjection.Vertical + angularSpeedDumpFunctionIntegral * verticalLiftForce + GravityForce * horizontalSpeedDumpFunctionIntegral) / horizontalSpeedDumpFunction;

            // lw = int dt / fw = log fw / kw
            var angularSpeedDumpInverseIntegral = angularSpeedDumpCoeff == 0 ? dt : Math.Log(angularSpeedDumpFunction) / angularSpeedDumpCoeff;
            // lh = int dt / fh = log fh / kh
            var horizontalSpeedDumpInverseIntegral = horizontalSpeedDumpCoeff == 0 ? dt : Math.Log(horizontalSpeedDumpFunction) / horizontalSpeedDumpCoeff;
            // P_v = P_v0 + int V_v dt
            Point3D verticalPosition = positionProjection.Vertical + horizontalSpeedDumpInverseIntegral * speedProjection.Vertical;
            if (horizontalSpeedDumpCoeff == 0)
            {
                // P_v = P_v0 + V_v0 t + G t^2 / 2
                verticalPosition += GravityForce * (dt * dt / 2);
            }
            else
            {
                // P_v = P_v0 + lh V_v0 + L V_h0 x W_h0 (lw - lh) / (kh - kw) + G (fh^2 - 1 - 2 kh lh) / 4kh^2
                var verticalLiftIntegral = (angularSpeedDumpInverseIntegral - horizontalSpeedDumpInverseIntegral) / (horizontalSpeedDumpCoeff - angularSpeedDumpCoeff);
                var gravityIntegral = (horizontalSpeedDumpFunction * horizontalSpeedDumpFunction - 1 - 2 * horizontalSpeedDumpCoeff * horizontalSpeedDumpInverseIntegral) / (4 * horizontalSpeedDumpCoeff * horizontalSpeedDumpCoeff);
                verticalPosition += verticalLiftIntegral * verticalLiftForce + gravityIntegral * GravityForce;
            }

            // int V_h dt
            Point3D horizontalSpeedIntegral;
            if (horizontalSpeedDumpCoeff == 0)
            {
                // V_h = 0
                horizontalSpeedIntegral = Point3D.Empty;
            }
            else
            {
                // R_h = int (e^iA_h / fh) dt
                Complex horizontalLiftIntegral;
                if (angularSpeedDumpCoeff == 0)
                {
                    // R_h = lh
                    horizontalLiftIntegral = horizontalSpeedDumpInverseIntegral;
                }
                else
                {
                    // I_0 = L W_v0
                    var I0 = Constants.BallLiftCoeff * angularSpeedProjection.Vertical.Y;
                    // I_wh = I_0 / (kw - kh)
                    var Iwh = I0 / (angularSpeedDumpCoeff - horizontalSpeedDumpCoeff);
                    // I_w = I_0 / kw
                    var Iw = I0 / angularSpeedDumpCoeff;
                    // x(j, f) = e^ij * (Ei(-ijf) - Ei(-ij))
                    // R_h = (x(I_wh, fh / fw) - x(I_w, 1 / fw)) / kh
                    horizontalLiftIntegral = (_horizontalLiftIntegral(Iwh, horizontalSpeedDumpFunction / angularSpeedDumpFunction) - _horizontalLiftIntegral(Iw, 1 / angularSpeedDumpFunction)) / horizontalSpeedDumpCoeff;
                }
                // int V_h dt = rotate(V_h0 |R_h|, arg R_h)
                horizontalSpeedIntegral = (speedProjection.Horizontal * horizontalLiftIntegral.Length).RotateByAngle3D(Point3D.YAxis * horizontalLiftIntegral.Arg);
            }
            // P_h = P_h0 + int V_h dt
            var horizontalPosition = positionProjection.Horizontal + horizontalSpeedIntegral;

            Speed = horizontalSpeed + verticalSpeed;
            Position = horizontalPosition + verticalPosition;
        }

        // x(j, f) = e^j * (Ei(-jf) - Ei(-j))
        private static Complex _horizontalLiftIntegral(double j, double x)
        {
            return Complex.Exp(Complex.I * j) * (Complex.ExpIntOfImaginaryArg(-j, -j * x));
        }

        public struct ProjectionToSurface
        {
            public Point3D.ProjectionToNormal Position, Speed;
        }

        public ProjectionToSurface ProjectToSurface(ASurface surface)
        {
            var surfaceNormal = surface.Normal;
            return new ProjectionToSurface()
            {
                Position = (Position - surface.Position).ProjectToNormalVector(surfaceNormal),
                Speed = (Speed - surface.Speed).ProjectToNormalVector(surfaceNormal)
            };
        }

        public void RestoreFromSurfaceProjection(ASurface surface, ProjectionToSurface projection)
        {
            Position = surface.Position + projection.Position.Full;
            Speed = surface.Speed + projection.Speed.Full;
        }

        public void ProcessHit(ASurface surface, double horizontalHitCoeff, double verticalHitCoeff, int side = 1)
        {
            var surfaceNormal = surface.Normal * side;
            var projection = ProjectToSurface(surface);
            projection.Position.Vertical = 2 * Constants.BallRadius * surfaceNormal - projection.Position.Vertical;
            projection.Speed.Vertical *= -verticalHitCoeff;
            var ballPoint = -Constants.BallRadius * surfaceNormal;
            var fullPerpendicularSpeed = projection.Speed.Horizontal + Point3D.VectorMult(ballPoint, AngularSpeed);
            var force = -horizontalHitCoeff * fullPerpendicularSpeed;
            projection.Speed.Horizontal += force;
            AngularSpeed += Point3D.VectorMult(force, ballPoint.Normal) / Constants.BallRadius;
            RestoreFromSurfaceProjection(surface, projection);
        }
    }
}
