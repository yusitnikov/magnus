using System;

namespace Magnus
{
    class Constants
    {
        // units
        public const double LengthUnit = 1, TimeUnit = 1, SpeedUnit = LengthUnit / TimeUnit, ForceUnit = LengthUnit / TimeUnit / TimeUnit;

        // table/net/ball/bat size constants
        public const double TableLength = 274 * LengthUnit, HalfTableLength = TableLength / 2, TableWidth = 152.5 * LengthUnit, HalfTableWidth = TableWidth / 2, TableHeight = 76 * LengthUnit, TableThickness = LengthUnit, NetHeight = 15 * LengthUnit, NetWidth = TableWidth + NetHeight, HalfNetWidth = NetWidth / 2, BallRadius = 2 * LengthUnit, BatWidth = NetHeight, BatRadius = BatWidth / 2, BatLength = BatWidth * 1.2, BatBiggerRadius = BatLength / 2, BatThickness = LengthUnit;

        // simulation frame time
        public const double SimulationFrameTime = 0.001 * TimeUnit, SimplifiedSimulationFrameTime = 0.01 * TimeUnit;

        // player strength constants
        public const double MaxPlayerSpeed = 1500 * SpeedUnit, MaxPlayerForce = 12000 * ForceUnit;

        // ball forces constants
        public const double BallDumpCoeff = 0.002 / LengthUnit, BallLiftCoeff = 0.007, GravityForce = 980 * ForceUnit;
        public static readonly double BallAngularDumpCoeff = 0.08 / Math.Sqrt(TimeUnit);
        public const double MinTimeToMoveBeforeHit = 0.2 * TimeUnit, MinDistanceToMoveBeforeHit = 40 * LengthUnit;

        // hit coefficients
        public const double BallHitVerticalCoeff = 0.58, BallHitHorizontalCoeff = 0.6, TableHitVerticalCoeff = 0.9, TableHitHorizontalCoeff = 0.2;

        // player sides
        public const int LeftPlayerIndex = 0, RightPlayerIndex = 1;

        // player strategy constants
        public const double BatWaitX = HalfTableLength + BatWidth * 2, BatWaitY = NetHeight, MinBallServeX = 20 * LengthUnit, MaxBallServeX = 80 * LengthUnit, MinBallServeY = NetHeight, MaxBallServeY = NetHeight * 2, MinBallServeThrowSpeed = 200 * SpeedUnit, MaxBallServeThrowSpeed = 600 * SpeedUnit, MaxAttackAngleDifference = 72 * Math.PI / 180, MinHitY = -NetHeight * 2;

        // simulation constants
        public const double MaxThinkTimePerFrame = 0.02, MinNetCrossY = NetHeight + BallRadius * 2, SimulationNetMargin = HalfTableLength / 3, SimulationBordersMargin = BatWidth, MaxBallMaxHeight = NetHeight * 4;

        public const double ScreenZoom = 0.4;
    }
}
