using Mathematics.Expressions;

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

        public Ball Evaluate(int cacheGeneration = 0)
        {
            if (cacheGeneration == 0)
            {
                cacheGeneration = Expression.NextAutoIncrementId;
            }
            return new Ball()
            {
                Position = Position.Evaluate(cacheGeneration),
                Speed = Speed.Evaluate(cacheGeneration),
                AngularSpeed = AngularSpeed.Evaluate(cacheGeneration)
            };
        }

        public BallExpression Derivate(Variable v)
        {
            return new BallExpression(
                Position.Derivate(v),
                Speed.Derivate(v),
                AngularSpeed.Derivate(v)
            );
        }

        public BallExpression Clone()
        {
            return new BallExpression(Position, Speed, AngularSpeed);
        }

        public class ProjectionToSurfaceExpression
        {
            public Point3DExpression.ProjectionToNormalExpression Position, Speed;
        }

        public ProjectionToSurfaceExpression ProjectToSurface(ASurfaceExpression surface)
        {
            var surfaceNormal = surface.Normal;
            return new ProjectionToSurfaceExpression()
            {
                Position = (Position - surface.Position).ProjectToNormalVector(surfaceNormal),
                Speed = (Speed - surface.Speed).ProjectToNormalVector(surfaceNormal)
            };
        }

        public void RestoreFromSurfaceProjection(ASurfaceExpression surface, ProjectionToSurfaceExpression projection)
        {
            Position = surface.Position + projection.Position.Full;
            Speed = surface.Speed + projection.Speed.Full;
        }

        public void ProcessHit(ASurfaceExpression surface, double horizontalHitCoeff, double verticalHitCoeff)
        {
            var surfaceNormal = surface.Normal;
            var projection = ProjectToSurface(surface);
            projection.Position.Vertical = 2 * Constants.BallRadius * surfaceNormal - projection.Position.Vertical;
            projection.Speed.Vertical *= -verticalHitCoeff;
            var ballPoint = -Constants.BallRadius * surfaceNormal;
            var fullPerpendicularSpeed = projection.Speed.Horizontal + Point3DExpression.VectorMult(ballPoint, AngularSpeed);
            var force = -horizontalHitCoeff * fullPerpendicularSpeed;
            projection.Speed.Horizontal += force;
            AngularSpeed += Point3DExpression.VectorMult(force, ballPoint.Normal) / Constants.BallRadius;
            RestoreFromSurfaceProjection(surface, projection);
        }

        public static implicit operator BallExpression(Ball ball)
        {
            return new BallExpression(ball);
        }
    }
}
