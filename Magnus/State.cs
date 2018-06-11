using System;

namespace Magnus
{
    class State
    {
        public DoublePoint Position, Speed;
        public double Angle, AngularSpeed;

        public int BallSide => Math.Sign(Position.X);

        public double Time;

        public Player[] Players;

        public int HitSide;
        public GameState GameState;

        public State()
        {
            Players = new Player[2];
            Time = 0;
            for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
            {
                Players[playerIndex] = new Player(playerIndex);
            }
            HitSide = Misc.Rnd(0, 1) < 0.5 ? Constants.LeftPlayerIndex : Constants.RightPlayerIndex;
            Reset(true, true);
        }

        private State(State other)
        {
            CopyFrom(other);
        }

        public void Reset(bool resetPosition, bool resetAngle)
        {
            GameState = GameState.Serving;
            Position = new DoublePoint((Constants.HalfTableWidth + Misc.Rnd(100, 400)) * Misc.GetPlayerSideByIndex(HitSide), Constants.NetHeight);
            Speed = new DoublePoint(0, Misc.Rnd(150, 400));
            AngularSpeed = 0;

            foreach (var player in Players)
            {
                player.Reset(Math.Abs(Position.X) + Constants.NetHeight, resetPosition, resetAngle);
            }

            Players[HitSide].RequestAim();
        }

        private Event doStep(State relativeState, double dt, bool useBat, bool updateBat = false)
        {
            var events = Event.None;

            var prevBallState = new State()
            {
                Position = Position,
                Speed = Speed
            };

            if (useBat)
            {
                events |= doPlayerStep(dt, updateBat);
            }

            doBallAirStep(relativeState, dt);

            events |= checkForHits();

            if (BallSide != prevBallState.BallSide)
            {
                events |= Event.NetCross;
            }

            var lowCrossY = -Constants.NetHeight * 2;
            if (Position.Y <= lowCrossY && prevBallState.Position.Y > lowCrossY)
            {
                events |= Event.LowCross;
            }

            if (Speed.Y <= 0 && prevBallState.Speed.Y > 0 && events == 0)
            {
                events = Event.MaxHeight;
            }

            if (relativeState != null && events.HasOneOfEvents(Event.AnyHit))
            {
                relativeState.CopyFrom(this);
            }

            return events;
        }

        private void doBallAirStep(State relativeState, double dt)
        {
            var simplifiedSpeed = relativeState == null ? Speed : new DoublePoint(Speed.X, 0);
            DoublePoint force = Constants.BallLiftCoeff * AngularSpeed * simplifiedSpeed.RotateRight90() - Constants.BallDumpCoeff * simplifiedSpeed.Length * Speed - new DoublePoint(0, Constants.GravityForce);
            double angularForce = Constants.BallAngularDumpCoeff * AngularSpeed * Math.Sqrt(Math.Abs(AngularSpeed));

            Position += Speed * dt + force * (dt * dt / 2);
            Angle += AngularSpeed * dt;
            Time += dt;
            Speed += force * dt;
            AngularSpeed -= angularForce * dt;
            if (relativeState != null)
            {
                double t = Time - relativeState.Time;
                double angularSpeedCharacteristic = calcAngularSpeedCharacteristic(relativeState.AngularSpeed, t);
                AngularSpeed = Math.Sign(relativeState.AngularSpeed) / (angularSpeedCharacteristic * angularSpeedCharacteristic);
                Speed.X = 1 / (Constants.BallDumpCoeff * Math.Sign(relativeState.Speed.X) * t + 1 / relativeState.Speed.X);
                Speed.Y = (relativeState.Speed.Y / relativeState.Speed.X - Constants.BallLiftCoeff * calcAngularSpeedCharacteristicDefiniteIntegral(relativeState.AngularSpeed, t) - Constants.GravityForce * (Constants.BallDumpCoeff * Math.Sign(relativeState.Speed.X) * t * t / 2 + t / relativeState.Speed.X)) * Speed.X;
            }
        }

        private Event doPlayerStep(double dt, bool updateBat = false)
        {
            var events = Event.None;

            for (var playerIndex = 0; playerIndex <= 1; playerIndex++)
            {
                var player = Players[playerIndex];

                if (updateBat)
                {
                    player.DoStep(this, dt);
                }

                // project to bat CS
                var batNormal = DoublePoint.FromAngle(player.Angle);
                var ballPositionInBatSystem = (Position - player.Position).ProjectToNormalVector(batNormal);
                var ballSpeedInBatSystem = (Speed - player.Speed).ProjectToNormalVector(batNormal);
                if (Math.Abs(ballPositionInBatSystem.X) < Constants.BatRadius + Constants.BallRadius && Math.Abs(ballPositionInBatSystem.Y) <= Constants.BallRadius && ballSpeedInBatSystem.Y <= 0)
                {
                    events |= Event.BatHit;
                    events |= playerIndex == Constants.RightPlayerIndex ? Event.RightBatHit : Event.LeftBatHit;

                    processBallHit(ref ballSpeedInBatSystem, Constants.BallHitHorizontalCoeff, Constants.BallHitVerticalCoeff);
                    Speed = player.Speed + ballSpeedInBatSystem.ProjectFromNormalVector(batNormal);

                    if (GameState != GameState.Failed)
                    {
                        if (playerIndex != HitSide || GameState.IsOneOf(GameState.NotReadyToHit))
                        {
                            endSet(false);
                        }
                        else
                        {
                            HitSide = Misc.GetOtherPlayerIndex(playerIndex);
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
                ++Players[HitSide].Score;
                EndSet();
            }
        }

        private void processBallHit(ref DoublePoint relativeSpeed, double horizontalHitCoeff, double verticalHitCoeff)
        {
            const double angularSpeedToPlainCoeff = Constants.BallRadius * Math.PI / 180;
            relativeSpeed.Y *= -verticalHitCoeff;
            double rollSpeedAtPoint = -AngularSpeed * angularSpeedToPlainCoeff;
            double force = -horizontalHitCoeff * (rollSpeedAtPoint + relativeSpeed.X);
            rollSpeedAtPoint += force;
            relativeSpeed.X += force;
            AngularSpeed = -rollSpeedAtPoint / angularSpeedToPlainCoeff;
        }

        private double calcAngularSpeedCharacteristic(double angularSpeed, double t)
        {
            return Constants.BallAngularDumpCoeff * t / 2 + 1 / Math.Sqrt(Math.Abs(angularSpeed));
        }

        private double calcAngularSpeedCharacteristicIndefiniteIntegral(double angularSpeed, double t)
        {
            return -Math.Sign(angularSpeed) * 2 / Constants.BallAngularDumpCoeff / calcAngularSpeedCharacteristic(angularSpeed, t);
        }

        private double calcAngularSpeedCharacteristicDefiniteIntegral(double angularSpeed, double t)
        {
            return calcAngularSpeedCharacteristicIndefiniteIntegral(angularSpeed, t) - calcAngularSpeedCharacteristicIndefiniteIntegral(angularSpeed, 0);
        }

        private Event checkForHits()
        {
            Event events = 0;

            var floorHitY = Constants.BallRadius - Constants.TableHeight;
            if (Position.Y < floorHitY)
            {
                events |= Event.FloorHit;

                Position.Y = 2 * floorHitY - Position.Y;
                Speed.Y = Constants.TableHitVerticalCoeff * Math.Abs(Speed.Y);

                endSet(GameState.IsOneOf(GameState.NotReadyToHit));
            }

            var tableHitY = Constants.BallRadius;
            var tableEndX = Constants.HalfTableWidth + Constants.BallRadius;
            if (Position.Y < tableHitY && Math.Abs(Position.X) < tableEndX)
            {
                if (Speed.Y < 0 && Position.Y - tableHitY > Math.Abs(Position.X) - tableEndX)
                {
                    events |= Event.TableHit;

                    Position.Y = 2 * tableHitY - Position.Y;
                    processBallHit(ref Speed, Constants.TableHitHorizontalCoeff, Constants.TableHitVerticalCoeff);

                    var isHitSide = BallSide == Misc.GetPlayerSideByIndex(HitSide);
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

                    Position.X = 2 * tableEndX * BallSide - Position.X;
                    Speed.X = Math.Abs(Speed.X) * BallSide;

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

        public void CopyFrom(State state)
        {
            HitSide = state.HitSide;
            GameState = state.GameState;
            Position = state.Position;
            Angle = state.Angle;
            Speed = state.Speed;
            AngularSpeed = state.AngularSpeed;
            Time = state.Time;
            Players = new Player[] { state.Players[0].Clone(), state.Players[1].Clone() };
        }

        public State Clone()
        {
            return new State(this);
        }
    }
}
