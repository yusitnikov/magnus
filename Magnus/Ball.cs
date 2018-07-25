using System;

namespace Magnus
{
    class Ball
    {
        public static readonly DoublePoint3D GravityForce = new DoublePoint3D(0, -Constants.GravityForce, 0);

        public DoublePoint3D Position, Speed;
        public DoublePoint3D AngularSpeed;
        // A point on the ball that rotates with the ball and indicates ball rotation in UI
        public DoublePoint3D MarkPoint;

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
            var simplifiedSpeed = simplified ? new DoublePoint3D(Speed.X, 0, Speed.Z) : Speed;
            // V' = L V x W - D |V| V + G
            DoublePoint3D force = Constants.BallLiftCoeff * DoublePoint3D.VectorMult(simplifiedSpeed, AngularSpeed) - Constants.BallDumpCoeff * simplifiedSpeed.Length * Speed + GravityForce;
            // W' = -AD W sqrt |W|
            DoublePoint3D angularForce = -Constants.BallAngularDumpCoeff * AngularSpeed * Math.Sqrt(AngularSpeed.Length);

            Position += Speed * dt + force * (dt * dt / 2);
            MarkPoint = MarkPoint.RotateByAngle3D(AngularSpeed * dt).Normal * Constants.BallRadius;
            Speed += force * dt;
            AngularSpeed += angularForce * dt;
        }

        public void DoStepSimplified(Ball relativeBallState, double dt)
        {
            // V_v0, V_h0
            var speedProjection = relativeBallState.Speed.ProjectToNormalVector(Surface.Horizontal.Normal);
            // W_v0, W_h0
            var angularSpeedProjection = relativeBallState.AngularSpeed.ProjectToNormalVector(Surface.Horizontal.Normal);

            // W' = -AD W sqrt |W|
            // kf = AD sqrt |W_0| / 2
            var angularSpeedDumpCoeff = Constants.BallAngularDumpCoeff * Math.Sqrt(relativeBallState.AngularSpeed.Length) / 2;
            // f = kf t + 1
            var angularSpeedDumpFunction = angularSpeedDumpCoeff * dt + 1;
            // int dt / f^2 = (1 - 1 / f) / kf
            var angularSpeedDumpFunctionIntegral = (1 - 1 / angularSpeedDumpFunction) / angularSpeedDumpCoeff;
            // W = W_0 / f^2
            AngularSpeed = relativeBallState.AngularSpeed / (angularSpeedDumpFunction * angularSpeedDumpFunction);

            // V_h' = L V_h x W_v - D |V_h| V_h
            // kg = D |V_h0|
            var horizontalSpeedDumpCoeff = Constants.BallDumpCoeff * speedProjection.Horizontal.Length;
            // g = kg t + 1
            var horizontalSpeedDumpFunction = horizontalSpeedDumpCoeff * dt + 1;
            // int g dt = kg t^2/2 + t
            var horizontalSpeedDumpFunctionIntegral = horizontalSpeedDumpCoeff * dt * dt / 2 + dt;
            // |V_h| = |V_h0| / g
            // V_h = rotate(V_h0 / g, L W_v0 int dt / f^2)
            var horizontalSpeed = (speedProjection.Horizontal / horizontalSpeedDumpFunction).RotateByAngle3D(Constants.BallLiftCoeff * angularSpeedDumpFunctionIntegral * angularSpeedProjection.Vertical);
            Speed.X = horizontalSpeed.X;
            Speed.Z = horizontalSpeed.Z;

            // V_v' = L V_h0 x W_h / g - D |V_h| V_v + G
            // V_v = (V_v0 + L V_h0 x W_h0 (int dt / f^2) + G (int g dt)) / g
            var verticalSpeed = (speedProjection.Vertical + Constants.BallLiftCoeff * angularSpeedDumpFunctionIntegral * DoublePoint3D.VectorMult(speedProjection.Horizontal, angularSpeedProjection.Horizontal) + GravityForce * horizontalSpeedDumpFunctionIntegral) / horizontalSpeedDumpFunction;
            Speed.Y = verticalSpeed.Y;
        }

        public struct ProjectionToSurface
        {
            public DoublePoint3D.ProjectionToNormal Position, Speed;
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

        public void ProcessHit(ASurface surface, double horizontalHitCoeff, double verticalHitCoeff)
        {
            var surfaceNormal = surface.Normal;
            var projection = ProjectToSurface(surface);
            projection.Position.Vertical = 2 * Constants.BallRadius * surfaceNormal - projection.Position.Vertical;
            projection.Speed.Vertical *= -verticalHitCoeff;
            var ballPoint = -Constants.BallRadius * surfaceNormal;
            var fullPerpendicularSpeed = projection.Speed.Horizontal + DoublePoint3D.VectorMult(ballPoint, AngularSpeed);
            var force = -horizontalHitCoeff * fullPerpendicularSpeed;
            projection.Speed.Horizontal += force;
            AngularSpeed += DoublePoint3D.VectorMult(force, ballPoint.Normal) / Constants.BallRadius;
            RestoreFromSurfaceProjection(surface, projection);
        }
    }
}
