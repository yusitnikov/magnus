namespace Magnus.Strategies
{
    class BackSpinner : Strategy
    {
        public override double GetBackHitTime(double t1, double t2)
        {
            return t2 * Misc.Rnd(0.5, 0.8);
        }

        public override double GetAttackAngle()
        {
            return Misc.Rnd(-40, 30);
        }

        public override double GetVelocityAttackAngle()
        {
            return Misc.Rnd(70, 90);
        }

        public override double GetHitSpeed()
        {
            return Misc.Rnd(0.3, 1);
        }
    }
}
