namespace Magnus.Strategies
{
    class Passive : Strategy
    {
        public override double GetBackHitTime(double t1, double t2)
        {
            return t2 * Misc.Rnd(0.5, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.Rnd(-20, 40, true);
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0, 0.5);
        }
    }
}
