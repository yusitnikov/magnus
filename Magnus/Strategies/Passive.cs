namespace Magnus.Strategies
{
    class Passive : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * Misc.Rnd(0.5, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(-20, 40, true));
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0, 0.5);
        }
    }
}
