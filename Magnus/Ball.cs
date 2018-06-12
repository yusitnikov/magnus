using System;

namespace Magnus
{
    class Ball
    {
        public DoublePoint Position, Speed;
        public double Angle, AngularSpeed;

        public int Side => Math.Sign(Position.X);

        public void CopyFrom(Ball ball)
        {
            Position = ball.Position;
            Angle = ball.Angle;
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
            var simplifiedSpeed = simplified ? new DoublePoint(Speed.X, 0) : Speed;
            DoublePoint force = Constants.BallLiftCoeff * AngularSpeed * simplifiedSpeed.RotateRight90() - Constants.BallDumpCoeff * simplifiedSpeed.Length * Speed - new DoublePoint(0, Constants.GravityForce);
            double angularForce = Constants.BallAngularDumpCoeff * AngularSpeed * Math.Sqrt(Math.Abs(AngularSpeed));

            Position += Speed * dt + force * (dt * dt / 2);
            Angle += AngularSpeed * dt;
            Speed += force * dt;
            AngularSpeed -= angularForce * dt;
        }

        public void DoStepSimplified(Ball relativeBallState, double dt)
        {
            double angularSpeedCharacteristic = calcAngularSpeedCharacteristic(relativeBallState.AngularSpeed, dt);
            AngularSpeed = Math.Sign(relativeBallState.AngularSpeed) / (angularSpeedCharacteristic * angularSpeedCharacteristic);
            Speed.X = 1 / (Constants.BallDumpCoeff * Math.Sign(relativeBallState.Speed.X) * dt + 1 / relativeBallState.Speed.X);
            Speed.Y = (relativeBallState.Speed.Y / relativeBallState.Speed.X - Constants.BallLiftCoeff * calcAngularSpeedCharacteristicDefiniteIntegral(relativeBallState.AngularSpeed, dt) - Constants.GravityForce * (Constants.BallDumpCoeff * Math.Sign(relativeBallState.Speed.X) * dt * dt / 2 + dt / relativeBallState.Speed.X)) * Speed.X;
        }

        private double calcAngularSpeedCharacteristic(double angularSpeed, double t)
        {
            return Constants.BallAngularDumpCoeff * t / 2 + 1 / Math.Sqrt(Math.Abs(angularSpeed));
        }

        private double calcAngularSpeedCharacteristicIndefiniteIntegral(double angularSpeed, double t)
        {
            return -Math.Sign(angularSpeed) * 2 / Constants.BallAngularDumpCoeff / calcAngularSpeedCharacteristic(angularSpeed, t);
        }

        private double calcAngularSpeedCharacteristicDefiniteIntegral(double angularSpeed, double t)
        {
            return calcAngularSpeedCharacteristicIndefiniteIntegral(angularSpeed, t) - calcAngularSpeedCharacteristicIndefiniteIntegral(angularSpeed, 0);
        }

        public Ball ProjectToBat(Player player)
        {
            var batNormal = DoublePoint.FromAngle(player.Angle);

            return new Ball()
            {
                Position = (Position - player.Position).ProjectToNormalVector(batNormal),
                Speed = (Speed - player.Speed).ProjectToNormalVector(batNormal),
                Angle = Angle - player.Angle,
                AngularSpeed = AngularSpeed
            };
        }

        public Ball ProjectFromBat(Player player)
        {
            var batNormal = DoublePoint.FromAngle(player.Angle);

            return new Ball()
            {
                Position = player.Position + Position.ProjectFromNormalVector(batNormal),
                Speed = player.Speed + Speed.ProjectFromNormalVector(batNormal),
                Angle = player.Angle + Angle,
                AngularSpeed = AngularSpeed
            };
        }

        public void ProcessHit(double horizontalHitCoeff, double verticalHitCoeff)
        {
            const double angularSpeedToPlainCoeff = Constants.BallRadius * Math.PI / 180;
            Position.Y = 2 * Constants.BallRadius - Position.Y;
            Speed.Y *= -verticalHitCoeff;
            double rollSpeedAtPoint = -AngularSpeed * angularSpeedToPlainCoeff;
            double force = -horizontalHitCoeff * (rollSpeedAtPoint + Speed.X);
            rollSpeedAtPoint += force;
            Speed.X += force;
            AngularSpeed = -rollSpeedAtPoint / angularSpeedToPlainCoeff;
        }
    }
}
