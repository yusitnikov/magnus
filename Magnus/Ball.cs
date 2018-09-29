using Mathematics;
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
        public Point3D MarkPoint = Point3D.XAxis;

        // V' = L V x W - D |V| V + G
        public Point3D Force => Constants.BallLiftCoeff * Point3D.VectorMult(Speed, AngularSpeed) - Constants.BallDumpCoeff * Speed.Length * Speed + GravityForce;
        // W' = -AD W sqrt |W|
        public Point3D AngularForce => -Constants.BallAngularDumpCoeff * AngularSpeed * Math.Sqrt(AngularSpeed.Length);

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

        public void DoStep(double dt)
        {
            Point3D force = Force;
            Point3D angularForce = AngularForce;

            Position += Speed * dt + force * (dt * dt / 2);
            MarkPoint = MarkPoint.RotateByAngle3D(AngularSpeed * dt);
            Speed += force * dt;
            AngularSpeed += angularForce * dt;
        }

        private class simplifiedStepCalculationVars
        {
            // temporary variables:
            public Point3D.ProjectionToNormal positionProjection, speedProjection, angularSpeedProjection;
            public double angularSpeedDumpCoeff, angularSpeedDumpFunction, angularSpeedDumpFunctionIntegral;
            public double horizontalSpeedDumpCoeff, horizontalSpeedDumpFunction, horizontalSpeedDumpFunctionIntegral;
            public double horizontalRotateAngle;
            public Point3D horizontalSpeedBeforeRotation, horizontalSpeed;
            public Point3D verticalLiftForce, verticalSpeed;
            public double angularSpeedDumpInverseIntegral, horizontalSpeedDumpInverseIntegral;
            public double verticalLiftIntegral, gravityIntegral;
            public Point3D verticalPosition;
            public double I0, Iwh, Iw;
            public Complex horizontalLiftIntegral;
            public Point3D horizontalSpeedIntegral;
            public Point3D horizontalPosition;

            // results:
            public Point3D Position, Speed, AngularSpeed;
        }
        public class SimplifiedStepDerivatives
        {
            public int VarsCount;
            public double[] TDv;
            public Ball[] RelativeBallStateDv;
        }
        public static Ball DoStepSimplified(Ball relativeBallState, double t)
        {
            return DoStepSimplified(relativeBallState, t, null, out SimplifiedStepDerivatives tmp);
        }
        public static Ball DoStepSimplified(Ball relativeBallState, double t, SimplifiedStepDerivatives derivatives, out SimplifiedStepDerivatives outDerivatives)
        {
            outDerivatives = null;
            var r = new simplifiedStepCalculationVars();
            int varsCount = 0;
            simplifiedStepCalculationVars[] resultDerivatives = null;
            if (derivatives != null)
            {
                varsCount = derivatives.VarsCount;
                outDerivatives = new SimplifiedStepDerivatives()
                {
                    VarsCount = varsCount,
                    RelativeBallStateDv = new Ball[varsCount]
                };
                resultDerivatives = new simplifiedStepCalculationVars[varsCount];
                for (var i = 0; i < varsCount; i++)
                {
                    resultDerivatives[i] = new simplifiedStepCalculationVars();
                }
            }

            // P_v0, P_h0
            r.positionProjection = relativeBallState.Position.ProjectToHorizontalSurface();
            // V_v0, V_h0
            r.speedProjection = relativeBallState.Speed.ProjectToHorizontalSurface();
            // W_v0, W_h0
            r.angularSpeedProjection = relativeBallState.AngularSpeed.ProjectToHorizontalSurface();
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];
                    var bdv = derivatives.RelativeBallStateDv[i];

                    rdv.positionProjection = bdv.Position.ProjectToHorizontalSurface();
                    rdv.speedProjection = bdv.Speed.ProjectToHorizontalSurface();
                    rdv.angularSpeedProjection = bdv.AngularSpeed.ProjectToHorizontalSurface();
                }
            }

            // W' = -AD W sqrt |W|
            // kw = AD sqrt |W_0| / 2
            var relativeAngularSpeedLength = relativeBallState.AngularSpeed.Length;
            var relativeAngularSpeedLengthSqrt = Math.Sqrt(relativeAngularSpeedLength);
            r.angularSpeedDumpCoeff = Constants.BallAngularDumpCoeff * relativeAngularSpeedLengthSqrt / 2;
            // fw = kw t + 1
            r.angularSpeedDumpFunction = r.angularSpeedDumpCoeff * t + 1;
            // Fw = int dt / fw^2 = (1 - 1 / fw) / kw
            // kw = 0 => Fw = t
            r.angularSpeedDumpFunctionIntegral = r.angularSpeedDumpCoeff == 0 ? t : (1 - 1 / r.angularSpeedDumpFunction) / r.angularSpeedDumpCoeff;
            // W = W_0 / fw^2
            r.AngularSpeed = relativeBallState.AngularSpeed / (r.angularSpeedDumpFunction * r.angularSpeedDumpFunction);
            if (derivatives != null)
            {
                var angularSpeedDumpCoeff = relativeBallState.AngularSpeed * (Constants.BallAngularDumpCoeff / relativeAngularSpeedLength / relativeAngularSpeedLengthSqrt / 4);
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];
                    var bdv = derivatives.RelativeBallStateDv[i];
                    var tdv = derivatives.TDv != null ? derivatives.TDv[i] : 0;

                    // d|W_0| = dot(W_0, dW_0) / |W_0|
                    // d(sqrt |W_0|) = d|W_0| / (2 sqrt |W_0|) = dot(W_0, dW_0) / (2 |W_0|^1.5)
                    rdv.angularSpeedDumpCoeff = Point3D.ScalarMult(angularSpeedDumpCoeff, bdv.AngularSpeed);
                    rdv.angularSpeedDumpFunction = rdv.angularSpeedDumpCoeff * t + r.angularSpeedDumpCoeff * tdv;
                    // dFw = (dfw / fw^2 - Fw * dkw) / kw
                    rdv.angularSpeedDumpFunctionIntegral = rdv.angularSpeedDumpCoeff == 0
                        ? tdv
                        : (rdv.angularSpeedDumpFunction / (r.angularSpeedDumpFunction * r.angularSpeedDumpFunction) - r.angularSpeedDumpFunctionIntegral * rdv.angularSpeedDumpCoeff) / r.angularSpeedDumpCoeff;
                    // dW = dW_0 / fw^2 - 2 W_0 * dfw / fw^3
                    rdv.AngularSpeed = bdv.AngularSpeed / (r.angularSpeedDumpFunction * r.angularSpeedDumpFunction)
                                   - 2 * rdv.angularSpeedDumpFunction * r.AngularSpeed / r.angularSpeedDumpFunction;
                }
            }

            // V_h' = L V_h x W_v - D |V_h| V_h
            // kh = D |V_h0|
            r.horizontalSpeedDumpCoeff = Constants.BallDumpCoeff * r.speedProjection.Horizontal.Length;
            // fh = kh t + 1
            r.horizontalSpeedDumpFunction = r.horizontalSpeedDumpCoeff * t + 1;
            // int fh dt = kh t^2/2 + t
            r.horizontalSpeedDumpFunctionIntegral = r.horizontalSpeedDumpCoeff * t * t / 2 + t;
            // |V_h| = |V_h0| / fh
            // A_h = L W_v0 Fw
            r.horizontalRotateAngle = Constants.BallLiftCoeff * r.angularSpeedDumpFunctionIntegral * r.angularSpeedProjection.Vertical.Y;
            // V_h0 / fh
            r.horizontalSpeedBeforeRotation = r.speedProjection.Horizontal / r.horizontalSpeedDumpFunction;
            // V_h = rotate(V_h0 / fh, A_h)
            r.horizontalSpeed = r.horizontalSpeedBeforeRotation.RotateYaw(r.horizontalRotateAngle);
            if (derivatives != null)
            {
                var horizontalSpeedNormal = r.speedProjection.Horizontal.Normal;
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];
                    var tdv = derivatives.TDv != null ? derivatives.TDv[i] : 0;

                    rdv.horizontalSpeedDumpCoeff = Constants.BallDumpCoeff * Point3D.ScalarMult(horizontalSpeedNormal, rdv.speedProjection.Horizontal);
                    rdv.horizontalSpeedDumpFunction = rdv.horizontalSpeedDumpCoeff * t + r.horizontalSpeedDumpCoeff * tdv;
                    rdv.horizontalSpeedDumpFunctionIntegral = rdv.horizontalSpeedDumpCoeff * t * t / 2 + r.horizontalSpeedDumpCoeff * t * tdv + tdv;
                    rdv.horizontalRotateAngle = Constants.BallLiftCoeff * (r.angularSpeedDumpFunctionIntegral * rdv.angularSpeedProjection.Vertical.Y + rdv.angularSpeedDumpFunctionIntegral * r.angularSpeedProjection.Vertical.Y);
                    rdv.horizontalSpeedBeforeRotation = (rdv.speedProjection.Horizontal - rdv.horizontalSpeedDumpFunction * r.horizontalSpeedBeforeRotation) / r.horizontalSpeedDumpFunction;
                    rdv.horizontalSpeed = r.horizontalSpeedBeforeRotation.GetRotateYawDerivative(r.horizontalRotateAngle, rdv.horizontalSpeedBeforeRotation, rdv.horizontalRotateAngle);
                }
            }

            // V_v' = L V_h0 x W_h / fh - D |V_h| V_v + G
            // L V_h0 x W_h0
            r.verticalLiftForce = Constants.BallLiftCoeff * Point3D.VectorMult(r.speedProjection.Horizontal, r.angularSpeedProjection.Horizontal);
            // V_v = (V_v0 + L V_h0 x W_h0 (int dt / fw^2) + G (int fh dt)) / fh
            var verticalSpeedDividend = r.speedProjection.Vertical + r.angularSpeedDumpFunctionIntegral * r.verticalLiftForce + GravityForce * r.horizontalSpeedDumpFunctionIntegral;
            r.verticalSpeed = verticalSpeedDividend / r.horizontalSpeedDumpFunction;
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];

                    rdv.verticalLiftForce = Constants.BallLiftCoeff * (Point3D.VectorMult(rdv.speedProjection.Horizontal, r.angularSpeedProjection.Horizontal) + Point3D.VectorMult(r.speedProjection.Horizontal, rdv.angularSpeedProjection.Horizontal));
                    var verticalSpeedDividendDerivative = rdv.speedProjection.Vertical
                        + rdv.angularSpeedDumpFunctionIntegral * r.verticalLiftForce + r.angularSpeedDumpFunctionIntegral * rdv.verticalLiftForce
                        + GravityForce * rdv.horizontalSpeedDumpFunctionIntegral;
                    rdv.verticalSpeed = (verticalSpeedDividendDerivative - rdv.horizontalSpeedDumpFunction * r.verticalSpeed) / r.horizontalSpeedDumpFunction;
                }
            }

            r.Speed = r.horizontalSpeed + r.verticalSpeed;
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];

                    rdv.Speed = rdv.horizontalSpeed + rdv.verticalSpeed;
                }
            }

            // lw = int dt / fw = log fw / kw
            r.angularSpeedDumpInverseIntegral = r.angularSpeedDumpCoeff == 0 ? t : Math.Log(r.angularSpeedDumpFunction) / r.angularSpeedDumpCoeff;
            // lh = int dt / fh = log fh / kh
            r.horizontalSpeedDumpInverseIntegral = r.horizontalSpeedDumpCoeff == 0 ? t : Math.Log(r.horizontalSpeedDumpFunction) / r.horizontalSpeedDumpCoeff;
            // P_v = P_v0 + int V_v dt
            r.verticalPosition = r.positionProjection.Vertical + r.horizontalSpeedDumpInverseIntegral * r.speedProjection.Vertical;
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];
                    var tdv = derivatives.TDv != null ? derivatives.TDv[i] : 0;

                    // dlw = (dfw / fw - lw dkw) / kw
                    rdv.angularSpeedDumpInverseIntegral = r.angularSpeedDumpCoeff == 0 ? tdv : (rdv.angularSpeedDumpFunction / r.angularSpeedDumpFunction - rdv.angularSpeedDumpCoeff * r.angularSpeedDumpInverseIntegral) / r.angularSpeedDumpCoeff;
                    // dlh = (dfh / fh - lh dkh) / kh
                    rdv.horizontalSpeedDumpInverseIntegral = r.horizontalSpeedDumpCoeff == 0 ? tdv : (rdv.horizontalSpeedDumpFunction / r.horizontalSpeedDumpFunction - rdv.horizontalSpeedDumpCoeff * r.horizontalSpeedDumpInverseIntegral) / r.horizontalSpeedDumpCoeff;
                    rdv.verticalPosition = rdv.positionProjection.Vertical + rdv.horizontalSpeedDumpInverseIntegral * r.speedProjection.Vertical + r.horizontalSpeedDumpInverseIntegral * rdv.speedProjection.Vertical;
                }
            }
            if (r.horizontalSpeedDumpCoeff == 0)
            {
                // P_v = P_v0 + V_v0 t + G t^2 / 2
                r.verticalPosition += GravityForce * (t * t / 2);
                if (derivatives != null && derivatives.TDv != null)
                {
                    for (var i = 0; i < varsCount; i++)
                    {
                        var rdv = resultDerivatives[i];
                        var tdv = derivatives.TDv[i];

                        rdv.verticalPosition += GravityForce * t * tdv;
                    }
                }
            }
            else
            {
                // P_v = P_v0 + lh V_v0 + L V_h0 x W_h0 (lw - lh) / (kh - kw) + G (fh^2 - 1 - 2 kh lh) / 4kh^2
                var verticalLiftIntegralDividend = r.angularSpeedDumpInverseIntegral - r.horizontalSpeedDumpInverseIntegral;
                var verticalLiftIntegralDivisor = r.horizontalSpeedDumpCoeff - r.angularSpeedDumpCoeff;
                r.verticalLiftIntegral = verticalLiftIntegralDividend / verticalLiftIntegralDivisor;
                var gravityIntegralDividend = r.horizontalSpeedDumpFunction * r.horizontalSpeedDumpFunction - 1 - 2 * r.horizontalSpeedDumpCoeff * r.horizontalSpeedDumpInverseIntegral;
                var gravityIntegralDivisor = 4 * r.horizontalSpeedDumpCoeff * r.horizontalSpeedDumpCoeff;
                r.gravityIntegral = gravityIntegralDividend / gravityIntegralDivisor;
                r.verticalPosition += r.verticalLiftIntegral * r.verticalLiftForce + r.gravityIntegral * GravityForce;
                if (derivatives != null)
                {
                    for (var i = 0; i < varsCount; i++)
                    {
                        var rdv = resultDerivatives[i];

                        var verticalLiftIntegralDividendDerivative = rdv.angularSpeedDumpInverseIntegral - rdv.horizontalSpeedDumpInverseIntegral;
                        var verticalLiftIntegralDivisorDerivative = rdv.horizontalSpeedDumpCoeff - rdv.angularSpeedDumpCoeff;
                        rdv.verticalLiftIntegral = (verticalLiftIntegralDividendDerivative - verticalLiftIntegralDivisorDerivative * r.verticalLiftIntegral) / verticalLiftIntegralDivisor;
                        var gravityIntegralDividendDerivative = rdv.horizontalSpeedDumpFunction * r.horizontalSpeedDumpFunction + r.horizontalSpeedDumpFunction * rdv.horizontalSpeedDumpFunction
                                                              - 2 * (rdv.horizontalSpeedDumpCoeff * r.horizontalSpeedDumpInverseIntegral + r.horizontalSpeedDumpCoeff * rdv.horizontalSpeedDumpInverseIntegral);
                        var gravityIntegralDivisorDerivative = 8 * r.horizontalSpeedDumpCoeff * rdv.horizontalSpeedDumpCoeff;
                        rdv.gravityIntegral = (gravityIntegralDividendDerivative - gravityIntegralDivisorDerivative * r.gravityIntegral) / gravityIntegralDivisor;
                        rdv.verticalPosition += rdv.verticalLiftIntegral * r.verticalLiftForce + r.verticalLiftIntegral * rdv.verticalLiftForce + rdv.gravityIntegral * GravityForce;
                    }
                }
            }

            // int V_h dt
            if (r.horizontalSpeedDumpCoeff == 0)
            {
                // V_h = 0
                r.horizontalSpeedIntegral = Point3D.Empty;
                if (derivatives != null)
                {
                    for (var i = 0; i < varsCount; i++)
                    {
                        var rdv = resultDerivatives[i];

                        rdv.horizontalSpeedIntegral = Point3D.Empty;
                    }
                }
            }
            else
            {
                // R_h = int (e^iA_h / fh) dt
                if (r.angularSpeedDumpCoeff == 0)
                {
                    // R_h = lh
                    r.horizontalLiftIntegral = r.horizontalSpeedDumpInverseIntegral;
                    if (derivatives != null)
                    {
                        for (var i = 0; i < varsCount; i++)
                        {
                            var rdv = resultDerivatives[i];

                            rdv.horizontalLiftIntegral = rdv.horizontalSpeedDumpInverseIntegral;
                        }
                    }
                }
                else
                {
                    // I_0 = L W_v0
                    r.I0 = Constants.BallLiftCoeff * r.angularSpeedProjection.Vertical.Y;
                    // I_wh = I_0 / (kw - kh)
                    r.Iwh = r.I0 / (r.angularSpeedDumpCoeff - r.horizontalSpeedDumpCoeff);
                    // I_w = I_0 / kw
                    r.Iw = r.I0 / r.angularSpeedDumpCoeff;
                    // x(j, f) = e^ij * (Ei(-ijf) - Ei(-ij))
                    // R_h = (x(I_wh, fh / fw) - x(I_w, 1 / fw)) / kh
                    var horizontalLiftIntegralDividend = _horizontalLiftIntegral(r.Iwh, r.horizontalSpeedDumpFunction / r.angularSpeedDumpFunction) - _horizontalLiftIntegral(r.Iw, 1 / r.angularSpeedDumpFunction);
                    r.horizontalLiftIntegral = horizontalLiftIntegralDividend / r.horizontalSpeedDumpCoeff;
                    if (derivatives != null)
                    {
                        for (var i = 0; i < varsCount; i++)
                        {
                            var rdv = resultDerivatives[i];

                            rdv.I0 = Constants.BallLiftCoeff * rdv.angularSpeedProjection.Vertical.Y;
                            rdv.Iwh = (rdv.I0 - (rdv.angularSpeedDumpCoeff - rdv.horizontalSpeedDumpCoeff) * r.Iwh) / (r.angularSpeedDumpCoeff - r.horizontalSpeedDumpCoeff);
                            rdv.Iw = (rdv.I0 - rdv.angularSpeedDumpCoeff * r.Iw) / r.angularSpeedDumpCoeff;
                            var horizontalLiftIntegralDividendDerivative = _horizontalLiftIntegralDerivative(
                                    r.Iwh, r.horizontalSpeedDumpFunction / r.angularSpeedDumpFunction,
                                    rdv.Iwh, (rdv.horizontalSpeedDumpFunction - rdv.angularSpeedDumpFunction * r.horizontalSpeedDumpFunction / r.angularSpeedDumpFunction) / r.angularSpeedDumpFunction
                                )
                                - _horizontalLiftIntegralDerivative(
                                    r.Iw, 1 / r.angularSpeedDumpFunction,
                                    rdv.Iw, -rdv.angularSpeedDumpFunction / (r.angularSpeedDumpFunction * r.angularSpeedDumpFunction)
                                );
                            rdv.horizontalLiftIntegral = (horizontalLiftIntegralDividendDerivative - rdv.horizontalSpeedDumpCoeff * r.horizontalLiftIntegral) / r.horizontalSpeedDumpCoeff;
                        }
                    }
                }
                // int V_h dt = rotate(V_h0 |R_h|, arg R_h)
                r.horizontalSpeedIntegral = (r.speedProjection.Horizontal * r.horizontalLiftIntegral.Length).RotateYaw(r.horizontalLiftIntegral.Arg);
                if (derivatives != null)
                {
                    for (var i = 0; i < varsCount; i++)
                    {
                        var rdv = resultDerivatives[i];

                        rdv.horizontalSpeedIntegral = (r.speedProjection.Horizontal * r.horizontalLiftIntegral.Length).GetRotateYawDerivative(
                            r.horizontalLiftIntegral.Arg,
                            rdv.speedProjection.Horizontal * r.horizontalLiftIntegral.Length + r.speedProjection.Horizontal * r.horizontalLiftIntegral.GetLengthDerivative(rdv.horizontalLiftIntegral),
                            r.horizontalLiftIntegral.GetArgDerivative(rdv.horizontalLiftIntegral)
                        );
                    }
                }
            }
            // P_h = P_h0 + int V_h dt
            r.horizontalPosition = r.positionProjection.Horizontal + r.horizontalSpeedIntegral;
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];

                    rdv.horizontalPosition = rdv.positionProjection.Horizontal + rdv.horizontalSpeedIntegral;
                }
            }

            r.Position = r.horizontalPosition + r.verticalPosition;
            if (derivatives != null)
            {
                for (var i = 0; i < varsCount; i++)
                {
                    var rdv = resultDerivatives[i];

                    rdv.Position = rdv.horizontalPosition + rdv.verticalPosition;

                    outDerivatives.RelativeBallStateDv[i] = new Ball()
                    {
                        Position = rdv.Position,
                        Speed = rdv.Speed,
                        AngularSpeed = rdv.AngularSpeed
                    };
                }
            }

            return new Ball()
            {
                Position = r.Position,
                Speed = r.Speed,
                AngularSpeed = r.AngularSpeed
            };
        }

        // x(j, f) = e^j * (Ei(-jf) - Ei(-j))
        private static Complex _horizontalLiftIntegral(double j, double x)
        {
            return Complex.Exp(Complex.I * j) * Complex.ExpIntOfImaginaryArg(-j, -j * x);
        }

        private static Complex _horizontalLiftIntegralDerivative(double j, double x, double dj, double dx)
        {
            return Complex.Exp(Complex.I * j) * (Complex.I * dj * Complex.ExpIntOfImaginaryArg(-j, -j * x) + Complex.ExpIntOfImaginaryArgDerivative(-j, -j * x, -dj, -dj * x - j * dx));
        }

        public class ProjectionToSurface
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
