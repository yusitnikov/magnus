namespace Magnus
{
    class Constants
    {
        // table/net/ball/bat size constants
        public const double tw = 760, th = 400, nh = 75, br = 10, btr = nh / 2;

        // simulation frame time
        public const double sdt = 0.01;

        // player strength constants
        public const double mv = 1000, ma = 800;

        // ball forces constants
        public const double cd = 0.0004, cl = 0.00012, cw = 0.004, g = 100;

        // hit coefficients
        public const double bhky = 0.58, bhkx = 0.8, thky = 0.9, thkx = 0.2;

        // player sides
        public const int LEFT_SIDE = 0, RIGHT_SIDE = 1;
    }
}
