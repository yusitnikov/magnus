namespace Magnus.Strategies
{
    class SuperBlocker : Strategy
    {
        public override double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight * Misc.Rnd(0.2, 0.4);
        }
    }
}
