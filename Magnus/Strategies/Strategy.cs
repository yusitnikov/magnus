namespace Magnus.Strategies
{
    public class Strategy
    {
        public virtual double GetBackHitTime(double t1, double t2)
        {
            return t2 * Misc.Rnd(0.2, 0.8);
        }

        public virtual double GetAttackAngle()
        {
            return Misc.Rnd(-30, 60);
        }

        public virtual double GetVelocityAttackAngle()
        {
            return Misc.Rnd(-90, 90);
        }

        public virtual double GetHitSpeed()
        {
            return Misc.Rnd(0, 1);
        }

        public override string ToString()
        {
            var className = GetType().Name;
            return className == "Strategy" ? "Default" : className;
        }
    }
}
