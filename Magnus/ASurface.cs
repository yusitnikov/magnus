namespace Magnus
{
    abstract class ASurface
    {
        public DoublePoint3D Position, Speed;

        public abstract DoublePoint3D Normal { get; set; }
    }
}
