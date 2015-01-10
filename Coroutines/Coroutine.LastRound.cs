﻿#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GarrisonButler.Config;
using Styx;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static DateTime lastRoundCheckTime = DateTime.MinValue;
        private static int _lastRoundTemp;

        private static readonly List<WoWPoint> LastRoundWaypointsHorde = new List<WoWPoint>
        {
            new WoWPoint(5595.488, 4530.896, 126.0771),
            new WoWPoint(5502.666, 4475.98, 138.9149),
            new WoWPoint(5440.396, 4572.317, 135.7494)
        };

        private static readonly List<WoWPoint> LastRoundWaypointsAlly = new List<WoWPoint>
        {
            new WoWPoint(1917.989, 127.5877, 83.37553),
            new WoWPoint(1866.669, 226.6118, 76.641),
            new WoWPoint(1819.171, 212.0933, 71.44927)
        };

        private static bool CanRunLastRound()
        {
            TimeSpan elapsedTime = DateTime.Now - lastRoundCheckTime;
            if (elapsedTime.TotalMinutes > GaBSettings.Get().TimeMinBetweenRun)
                return true;
            return false;
        }

        private static bool LastRoundInit = false;
        private static async Task<bool> LastRound()
        {
            if (!CanRunLastRound())
                return false;

            List<WoWPoint> myLastRoundPoints = Me.IsAlliance ? LastRoundWaypointsAlly : LastRoundWaypointsHorde;
            if (!LastRoundInit)
            {
                Random r = new Random(DateTime.Now.Second);
                float randomX = (float)(r.NextDouble()-0.5) * 5;
                float randomY = (float)(r.NextDouble() - 0.5) * 5;
                var toAdd = Me.IsAlliance ? TableAlliance : TableHorde;
                toAdd.X = toAdd.X + randomX;
                toAdd.Y = toAdd.Y + randomY;
                myLastRoundPoints.Add(toAdd);
                LastRoundInit = true;
            }

            if (_lastRoundTemp > myLastRoundPoints.Count - 1)
            {
                _lastRoundTemp = 0;
                lastRoundCheckTime = DateTime.Now;
                return false;
            }
            if (
                await
                    MoveTo(myLastRoundPoints[_lastRoundTemp],
                        "Doing a last round to check if something was not too far to see before."))
            {
                return true;
            }

            _lastRoundTemp++;
            return true;
        }
    }
}