using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Magnus
{
    class WorldDrawer
    {
        private Graphics graphics;
        private Font font;
        private int screenWidth, screenHeight;
        private double screenCoeff;

        private List<DoublePoint3D> tableKeyPoints = new List<DoublePoint3D>();

        public WorldDrawer(Graphics graphics, Font font, int screenWidth, int screenHeight)
        {
            this.graphics = graphics;
            this.font = font;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            screenCoeff = Constants.ScreenZoom * screenWidth / Constants.TableLength;

            initTableKeyPoints();
        }

        private void initTableKeyPoints()
        {
            addTableKeyCube(-Constants.HalfTableLength, 0, -Constants.HalfTableWidth, Constants.HalfTableLength, -1, Constants.HalfTableWidth);
            for (int xSign = -1; xSign <= 1; xSign += 2)
            {
                for (int zSign = -1; zSign <= 1; zSign += 2)
                {
                    var x = zSign * (Constants.HalfTableLength - 10);
                    var z = xSign * (Constants.HalfTableWidth - 10);
                    addTableKeyCube(x - 3, 0, z - 3, x + 3, -Constants.TableHeight, z + 3);
                }
            }
            tableKeyPoints.AddRange(new DoublePoint3D[]
            {
                new DoublePoint3D(0, Constants.NetHeight, -Constants.HalfNetWidth),
                new DoublePoint3D(0, Constants.NetHeight, Constants.HalfNetWidth),
                new DoublePoint3D(0, 0, Constants.HalfNetWidth),
                new DoublePoint3D(0, 0, -Constants.HalfNetWidth)
            });
        }

        private void addTableKeyCube(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            tableKeyPoints.AddRange(new DoublePoint3D[]
            {
                new DoublePoint3D(x1, y1, z1),
                new DoublePoint3D(x1, y1, z2),
                new DoublePoint3D(x2, y1, z2),
                new DoublePoint3D(x2, y1, z1),
                new DoublePoint3D(x1, y2, z1),
                new DoublePoint3D(x1, y2, z2),
                new DoublePoint3D(x2, y2, z2),
                new DoublePoint3D(x2, y2, z1)
            });
        }

        private PointF getPointProjection(DoublePoint3D point)
        {
            return new PointF(
                (float)(screenWidth / 2 + screenCoeff * (point.X + point.Z * 0.3)),
                (float)(screenHeight * 0.5 - screenCoeff * (point.Y + point.Z * 0.3))
            );
        }

        private float getDistanceProjection(double distance)
        {
            return (float)(screenCoeff * distance);
        }

        private void drawLine(Pen pen, params DoublePoint3D[] points)
        {
            graphics.DrawLines(pen, points.Select(getPointProjection).ToArray());
        }

        private void drawPolygon(Pen borderColor, params DoublePoint3D[] points)
        {
            graphics.DrawPolygon(borderColor, points.Select(getPointProjection).ToArray());
        }

        private bool isSurfaceVisibleClockwise(params PointF[] projections)
        {
            PointF p1 = projections[0], p2 = projections[1], p3 = projections[2];
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X) >= 0;
        }

        private bool isSurfaceVisibleClockwise(params DoublePoint3D[] points)
        {
            return isSurfaceVisibleClockwise(points.Select(getPointProjection).ToArray());
        }

        private void fillPolygon(Brush fillColor, Pen borderColor, params DoublePoint3D[] points)
        {
            fillPolygon(fillColor, borderColor, true, points);
        }

        private void fillPolygon(Brush fillColor, Pen borderColor, bool onlyClockwise, params DoublePoint3D[] points)
        {
            var projections = points.Select(getPointProjection).ToArray();
            if (onlyClockwise && !isSurfaceVisibleClockwise(projections))
            {
                return;
            }
            graphics.FillPolygon(fillColor, projections);
            graphics.DrawPolygon(borderColor, projections);
        }

        private void drawKeyPointsCube(Brush fillColor, Pen borderColor, int i)
        {
            fillPolygon(fillColor, borderColor, tableKeyPoints[i + 0], tableKeyPoints[i + 1], tableKeyPoints[i + 2], tableKeyPoints[i + 3]);
            fillPolygon(fillColor, borderColor, tableKeyPoints[i + 0], tableKeyPoints[i + 4], tableKeyPoints[i + 5], tableKeyPoints[i + 1]);
            fillPolygon(fillColor, borderColor, tableKeyPoints[i + 1], tableKeyPoints[i + 5], tableKeyPoints[i + 6], tableKeyPoints[i + 2]);
            fillPolygon(fillColor, borderColor, tableKeyPoints[i + 2], tableKeyPoints[i + 6], tableKeyPoints[i + 7], tableKeyPoints[i + 3]);
            fillPolygon(fillColor, borderColor, tableKeyPoints[i + 3], tableKeyPoints[i + 7], tableKeyPoints[i + 4], tableKeyPoints[i + 0]);
        }

        private void drawCircle(Brush fillColor, Pen borderColor, DoublePoint3D center, double radius)
        {
            var centerProjection = getPointProjection(center);
            var radiusProjection = getDistanceProjection(radius);
            var diameterProjection = 2 * radiusProjection + 1;
            graphics.FillEllipse(fillColor, centerProjection.X - radiusProjection, centerProjection.Y - radiusProjection, diameterProjection, diameterProjection);
            graphics.DrawEllipse(borderColor, centerProjection.X - radiusProjection, centerProjection.Y - radiusProjection, diameterProjection, diameterProjection);
        }

        private DoublePoint3D[] getCirclePoints(double radius)
        {
            const int pointsCount = 30;
            var points = new DoublePoint3D[pointsCount + 1];
            for (var i = 0; i <= pointsCount; i++)
            {
                points[i] = radius * DoublePoint3D.YAxis.RotateRoll(2 * Math.PI * i / pointsCount);
            }
            return points;
        }

        private DoublePoint3D[] getCirclePointsByNormal(DoublePoint3D center, DoublePoint3D normal, double radius)
        {
            var pitch = normal.Pitch + Math.PI / 2;
            var yaw = normal.Yaw;
            return getCirclePoints(radius).Select(point => center + point.RotatePitch(pitch).RotateYaw(yaw)).ToArray();
        }

        private void drawCircleByNormal(Brush fillColor, Pen borderColor, DoublePoint3D center, DoublePoint3D normal, double radius, bool onlyClockwise = false)
        {
            fillPolygon(fillColor, borderColor, onlyClockwise, getCirclePointsByNormal(center, normal, radius));
        }

        public void DrawTable()
        {
            var fillColor = Brushes.Green;
            var borderColor = Pens.LightGreen;
            var lineColor = new Pen(Color.White, 3);

            var legs = new List<int>() { 8, 16, 24, 32 };
            legs.Sort((i1, i2) => tableKeyPoints[i2].Z.CompareTo(tableKeyPoints[i1].Z));
            foreach (var i in legs)
            {
                drawKeyPointsCube(fillColor, borderColor, i);
            }

            drawKeyPointsCube(fillColor, borderColor, 0);
            drawLine(lineColor, new DoublePoint3D(0, 0, -Constants.HalfTableWidth), new DoublePoint3D(0, 0, Constants.HalfTableWidth));
            drawLine(lineColor, new DoublePoint3D(-Constants.HalfTableLength, 0, 0), new DoublePoint3D(Constants.HalfTableLength, 0, 0));

            fillPolygon(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), Pens.Black, false, tableKeyPoints[40], tableKeyPoints[41], tableKeyPoints[42], tableKeyPoints[43]);
            drawLine(lineColor, tableKeyPoints[40], tableKeyPoints[41]);
        }

        private double getPointShadowY(DoublePoint3D point)
        {
            return Math.Abs(point.X) < Constants.HalfTableLength && Math.Abs(point.Z) < Constants.HalfTableWidth ? 0 : -Constants.TableHeight;
        }

        private DoublePoint3D getPointShadowCoords(DoublePoint3D point, double shadowY)
        {
            return new DoublePoint3D(point.X, shadowY, point.Z);
        }

        private DoublePoint3D[] getBatPoints(Player player)
        {
            return getCirclePoints(1).Select(point => player.TranslatePointFromBatCoords(new DoublePoint3D(point.Y * Constants.BatRadius, 0, point.Z * Constants.BatBiggerRadius))).ToArray();
        }

        private DoublePoint3D[] getBatLinePoints(Player player)
        {
            return new DoublePoint3D[]
            {
                player.TranslatePointFromBatCoords(new DoublePoint3D(0, 0, Constants.BatBiggerRadius)),
                player.TranslatePointFromBatCoords(new DoublePoint3D(0, 0, Constants.BatBiggerRadius * 1.5))
            };
        }

        public void DrawPlayer(Player player)
        {
            var woodColor = player.NeedAim ? Color.DarkRed : Color.BurlyWood;
            var borderColor = new Pen(woodColor, 3);
            var batPoints = getBatPoints(player);
            var batLinePoints = getBatLinePoints(player);

            fillPolygon(Brushes.Black, borderColor, batPoints);
            fillPolygon(Brushes.Red, borderColor, batPoints.Reverse().ToArray());
            drawLine(new Pen(woodColor, getDistanceProjection(Constants.BallRadius)), batLinePoints);
        }

        public void DrawPlayerShadow(Player player)
        {
            var shadowColor = Color.FromArgb(128, 0, 0, 0);
            var batPoints = getBatPoints(player);
            var batLinePoints = getBatLinePoints(player);

            var shadowY = getPointShadowY(player.Position);
            fillPolygon(new SolidBrush(shadowColor), new Pen(shadowColor, 3), false, batPoints.Select(point => getPointShadowCoords(point, shadowY)).ToArray());
            drawLine(new Pen(shadowColor, getDistanceProjection(Constants.BallRadius)), batLinePoints.Select(point => getPointShadowCoords(point, shadowY)).ToArray());
        }

        public void DrawBall(Ball ball)
        {
            drawCircle(Brushes.White, Pens.Black, ball.Position, Constants.BallRadius);
            drawCircleByNormal(Brushes.Red, Pens.Transparent, ball.Position + ball.MarkPoint, ball.MarkPoint.Normal, Constants.BallRadius / 2, true);
            drawCircleByNormal(Brushes.Red, Pens.Transparent, ball.Position - ball.MarkPoint, -ball.MarkPoint.Normal, Constants.BallRadius / 2, true);
            drawCircle(Brushes.Transparent, Pens.Black, ball.Position, Constants.BallRadius);
            var shadowY = getPointShadowY(ball.Position);
            drawCircleByNormal(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), Pens.Transparent, getPointShadowCoords(ball.Position, shadowY), DoublePoint3D.YAxis, Constants.BallRadius);
        }

        public void DrawWorld(State state)
        {
            var ballIsOutOfTable = state.Ball.Position.Z > Constants.HalfNetWidth && state.Ball.Position.Y < 0;
            if (ballIsOutOfTable)
            {
                DrawBall(state.Ball);
            }
            DrawTable();
            foreach (var player in state.Players)
            {
                DrawPlayerShadow(player);
                DrawPlayer(player);
            }
            if (!ballIsOutOfTable)
            {
                DrawBall(state.Ball);
            }
            foreach (var player in state.Players)
            {
                var seeBatFrontSide = isSurfaceVisibleClockwise(getBatPoints(player));
                var ballIsFromFrontSide = DoublePoint3D.ScalarMult(state.Ball.ProjectToSurface(player).Position.Vertical, player.Normal) >= 0;
                if (ballIsFromFrontSide != seeBatFrontSide)
                {
                    DrawPlayer(player);
                }
            }
        }

        public void DrawString(string text, int line, float alignment)
        {
            graphics.DrawString(text, font, Brushes.Black, (screenWidth - graphics.MeasureString(text, font).Width) * alignment, line * font.Height);
        }
    }
}
