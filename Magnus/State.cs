using System;

namespace Magnus
{
    class State
    {
        public Ball Ball;

        public double Time;

        public Player[] Players;

        public int HitSide;
        public GameState GameState;

        public State()
        {
            Ball = new Ball();
            Players = new Player[2];
            for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
            {
                Players[playerIndex] = new Player(playerIndex);
            }
            Time = 0;
            HitSide = Misc.Rnd(0, 1) < 0.5 ? Constants.LeftPlayerIndex : Constants.RightPlayerIndex;
            Reset(true, true);
        }

        private State(State other, bool copyPlayers = true)
        {
            CopyFrom(other, copyPlayers);
        }

        public void Reset(bool resetPosition, bool resetAngle)
        {
            GameState = GameState.Serving;
            Ball.Position = new DoublePoint((Constants.HalfTableWidth + Misc.Rnd(Constants.MinBallServeX, Constants.MaxBallServeX)) * Misc.GetPlayerSideByIndex(HitSide), Constants.NetHeight);
            Ball.Speed = new DoublePoint(0, Misc.Rnd(Constants.MinBallServeThrowSpeed, Constants.MaxBallServeThrowSpeed));
            Ball.AngularSpeed = 0;

            foreach (var player in Players)
            {
                player.Reset(Math.Abs(Ball.Position.X) + Constants.NetHeight, resetPosition, resetAngle);
            }

            Players[HitSide].RequestAim();
        }

        private Event doStep(State relativeState, double dt, bool useBat, bool updateBat = false)
        {
            var events = Event.None;

            var prevBallState = Ball.Clone();

            Time += dt;

            if (useBat)
            {
                events |= doPlayerStep(dt, updateBat);
            }

            var simplify = relativeState != null;
            Ball.DoStep(dt, simplify);
            if (simplify)
            {
                Ball.DoStepSimplified(relativeState.Ball, Time - relativeState.Time);
            }

            events |= checkForHits();

            if (Ball.Side != prevBallState.Side)
            {
                events |= Event.NetCross;
            }

            if (Ball.Position.Y <= Constants.MinHitY && prevBallState.Position.Y > Constants.MinHitY)
            {
                events |= Event.LowCross;
            }

            if (Ball.Speed.Y <= 0 && prevBallState.Speed.Y > 0 && events == 0)
            {
                events = Event.MaxHeight;
            }

            if (relativeState != null && events.HasOneOfEvents(Event.AnyHit))
            {
                relativeState.CopyFrom(this, false);
            }

            return events;
        }

        private Event doPlayerStep(double dt, bool updateBat = false)
        {
            var events = Event.None;

            foreach (var player in Players)
            {
                if (updateBat)
                {
                    player.DoStep(this, dt);
                }

                var ballInBatSystem = Ball.ProjectToBat(player);
                if (Math.Abs(ballInBatSystem.Position.X) < Constants.BatRadius + Constants.BallRadius && Math.Abs(ballInBatSystem.Position.Y) <= Constants.BallRadius && ballInBatSystem.Speed.Y <= 0)
                {
                    events |= Event.BatHit;
                    events |= player.Index == Constants.RightPlayerIndex ? Event.RightBatHit : Event.LeftBatHit;

                    ballInBatSystem.ProcessHit(Constants.BallHitHorizontalCoeff, Constants.BallHitVerticalCoeff);
                    Ball = ballInBatSystem.ProjectFromBat(player);

                    if (GameState != GameState.Failed)
                    {
                        if (player.Index != HitSide || GameState.IsOneOf(GameState.NotReadyToHit))
                        {
                            endSet(false);
                        }
                        else
                        {
                            HitSide = Misc.GetOtherPlayerIndex(player.Index);
                            switch (GameState)
                            {
                                case GameState.Serving:
                                    GameState = GameState.Served;
                                    break;
                                case GameState.FlyingToBat:
                                    GameState = GameState.FlyingToTable;
                                    break;
                            }
                        }
                    }
                }
            }

            return events;
        }

        public void EndSet()
        {
            GameState = GameState.Failed;
        }

        private void endSet(bool hitSideIsWinner)
        {
            if (GameState != GameState.Failed)
            {
                if (!hitSideIsWinner)
                {
                    HitSide = Misc.GetOtherPlayerIndex(HitSide);
                }
                if (Players != null)
                {
                    ++Players[HitSide].Score;
                }
                EndSet();
            }
        }

        private Event checkForHits()
        {
            Event events = 0;

            var floorHitY = Constants.BallRadius - Constants.TableHeight;
            if (Ball.Position.Y < floorHitY)
            {
                events |= Event.FloorHit;

                Ball.Position.Y = 2 * floorHitY - Ball.Position.Y;
                Ball.Speed.Y = Constants.TableHitVerticalCoeff * Math.Abs(Ball.Speed.Y);

                endSet(GameState.IsOneOf(GameState.NotReadyToHit));
            }

            var tableEndX = Constants.HalfTableWidth + Constants.BallRadius;
            if (Ball.Position.Y < Constants.BallRadius && Math.Abs(Ball.Position.X) < tableEndX)
            {
                if (Ball.Speed.Y < 0 && Ball.Position.Y - Constants.BallRadius > Math.Abs(Ball.Position.X) - tableEndX)
                {
                    events |= Event.TableHit;

                    Ball.ProcessHit(Constants.TableHitHorizontalCoeff, Constants.TableHitVerticalCoeff);

                    var isHitSide = Ball.Side == Misc.GetPlayerSideByIndex(HitSide);
                    switch (GameState)
                    {
                        case GameState.Serving:
                        case GameState.FlyingToBat:
                            endSet(false);
                            break;
                        case GameState.Served:
                            if (isHitSide)
                            {
                                endSet(true);
                            }
                            else
                            {
                                GameState = GameState.FlyingToTable;
                            }
                            break;
                        case GameState.FlyingToTable:
                            if (isHitSide)
                            {
                                GameState = GameState.FlyingToBat;
                            }
                            else
                            {
                                endSet(true);
                            }
                            break;
                    }
                }
                else
                {
                    events |= Event.FloorHit;

                    Ball.Position.X = 2 * tableEndX * Ball.Side - Ball.Position.X;
                    Ball.Speed.X = Math.Abs(Ball.Speed.X) * Ball.Side;

                    endSet(GameState.IsOneOf(GameState.NotReadyToHit));
                }
            }

            return events;
        }

        public Event DoStep(State relativeState = null, double dt = Constants.SimulationFrameTime, bool useBat = false)
        {
            return doStep(relativeState, dt, useBat);
        }

        public Event DoStepWithBatUpdate(State relativeState, double dt)
        {
            return doStep(relativeState, dt, true, true);
        }

        public bool DoStepsUntilGameState(GameState gameStates, State relativeState = null, double dt = Constants.SimulationFrameTime, bool useBat = false)
        {
            while (!GameState.IsOneOf(gameStates | GameState.Failed))
            {
                DoStep(relativeState, dt, useBat);
            }
            return GameState != GameState.Failed;
        }

        public bool DoStepsUntilEvent(Event events, State relativeState = null, double dt = Constants.SimulationFrameTime, bool useBat = false)
        {
            while (GameState != GameState.Failed)
            {
                if (DoStep(relativeState, dt, useBat).HasOneOfEvents(events))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyFrom(State state, bool copyPlayers = true)
        {
            HitSide = state.HitSide;
            GameState = state.GameState;
            Time = state.Time;
            Ball = state.Ball.Clone();
            if (copyPlayers)
            {
                Players = new Player[] { state.Players[0].Clone(), state.Players[1].Clone() };
            }
        }

        public State Clone(bool copyPlayers = true)
        {
            return new State(this, copyPlayers);
        }
    }
}
