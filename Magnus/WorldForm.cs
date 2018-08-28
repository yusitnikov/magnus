using Magnus.Strategies;
using OpenTK;
using OpenTK.Graphics;
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
            new StrategyKeys(new SuperBlocker(), Keys.F, Keys.J),
            new StrategyKeys(new Strategy(), Keys.E, Keys.I),
            new StrategyKeys(new Passive(), Keys.W, Keys.O),
            new StrategyKeys(new BackSpinner(), Keys.Q, Keys.P),
        };

        private GLControl glCanvas;
        private World world;
        private WorldDrawer drawer;

        public WorldForm()
        {
            InitializeComponent();

            SuspendLayout();
            var mode = GraphicsMode.Default;
            glCanvas = new GLControl(new GraphicsMode(mode.ColorFormat, mode.Depth, 8, mode.Samples, mode.AccumulatorFormat, mode.Buffers, mode.Stereo))
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(0, 0),
                Name = "glCanvas",
                Size = new System.Drawing.Size(763, 424),
                TabIndex = 0,
                VSync = false
            };
            glCanvas.Load += glCanvas_Load;
            glCanvas.Paint += glCanvas_Paint;
            glCanvas.KeyDown += WorldForm_KeyDown;
            Controls.Add(glCanvas);
            ResumeLayout(false);

            world = new World();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            glCanvas.Invalidate();
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

        private bool canDraw = false;

        private void glCanvas_Load(object sender, EventArgs e)
        {
            drawer = new WorldDrawer(Font);

            canDraw = true;
        }

        private void glCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (!canDraw)
            {
                return;
            }

            var stats = Profiler.Instance.AverageStats;

            Profiler.Instance.LogFrameStart();

            drawer.Start(glCanvas.Width, glCanvas.Height);

            Profiler.Instance.LogEvent("drawer.Start");

            world.DoStep();
            Profiler.Instance.LogEvent("world.DoStep");
            drawer.DrawWorld(world.State);
            drawer.DrawString("FPS: " + Profiler.Instance.FPS, 0, 0);
            drawer.DrawString("Speed: " + world.TimeCoeff + " / " + World.DefaultTimeCoeff, 1, 0);
            Player leftPlayer = world.State.Players[Constants.LeftPlayerIndex], rightPlayer = world.State.Players[Constants.RightPlayerIndex];
            drawer.DrawString(leftPlayer.Strategy + " " + leftPlayer.Score + " - " + rightPlayer.Score + " " + rightPlayer.Strategy, 0, 0.5f);
            var text = "";
            foreach (var pair in stats)
            {
                text += pair.Key + ": " + pair.Value.ToString("0.02") + "\r\n";
            }
            //drawer.DrawString(text, 3, 0);
            Profiler.Instance.LogEvent("drawer.DrawString");

            drawer.End();

            glCanvas.SwapBuffers();
            Profiler.Instance.LogEvent("glCanvas.SwapBuffers");
        }
    }
}
