namespace Magnus.Strategies
{
    class Blocker : Strategy
    {
        public override double GetBackHitTime(double t1, double t2)
        {
            return t1 * Misc.Rnd(0.2, 1);
        }

        public override double GetAttackAngle()
        {
            return Misc.Rnd(10, 60);
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0.5, 1);
            //return Misc.Rnd(1, 0, true);
        }
    }
}
