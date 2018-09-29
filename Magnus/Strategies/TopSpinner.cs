namespace Magnus.Strategies
{
    class TopSpinner : Strategy
    {
        public override double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight + (timeTillFall - timeTillMaxHeight) * 0.1;
        }
        public override double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight + (timeTillFall - timeTillMaxHeight) * 0.8;
        }

        public override double GetMinAttackAngle()
        {
            return Misc.FromDegrees(30);
        }
        public override double GetMaxAttackAngle()
        {
            return Misc.FromDegrees(60);
        }

        public override double GetMinHitSpeed()
        {
            return 0.6;
        }
    }
}
