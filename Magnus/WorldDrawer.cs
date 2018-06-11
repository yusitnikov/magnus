using System.Drawing;

namespace Magnus
{
    class WorldDrawer
    {
        private Graphics graphics;
        private Font font;
        private int screenWidth, screenHeight;
        private double screenCoeff;

        public WorldDrawer(Graphics graphics, Font font, int screenWidth, int screenHeight)
        {
            this.graphics = graphics;
            this.font = font;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            screenCoeff = 0.4 * screenWidth / Constants.TableWidth;
        }

        private PointF getPointProjection(DoublePoint point)
        {
            return new PointF(
                (float)(screenWidth / 2 + screenCoeff * point.X),
                (float)(screenHeight - screenCoeff * (Constants.TableHeight + point.Y))
            );
        }

        private float getDistanceProjection(double distance)
        {
            return (float)(screenCoeff * distance);
        }

        private void drawLine(DoublePoint point1, DoublePoint point2, Pen pen = null)
        {
            graphics.DrawLine(pen ?? Pens.Black, getPointProjection(point1), getPointProjection(point2));
        }

        private void drawCircle(DoublePoint center, double radius, Pen pen = null)
        {
            var centerProjection = getPointProjection(center);
            var radiusProjection = getDistanceProjection(radius);
            var diameterProjection = 2 * radiusProjection + 1;
            graphics.DrawEllipse(pen ?? Pens.Black, centerProjection.X - radiusProjection, centerProjection.Y - radiusProjection, diameterProjection, diameterProjection);
        }

        public void DrawTable()
        {
            drawLine(new DoublePoint(-Constants.HalfTableWidth, 0), new DoublePoint(Constants.HalfTableWidth, 0));
            drawLine(new DoublePoint(Constants.HalfTableWidth - Constants.NetHeight, 0), new DoublePoint(Constants.HalfTableWidth - Constants.NetHeight, -Constants.TableHeight));
            drawLine(new DoublePoint(Constants.NetHeight - Constants.HalfTableWidth, 0), new DoublePoint(Constants.NetHeight - Constants.HalfTableWidth, -Constants.TableHeight));
            drawLine(new DoublePoint(0, 0), new DoublePoint(0, Constants.NetHeight));
        }

        public void DrawPlayer(Player player)
        {
            var positionDelta = Constants.BatRadius * DoublePoint.FromAngle(player.Angle).RotateRight90();
            drawLine(player.Position + positionDelta, player.Position - positionDelta, player.NeedAim ? Pens.Red : Pens.Black);
        }

        public void DrawBall(State state)
        {
            drawCircle(state.Position, Constants.BallRadius);
            var positionDelta = Constants.BallRadius * DoublePoint.FromAngle(state.Angle);
            drawLine(state.Position + positionDelta, state.Position - positionDelta);
        }

        public void DrawWorld(State state)
        {
            DrawTable();
            foreach (var player in state.Players)
            {
                DrawPlayer(player);
            }
            DrawBall(state);
        }

        public void DrawString(string text, int line, float alignment)
        {
            graphics.DrawString(text, font, Brushes.Black, (screenWidth - graphics.MeasureString(text, font).Width) * alignment, line * font.Height);
        }
    }
}
