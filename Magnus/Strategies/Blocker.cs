namespace Magnus.Strategies
{
    class Blocker : Strategy
    {
        public override double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight * 0.2;
        }
        public override double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight;
        }

        public override double GetMinAttackAngle()
        {
            return Misc.FromDegrees(10);
        }
        public override double GetMaxAttackAngle()
        {
            return Misc.FromDegrees(60);
        }

        public override double GetMinHitSpeed()
        {
            return 0.3;
        }
        public override double GetMaxHitSpeed()
        {
            return 0.5;
        }
    }
}
