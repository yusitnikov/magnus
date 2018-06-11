namespace Magnus
{
    class Constants
    {
        // table/net/ball/bat size constants
        public const double TableWidth = 1520, HalfTableWidth = TableWidth / 2, TableHeight = 400, NetHeight = 75, BallRadius = 10, BatRadius = NetHeight / 2;

        // simulation frame time
        public const double SimulationFrameTime = 0.01;

        // player strength constants
        public const double MaxPlayerSpeed = 1000, MaxPlayerForce = 800;

        // ball forces constants
        public const double BallDumpCoeff = 0.0004, BallLiftCoeff = 0.00012, BallAngularDumpCoeff = 0.004, GravityForce = 100;

        // hit coefficients
        public const double BallHitVerticalCoeff = 0.58, BallHitHorizontalCoeff = 0.8, TableHitVerticalCoeff = 0.9, TableHitHorizontalCoeff = 0.2;

        // player sides
        public const int LeftPlayerIndex = 0, RightPlayerIndex = 1;
    }
}
