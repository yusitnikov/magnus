namespace Magnus.Strategies
{
    class BackSpinner : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * Misc.Rnd(0.5, 0.8);
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
