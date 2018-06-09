using System.Drawing;

namespace Magnus
{
    class WorldDrawer
    {
        private Graphics g;
        private int sw, sh;
        private double sk;

        public WorldDrawer(Graphics g, int sw, int sh)
        {
            this.g = g;
            this.sw = sw;
            this.sh = sh;
            sk = 0.2 * sw / Constants.tw;
        }

        private PointF tsp(DoublePoint p)
        {
            return new PointF(
                (float)(sw / 2 + sk * p.X),
                (float)(sh - sk * (Constants.th + p.Y))
            );
        }

        private float tsk(double k)
        {
            return (float)(sk * k);
        }

        private void drawLine(DoublePoint p1, DoublePoint p2, Pen pen = null)
        {
            PointF p1f = tsp(p1), p2f = tsp(p2);
            g.DrawLine(pen ?? Pens.Black, p1f, p2f);
        }

        private void drawCircle(DoublePoint p, double r, Pen pen = null)
        {
            var pf = tsp(p);
            var rf = tsk(r);
            g.DrawEllipse(pen ?? Pens.Black, pf.X - rf, pf.Y - rf, 2 * rf + 1, 2 * rf + 1);
        }

        public void DrawTable()
        {
            drawLine(new DoublePoint(-Constants.tw, 0), new DoublePoint(Constants.tw, 0));
            drawLine(new DoublePoint(Constants.tw - Constants.nh, 0), new DoublePoint(Constants.tw - Constants.nh, -Constants.th));
            drawLine(new DoublePoint(Constants.nh - Constants.tw, 0), new DoublePoint(Constants.nh - Constants.tw, -Constants.th));
            drawLine(new DoublePoint(0, 0), new DoublePoint(0, Constants.nh));
        }

        public void DrawPlayer(Player p)
        {
            var dp = Constants.nh / 2 * DoublePoint.FromAngle(p.a).RotateRight90();
            double dx = -Constants.nh / 2 * Misc.Cos(p.a), dy = Constants.nh / 2 * Misc.Sin(p.a);
            drawLine(p.pos + dp, p.pos - dp, p.needAim ? Pens.Red : Pens.Black);
        }

        public void DrawBall(State s)
        {
            drawCircle(s.pos, Constants.br);
            var dp = Constants.br * DoublePoint.FromAngle(s.a);
            drawLine(s.pos + dp, s.pos - dp);
        }

        public void DrawWorld(State s)
        {
            DrawTable();
            for (var i = 0; i <= 1; i++)
            {
                DrawPlayer(s.p[i]);
            }
            DrawBall(s);
        }
    }
}
