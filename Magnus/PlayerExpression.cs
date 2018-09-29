using Mathematics.Expressions;
using Mathematics.Math3D;

namespace Magnus
{
    class PlayerExpression : ASurfaceExpression
    {
        public int Index;

        public int Side => Misc.GetPlayerSideByIndex(Index);

        public Expression AnglePitch, AngleYaw;

        public override Point3DExpression Normal
        {
            get => TranslateVectorFromBatCoords(Point3D.YAxis);
            set
            {
                AnglePitch = value.Pitch;
                AngleYaw = value.Yaw;
            }
        }

        public Player Evaluate(int cacheGeneration)
        {
            if (cacheGeneration == 0)
            {
                cacheGeneration = Expression.NextAutoIncrementId;
            }
            return new Player()
            {
                Index = Index,
                Position = Position.Evaluate(cacheGeneration),
                Speed = Speed.Evaluate(cacheGeneration),
                AnglePitch = AnglePitch.Evaluate(cacheGeneration),
                AngleYaw = AngleYaw.Evaluate(cacheGeneration)
            };
        }

        public Point3DExpression TranslateVectorFromBatCoords(Point3DExpression point)
        {
            return point.RotatePitch(AnglePitch).RotateYaw(AngleYaw);
        }

        public Point3DExpression TranslatePointFromBatCoords(Point3DExpression point)
        {
            return Position + TranslateVectorFromBatCoords(point);
        }
    }
}
