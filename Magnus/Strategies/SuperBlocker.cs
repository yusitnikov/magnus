namespace Magnus.Strategies
{
    class SuperBlocker : Strategy
    {
        public override double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight * 0.2;
        }
        public override double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillMaxHeight * 0.4;
        }
    }
}
