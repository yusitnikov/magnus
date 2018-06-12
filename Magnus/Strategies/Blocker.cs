namespace Magnus.Strategies
{
    class Blocker : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight * Misc.Rnd(0.2, 1);
        }

        public override double GetAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(10, 60));
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0.3, 0.5);
        }
    }
}
