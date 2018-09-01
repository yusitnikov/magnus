using Mathematics.Expressions;
using Mathematics.Math3D;
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

        public double NextServeX;

        public State()
        {
            Ball = new Ball()
            {
                MarkPoint = Point3D.XAxis
            };
            UpdateNextServeX();
            Players = new Player[2];
            for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
            {
                Players[playerIndex] = new Player(playerIndex);
                Players[playerIndex].ResetPosition(NextServeX + Constants.NetHeight, true);
            }
            Time = 0;
            HitSide = Misc.Rnd(0, 1) < 0.5 ? Constants.LeftPlayerIndex : Constants.RightPlayerIndex;

            Reset();
        }

        private State(State other, bool copyPlayers = true)
        {
            CopyFrom(other, copyPlayers);
        }

        public void UpdateNextServeX()
        {
            NextServeX = Constants.HalfTableLength + Misc.Rnd(Constants.MinBallServeX, Constants.MaxBallServeX);
        }

        public void Reset()
        {
            GameState = GameState.Serving;
            Ball.Position = new Point3D(NextServeX * Misc.GetPlayerSideByIndex(HitSide), Constants.NetHeight, 0);
            Ball.Speed = new Point3D(0, Misc.Rnd(Constants.MinBallServeThrowSpeed, Constants.MaxBallServeThrowSpeed), 0);
            Ball.AngularSpeed = Point3D.Empty;

            UpdateNextServeX();
            Players[HitSide].RequestAim();
        }

        private Event doStep(State relativeState, BallExpression simplifiedTrajectory, Variable t, double dt, bool useBat, bool updateBat = false)
        {
            var events = Event.None;

            var prevBallState = Ball.Clone();

            Time += dt;

            if (useBat)
            {
                events |= doPlayerStep(dt, updateBat);
            }

            if (relativeState != null)
            {
                if (Simulation.UseBinarySearch)
                {
                    t.Value = Time - relativeState.Time;
                    Ball.AngularSpeed = simplifiedTrajectory.AngularSpeed.Evaluate();
                    Ball.Position = simplifiedTrajectory.Position.Evaluate();
                    Ball.Speed = simplifiedTrajectory.Speed.Evaluate();
                }
                else
                {
                    Ball.DoStepSimplified(relativeState.Ball, Time - relativeState.Time);
                }
            }
            else
            {
                Ball.DoStep(dt, false);
            }

            events |= checkForHits(prevBallState, relativeState != null);

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

                var ballInBatSystem = Ball.ProjectToSurface(player);
                if (ballInBatSystem.Position.Horizontal.Length < Constants.BatRadius + Constants.BallRadius && ballInBatSystem.Position.Vertical.Length <= Constants.BallRadius)
                {
                    events |= Event.BatHit;
                    events |= player.Index == Constants.RightPlayerIndex ? Event.RightBatHit : Event.LeftBatHit;

                    var batSide = Point3D.ScalarMult(ballInBatSystem.Speed.Vertical, player.Normal) <= 0 ? 1 : -1;
                    Ball.ProcessHit(player, Constants.BallHitHorizontalCoeff, Constants.BallHitVerticalCoeff, batSide);

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

        private Event checkForHits(Ball prevBallState, bool closerToFail = false)
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

            var tableEndMargin = closerToFail ? Constants.SimulationBordersMargin * Math.Sign(Ball.Speed.Y) : Constants.BallRadius;
            var tableEndX = Constants.HalfTableLength + tableEndMargin;
            var tableEndZ = Constants.HalfTableWidth + tableEndMargin;
            var verticalSpeedSign = Math.Sign(Ball.Speed.Y);
            if (
                verticalSpeedSign != 0 &&
                Ball.Position.Y * verticalSpeedSign > -Constants.BallRadius &&
                prevBallState.Position.Y * verticalSpeedSign <= -Constants.BallRadius &&
                Math.Abs(Ball.Position.X) < tableEndX &&
                Math.Abs(Ball.Position.Z) < tableEndZ
            )
            {
                if (Ball.Speed.Y < 0)
                {
                    events |= Event.TableHit;

                    Ball.ProcessHit(Surface.Horizontal, Constants.TableHitHorizontalCoeff, Constants.TableHitVerticalCoeff);

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

                    Ball.ProcessHit(Surface.HorizontalReverted, Constants.TableHitHorizontalCoeff, Constants.TableHitVerticalCoeff);

                    endSet(GameState.IsOneOf(GameState.NotReadyToHit));
                }
            }

            return events;
        }

        public Event DoStep(bool useBat = false, State relativeState = null, BallExpression simplifiedTrajectory = null, Variable t = null, double dt = Constants.SimulationFrameTime)
        {
            return doStep(relativeState, simplifiedTrajectory, t, dt, useBat);
        }

        public Event DoStepWithBatUpdate()
        {
            return doStep(null, null, null, Constants.SimulationFrameTime, true, true);
        }

        public bool DoStepsUntilGameState(GameState gameStates)
        {
            while (!GameState.IsOneOf(gameStates | GameState.Failed))
            {
                DoStep();
            }
            return GameState != GameState.Failed;
        }

        public bool DoStepsUntilEvent(Event events)
        {
            while (GameState != GameState.Failed)
            {
                if (DoStep().HasOneOfEvents(events))
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
