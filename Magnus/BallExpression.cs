using Mathematics.Expressions;
using Mathematics.Math3D;

namespace Magnus
{
    class BallExpression
    {
        public static readonly Point3DExpression GravityForce = Ball.GravityForce;

        public Point3DExpression Position, Speed;
        public Point3DExpression AngularSpeed;

        public BallExpression(Point3DExpression position, Point3DExpression speed, Point3DExpression angularSpeed)
        {
            Position = position;
            Speed = speed;
            AngularSpeed = angularSpeed;
        }
        public BallExpression(Ball ball) : this(ball.Position, ball.Speed, ball.AngularSpeed) { }

        public BallExpression GetSimplifiedTrajectory(Variable dt)
        {
            // P_v0, P_h0
            var positionProjection = Position.ProjectToNormalVector(Surface.Horizontal.Normal);
            // V_v0, V_h0
            var speedProjection = Speed.ProjectToNormalVector(Surface.Horizontal.Normal);
            // W_v0, W_h0
            var angularSpeedProjection = AngularSpeed.ProjectToNormalVector(Surface.Horizontal.Normal);

            // W' = -AD W sqrt |W|
            // kw = AD sqrt |W_0| / 2
            var angularSpeedDumpCoeff = Constants.BallAngularDumpCoeff * new Sqrt(AngularSpeed.Length) / 2;
            // fw = kw t + 1
            var angularSpeedDumpFunction = angularSpeedDumpCoeff * dt + 1;
            // Fw = int dt / fw^2 = (1 - 1 / fw) / kw
            // kw = 0 => Fw = t
            var angularSpeedDumpFunctionIntegral = new Coalesce(angularSpeedDumpCoeff, dt, (1 - 1 / angularSpeedDumpFunction) / angularSpeedDumpCoeff);
            // W = W_0 / fw^2
            var angularSpeed = AngularSpeed / new Square(angularSpeedDumpFunction);

            // V_h' = L V_h x W_v - D |V_h| V_h
            // kh = D |V_h0|
            var horizontalSpeedDumpCoeff = Constants.BallDumpCoeff * speedProjection.Horizontal.Length;
            // fh = kh t + 1
            var horizontalSpeedDumpFunction = horizontalSpeedDumpCoeff * dt + 1;
            // int fh dt = kh t^2/2 + t
            var horizontalSpeedDumpFunctionIntegral = horizontalSpeedDumpCoeff * dt * dt / 2 + dt;
            // |V_h| = |V_h0| / fh
            // A_h = L W_v0 Fw
            var horizontalRotateAngle = Constants.BallLiftCoeff * angularSpeedDumpFunctionIntegral * angularSpeedProjection.Vertical;
            // V_h = rotate(V_h0 / fh, A_h)
            var horizontalSpeed = (speedProjection.Horizontal / horizontalSpeedDumpFunction).RotateByAngle3D(horizontalRotateAngle);

            // V_v' = L V_h0 x W_h / fh - D |V_h| V_v + G
            // L V_h0 x W_h0
            var verticalLiftForce = Constants.BallLiftCoeff * Point3DExpression.VectorMult(speedProjection.Horizontal, angularSpeedProjection.Horizontal);
            // V_v = (V_v0 + L V_h0 x W_h0 (int dt / fw^2) + G (int fh dt)) / fh
            var verticalSpeed = (speedProjection.Vertical + angularSpeedDumpFunctionIntegral * verticalLiftForce + GravityForce * horizontalSpeedDumpFunctionIntegral) / horizontalSpeedDumpFunction;

            // lw = int dt / fw = log fw / kw
            var angularSpeedDumpInverseIntegral = new Coalesce(angularSpeedDumpCoeff, dt, new Log(angularSpeedDumpFunction) / angularSpeedDumpCoeff);
            // lh = int dt / fh = log fh / kh
            var horizontalSpeedDumpInverseIntegral = new Coalesce(horizontalSpeedDumpCoeff, dt, new Log(horizontalSpeedDumpFunction) / horizontalSpeedDumpCoeff);
            // P_v = P_v0 + int V_v dt
            var verticalPosition = positionProjection.Vertical + horizontalSpeedDumpInverseIntegral * speedProjection.Vertical;
            // P_v = P_v0 + lh V_v0 + L V_h0 x W_h0 (lw - lh) / (kh - kw) + G (fh^2 - 1 - 2 kh lh) / 4kh^2
            var verticalLiftIntegral = (angularSpeedDumpInverseIntegral - horizontalSpeedDumpInverseIntegral) / (horizontalSpeedDumpCoeff - angularSpeedDumpCoeff);
            var gravityIntegral = (horizontalSpeedDumpFunction * horizontalSpeedDumpFunction - 1 - 2 * horizontalSpeedDumpCoeff * horizontalSpeedDumpInverseIntegral) / (4 * horizontalSpeedDumpCoeff * horizontalSpeedDumpCoeff);
            verticalPosition += Point3DExpression.Coalesce(
                horizontalSpeedDumpCoeff,
                // P_v = P_v0 + V_v0 t + G t^2 / 2
                GravityForce / 2 * new Square(dt),
                verticalLiftIntegral * verticalLiftForce + gravityIntegral * GravityForce
            );

            // I_0 = L W_v0
            var I0 = Constants.BallLiftCoeff * angularSpeedProjection.Vertical.Y;
            // I_wh = I_0 / (kw - kh)
            var Iwh = I0 / (angularSpeedDumpCoeff - horizontalSpeedDumpCoeff);
            // I_w = I_0 / kw
            var Iw = I0 / angularSpeedDumpCoeff;
            // x(j, f) = e^ij * (Ei(-ijf) - Ei(-ij))
            // R_h = int (e^iA_h / fh) dt
            var horizontalLiftIntegral = ComplexExpression.Coalesce(
                angularSpeedDumpCoeff,
                // R_h = lh
                horizontalSpeedDumpInverseIntegral,
                // R_h = (x(I_wh, fh / fw) - x(I_w, 1 / fw)) / kh
                (_horizontalLiftIntegral(Iwh, horizontalSpeedDumpFunction / angularSpeedDumpFunction) - _horizontalLiftIntegral(Iw, 1 / angularSpeedDumpFunction)) / horizontalSpeedDumpCoeff
            );
            // int V_h dt
            var horizontalSpeedIntegral = Point3DExpression.Coalesce(
                horizontalSpeedDumpCoeff,
                // V_h = 0
                Point3D.Empty,
                // int V_h dt = rotate(V_h0 |R_h|, arg R_h)
                (speedProjection.Horizontal * horizontalLiftIntegral.Length).RotateByAngle3D((Point3DExpression)Point3D.YAxis * horizontalLiftIntegral.Arg)
            );
            // P_h = P_h0 + int V_h dt
            var horizontalPosition = positionProjection.Horizontal + horizontalSpeedIntegral;

            return new BallExpression(
                (horizontalPosition + verticalPosition).Simplify(),
                (horizontalSpeed + verticalSpeed).Simplify(),
                angularSpeed.Simplify()
            );
        }

        // x(j, f) = e^j * (Ei(-jf) - Ei(-j))
        private static ComplexExpression _horizontalLiftIntegral(Expression j, Expression x)
        {
            return ComplexExpression.Exp(ComplexExpression.I * j) * (ComplexExpression.ExpIntOfImaginaryArg(-j, -j * x));
        }

        public static implicit operator BallExpression(Ball ball)
        {
            return new BallExpression(ball);
        }
    }
}
