using System.Threading;

namespace Magnus
{
    class HitSearcherThread
    {
        private State state;
        private Player player;
        private HitSearcher searcher;

        private bool needAim;
        private bool stateChanged;
        private bool reset;

        private Thread thread;

        private AutoResetEvent needAimEvent;

        private Aim result;

        public HitSearcherThread()
        {
            searcher = new HitSearcher();
            needAimEvent = new AutoResetEvent(false);
            reset = true;

            thread = new Thread(run)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            thread.Start();
        }

        public void StartSearching(State state, Player player, bool reset)
        {
            lock (this)
            {
                this.state = state.Clone(false);
                this.player = player.Clone();

                stateChanged = true;
                if (reset)
                {
                    this.reset = true;
                }
                result = null;
                needAim = true;
                needAimEvent.Set();
            }
        }

        public void StopSearching()
        {
            lock (this)
            {
                stateChanged = true;
                result = null;
                needAim = false;
            }
        }

        public Aim GetResult()
        {
            lock (this)
            {
                return result;
            }
        }

        private void run()
        {
            while (true)
            {
                needAimEvent.WaitOne();

                while (true)
                {
                    lock (this)
                    {
                        if (!needAim)
                        {
                            break;
                        }

                        if (stateChanged)
                        {
                            if (!searcher.Initialize(state, player))
                            {
                                result = player.GetInitialPositionAim(state, true);
                                needAim = false;
                                break;
                            }

                            if (reset)
                            {
                                searcher.Reset();
                                reset = false;
                            }
                        }

                        stateChanged = false;
                    }

                    var aim = searcher.Search();

                    if (aim != null)
                    {
                        lock (this)
                        {
                            if (!reset)
                            {
                                result = aim;
                                if (!stateChanged)
                                {
                                    needAim = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
