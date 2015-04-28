using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using Styx;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    public class LastRound : Atom
    {
        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        private static readonly List<WoWPoint> LastRoundWaypointsHorde = new List<WoWPoint>
        {
            new WoWPoint(5595.488, 4530.896, 126.0771),
            new WoWPoint(5502.666, 4475.98, 138.9149),
            new WoWPoint(5440.396, 4572.317, 135.7494),
            new WoWPoint(5472.146, 4610.901, 134.6462)
        };
        private static readonly List<WoWPoint> LastRoundWaypointsAlly = new List<WoWPoint>
        {
            new WoWPoint(1917.989, 127.5877, 83.37553),
            new WoWPoint(1866.669, 226.6118, 76.641),
            new WoWPoint(1819.171, 212.0933, 71.44927)
        };


        private DateTime _lastRoundCheckTime = DateTime.MinValue;
        private int _lastRoundTemp;
        private List<WoWPoint> myLastRoundPoints;
        private Atom _currentAction = null; 
        public LastRound()
        {
            var r = new Random(DateTime.Now.Second);
            var randomX = (float)(r.NextDouble() - 0.5) * 5;
            var randomY = (float)(r.NextDouble() - 0.5) * 5;
            WoWPoint toAdd;

            if (StyxWoW.Me.IsAlliance)
            {
                myLastRoundPoints = LastRoundWaypointsAlly;
                toAdd = TableAlliance;
            }
            else
            {
                myLastRoundPoints = LastRoundWaypointsHorde; 
                toAdd = TableHorde;
            }
            toAdd.X = toAdd.X + randomX;
            toAdd.Y = toAdd.Y + randomY;

            myLastRoundPoints.Add(toAdd);
            _currentAction = new MoveTo(myLastRoundPoints[_lastRoundTemp]);
        }

        public override bool RequirementsMet()
        {
            return true; 
        }

        public override bool IsFulfilled()
        {
            if (GaBSettings.Get().DisableLastRoundCheck)
                return true;

            var elapsedTime = DateTime.Now - _lastRoundCheckTime;
            return elapsedTime.TotalMinutes < GaBSettings.Get().TimeMinBetweenRun;
        }

        public async override Task Action()
        {
            if (_currentAction.IsFulfilled())
            {
                _lastRoundTemp++;

                if (_lastRoundTemp > myLastRoundPoints.Count - 1)
                {
                    _lastRoundTemp = 0;
                    _lastRoundCheckTime = DateTime.Now;
                    return;
                }
                _currentAction = new MoveTo(myLastRoundPoints[_lastRoundTemp]);
                return;
            }

            await _currentAction.Execute();
        }

        public override string Name()
        {
            return "[LastRound]";
        }
    }
}