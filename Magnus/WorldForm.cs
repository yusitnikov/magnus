using Magnus.Strategies;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                Keys[Constants.LEFT_SIDE] = leftKey;
                Keys[Constants.RIGHT_SIDE] = rightKey;
            }
        }

        private List<StrategyKeys> strategies = new List<StrategyKeys>()
        {
            new StrategyKeys(new Strategy(), Keys.Q, Keys.A),
            new StrategyKeys(new TopSpinner(), Keys.W, Keys.S),
            new StrategyKeys(new Blocker(), Keys.E, Keys.D),
            new StrategyKeys(new Passive(), Keys.R, Keys.F),
            new StrategyKeys(new BackSpinner(), Keys.T, Keys.G),
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
            var g = e.Graphics;
            int w = ClientRectangle.Width, h = ClientRectangle.Height;
            g.Clear(BackColor);
            var drawer = new WorldDrawer(g, Font, w, h);
            drawer.DrawWorld(world.s);
            drawer.DrawString("FPS: " + fps, 0, 0);
            drawer.DrawString(world.s.p[Constants.LEFT_SIDE].strategy + " - " + world.s.p[Constants.RIGHT_SIDE].strategy, 0, 0.5f);
            for (var i = 0; i < strategies.Count; i++)
            {
                var strategyInfo = strategies[i];
                drawer.DrawString("[" + strategyInfo.Keys[Constants.LEFT_SIDE] + "] " + strategyInfo.Strategy + " [" + strategyInfo.Keys[Constants.RIGHT_SIDE] + "]", i + 2, 0.5f);
            }
        }

        private void WorldForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }

            if (e.KeyCode == Keys.Space)
            {
                world.Serve();
            }

            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                world.TimeCoeff = e.KeyCode - Keys.D1 + 1;
            }

            foreach (var strategyInfo in strategies)
            {
                for (var i = 0; i <= 1; i++)
                {
                    if (e.KeyCode == strategyInfo.Keys[i])
                    {
                        world.s.p[i].strategy = strategyInfo.Strategy;
                        world.s.p[i].RequestAim();
                        Invalidate();
                    }
                }
            }
        }
    }
}
