﻿using Mathematics.Expressions;
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

        private Event doStep(bool useBat, bool updateBat = false, State relativeState = null, Variable t = null, double dt = Constants.SimulationFrameTime)
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
                Ball = Ball.DoStepSimplified(relativeState.Ball, Time - relativeState.Time);
            }
            else
            {
                Ball.DoStep(dt);
            }

            events |= CheckForHits(prevBallState, relativeState != null);

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

        public void ProcessBatHit(Player player)
        {
            var batSide = Point3D.ScalarMult(Ball.Speed, player.Normal) <= 0 ? 1 : -1;
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
                    ProcessBatHit(player);
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

        public bool IsHittingTableSimplified(bool closerToFail = false)
        {
            return IsHittingTableSimplified(Ball.Position, closerToFail);
        }

        public bool IsHittingTableSimplified(Point3D ballPosition, bool closerToFail = false)
        {
            var tableEndMargin = closerToFail ? -Constants.SimulationBordersMargin : Constants.BallRadius;
            return Math.Abs(ballPosition.X) < Constants.HalfTableLength + tableEndMargin
                && Math.Abs(ballPosition.Z) < Constants.HalfTableWidth + tableEndMargin;
        }

        public void ProcessTableHit()
        {
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

        public Event CheckForHits(Ball prevBallState, bool closerToFail = false)
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

            var verticalSpeedSign = Math.Sign(Ball.Speed.Y);
            if (
                IsHittingTableSimplified(closerToFail) &&
                verticalSpeedSign != 0 &&
                Ball.Position.Y * verticalSpeedSign > -Constants.BallRadius &&
                prevBallState.Position.Y * verticalSpeedSign <= -Constants.BallRadius
            )
            {
                if (Ball.Speed.Y < 0)
                {
                    events |= Event.TableHit;

                    ProcessTableHit();
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

        public bool CheckForTableHitSimplified(bool closerToFail = false)
        {
            bool hitting = IsHittingTableSimplified(closerToFail);
            if (hitting)
            {
                ProcessTableHit();
            }
            return hitting;
        }

        public Event DoStep()
        {
            return doStep(false);
        }

        public Event DoSimplifiedStep(State relativeState, Variable t, double dt = Constants.SimulationFrameTime)
        {
            return doStep(false, false, relativeState, t, dt);
        }

        public Event DoStepWithBatUpdate()
        {
            return doStep(true, true);
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
