namespace Magnus
{
    class Surface : ASurface
    {
        public static readonly Surface Horizontal = new Surface(DoublePoint3D.Empty, DoublePoint3D.Empty, DoublePoint3D.YAxis);

        public override DoublePoint3D Normal { get; set; }

        public Surface(DoublePoint3D position, DoublePoint3D speed, DoublePoint3D normal)
        {
            Position = position;
            Speed = speed;
            Normal = normal;
        }
    }
}
