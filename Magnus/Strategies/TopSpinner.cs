namespace Magnus.Strategies
{
    class TopSpinner : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight + (timeTillFall - timeTillMaxHeight) * Misc.Rnd(0.1, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(30, 60));
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0.6, 1);
        }
    }
}
