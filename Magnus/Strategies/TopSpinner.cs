namespace Magnus.Strategies
{
    class TopSpinner : Strategy
    {
        public override double GetBackHitTime(double t1, double t2)
        {
            return t1 + (t2 - t1) * Misc.Rnd(0.1, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.Rnd(30, 60);
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0.6, 1);
        }
    }
}
