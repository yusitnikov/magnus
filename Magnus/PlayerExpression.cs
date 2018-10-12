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

        public Point3DExpression LowestPoint
        {
            get
            {
                var vector = TranslateVectorFromBatCoords(new Point3DExpression(Constants.BatRadius, Constant.Nil, Constant.Nil));
                return Position - vector * Expression.Sign(vector.Y);
            }
        }

        private string alias;
        public string Alias
        {
            get => alias;
            set
            {
                alias = value;
                if (value != null)
                {
                    Position.Alias = value + "Pos";
                    Speed.Alias = value + "Speed";
                    AnglePitch.Alias = value + "Pitch";
                    AngleYaw.Alias = value + "Yaw";
                }
            }
        }

        public Player Evaluate(int cacheGeneration = 0)
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

        public PlayerExpression Derivate(Variable v)
        {
            return new PlayerExpression()
            {
                Index = Index,
                Position = Position.Derivate(v),
                Speed = Speed.Derivate(v),
                AnglePitch = AnglePitch.Derivate(v),
                AngleYaw = AngleYaw.Derivate(v)
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
