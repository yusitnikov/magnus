using Mathematics.Expressions;

namespace Magnus
{
    abstract class ASurfaceExpression
    {
        public Point3DExpression Position, Speed;

        public abstract Point3DExpression Normal { get; set; }
    }
}
