namespace Magnus.Strategies
{
    class BackSpinner : Strategy
    {
        public override double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight;
        }
        public override double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight + (timeTillFall - timeTillMaxHeight) * 0.8;
        }

        public override double GetMinAttackAngle()
        {
            return Misc.FromDegrees(-40);
        }
        public override double GetMaxAttackAngle()
        {
            return Misc.FromDegrees(30);
        }

        public override double GetMinVelocityAttackAngle()
        {
            return Misc.FromDegrees(70);
        }
        public override double GetMaxVelocityAttackAngle()
        {
            return Misc.FromDegrees(90);
        }
    }
}
