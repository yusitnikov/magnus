namespace Magnus.Strategies
{
    public class Strategy
    {
        public virtual double GetBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * Misc.Rnd(0.2, 0.8);
        }

        public virtual double GetAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(-30, 60));
        }

        public virtual double GetVelocityAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(-90, 90));
        }

        public virtual double GetHitSpeed()
        {
            return Misc.Rnd(0, 1);
        }

        public virtual double GetServeHitSpeed(double ballX)
        {
            //return Misc.Rnd(0, 0.5) * ballX / Constants.HalfTableLength;
            return Misc.Rnd(0.2, 0.7) * ballX / Constants.HalfTableLength;
        }

        public virtual double GetServeAttackAngle()
        {
            return Misc.FromDegrees(Misc.Rnd(20, 100));
        }

        public virtual double GetServeVelocityAttackAngle()
        {
            var rnd = Misc.Rnd(-1, 1);
            return Misc.FromDegrees(90 + 60 * rnd * rnd * rnd);
        }

        public override string ToString()
        {
            var className = GetType().Name;
            return className == "Strategy" ? "Default" : className;
        }
    }
}
