namespace Magnus
{
    class AimByWaitPosition : AimByCoords<AimCoordByWaitPosition>
    {
        public AimByWaitPosition(Player aimPlayer, Player aimPlayer0, double aimT0) : base(aimPlayer, aimPlayer0, double.PositiveInfinity, aimT0)
        {
        }
    }
}
