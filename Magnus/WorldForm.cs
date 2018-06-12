using Magnus.Strategies;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Magnus
{
    public partial class WorldForm : Form
    {
        private class StrategyKeys
        {
            public Strategy Strategy;
            public Keys[] Keys;

            public StrategyKeys(Strategy strategy, Keys leftKey, Keys rightKey)
            {
                Strategy = strategy;
                Keys = new Keys[2];
                Keys[Constants.LeftPlayerIndex] = leftKey;
                Keys[Constants.RightPlayerIndex] = rightKey;
            }
        }

        private List<StrategyKeys> strategies = new List<StrategyKeys>()
        {
            new StrategyKeys(new TopSpinner(), Keys.T, Keys.Y),
            new StrategyKeys(new Blocker(), Keys.R, Keys.U),
            new StrategyKeys(new Strategy(), Keys.E, Keys.I),
            new StrategyKeys(new Passive(), Keys.W, Keys.O),
            new StrategyKeys(new BackSpinner(), Keys.Q, Keys.P),
        };

        private int second = 0;
        private int fps = 0;
        private int frames = 0;

        private World world;

        public WorldForm()
        {
            InitializeComponent();

            world = new World();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void WorldForm_Paint(object sender, PaintEventArgs e)
        {
            var curSecond = DateTime.Now.Second;
            if (second != curSecond)
            {
                fps = frames;
                frames = 0;
            }
            second = curSecond;
            ++frames;

            world.DoStep();
            var graphics = e.Graphics;
            int screenWidth = ClientRectangle.Width, screenHeight = ClientRectangle.Height;
            graphics.Clear(BackColor);
            var drawer = new WorldDrawer(graphics, Font, screenWidth, screenHeight);
            drawer.DrawWorld(world.State);
            drawer.DrawString("FPS: " + fps, 0, 0);
            drawer.DrawString("Speed: " + world.TimeCoeff, 1, 0);
            Player leftPlayer = world.State.Players[Constants.LeftPlayerIndex], rightPlayer = world.State.Players[Constants.RightPlayerIndex];
            drawer.DrawString(leftPlayer.Strategy + " " + leftPlayer.Score + " - " + rightPlayer.Score + " " + rightPlayer.Strategy, 0, 0.5f);
            for (var strategyIndex = 0; strategyIndex < strategies.Count; strategyIndex++)
            {
                var strategyInfo = strategies[strategyIndex];
                drawer.DrawString("[" + strategyInfo.Keys[Constants.LeftPlayerIndex] + "] " + strategyInfo.Strategy + " [" + strategyInfo.Keys[Constants.RightPlayerIndex] + "]", strategyIndex + 2, 0.5f);
            }
        }

        private void WorldForm_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.KeyCode;

            if (key == Keys.Escape)
            {
                Close();
            }

            if (key == Keys.Space)
            {
                world.State.EndSet();
            }

            if (key >= Keys.D1 && key <= Keys.D9)
            {
                world.TimeCoeff = key - Keys.D1 + 1;
            }

            foreach (var strategyInfo in strategies)
            {
                for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
                {
                    if (key == strategyInfo.Keys[playerIndex])
                    {
                        var player = world.State.Players[playerIndex];
                        player.Strategy = strategyInfo.Strategy;
                        if (world.State.GameState.IsOneOf(GameState.Playing))
                        {
                            player.RequestAim();
                        }
                    }
                }
            }
        }
    }
}
