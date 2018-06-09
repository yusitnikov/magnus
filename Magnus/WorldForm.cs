using System;
using System.Drawing;
using System.Windows.Forms;

namespace Magnus
{
    public partial class WorldForm : Form
    {
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
            (new WorldDrawer(g, w, h)).DrawWorld(world.s);
            g.DrawString(fps.ToString(), Font, Brushes.Black, 0, 0);
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
        }
    }
}
