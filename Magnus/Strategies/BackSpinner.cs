namespace Magnus.Strategies
{
    class BackSpinner : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight + (timeTillFall - timeTillMaxHeight) * Misc.Rnd(0, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(-40, 30));
        }

        public override double GetVelocityAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(70, 90));
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0, 1);
        }
    }
}
