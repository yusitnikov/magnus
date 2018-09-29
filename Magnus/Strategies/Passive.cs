namespace Magnus.Strategies
{
    class Passive : Strategy
    {
        public override double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * 0.5;
        }
        public override double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * 0.8;
        }

        public override double GetMinAttackAngle()
        {
            return Misc.FromDegrees(-20);
        }
        public override double GetMaxAttackAngle()
        {
            return Misc.FromDegrees(40);
        }

        public override double GetMaxHitSpeed()
        {
            return 0.5;
        }
    }
}
