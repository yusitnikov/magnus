namespace Magnus.Strategies
{
    public class Strategy
    {
        public virtual double GetMinBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * 0.2;
        }
        public virtual double GetMaxBackHitTime(double timeTillMaxHeight, double timeTillFall)
        {
            return timeTillFall * 0.8;
        }

        public virtual double GetMinAttackAngle()
        {
            return Misc.FromDegrees(-30);
        }
        public virtual double GetMaxAttackAngle()
        {
            return Misc.FromDegrees(60);
        }

        public virtual double GetMinVelocityAttackAngle()
        {
            return Misc.FromDegrees(-90);
        }
        public virtual double GetMaxVelocityAttackAngle()
        {
            return Misc.FromDegrees(90);
        }

        public virtual double GetMinHitSpeed()
        {
            return 0;
        }
        public virtual double GetMaxHitSpeed()
        {
            return 1;
        }

        public virtual double GetMinServeHitSpeed(double ballX)
        {
            //return 0;
            return 0.2 * ballX / Constants.HalfTableLength;
        }
        public virtual double GetMaxServeHitSpeed(double ballX)
        {
            //return 0.5 * ballX / Constants.HalfTableLength;
            return 0.7 * ballX / Constants.HalfTableLength;
        }

        public virtual double GetMinServeAttackAngle()
        {
            return Misc.FromDegrees(20);
        }
        public virtual double GetMaxServeAttackAngle()
        {
            return Misc.FromDegrees(100);
        }

        public virtual double GetMinServeVelocityAttackAngle()
        {
            return Misc.FromDegrees(30);
        }
        public virtual double GetMaxServeVelocityAttackAngle()
        {
            return Misc.FromDegrees(150);
        }

        public override string ToString()
        {
            var className = GetType().Name;
            return className == "Strategy" ? "Default" : className;
        }
    }
}
