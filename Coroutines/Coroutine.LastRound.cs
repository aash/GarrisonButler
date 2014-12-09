using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Styx;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static DateTime lastRoundCheckTime = DateTime.MinValue;
        private static int _lastRoundTemp;
        private static readonly List<WoWPoint> LastRoundWaypointsHorde = new List<WoWPoint>
        {
            new WoWPoint(5595.488, 4530.896, 126.0771),
            new WoWPoint(5502.666, 4475.98, 138.9149),
            new WoWPoint(5440.396, 4572.317, 135.7494),
        };
        private static readonly List<WoWPoint> LastRoundWaypointsAlly = new List<WoWPoint>
        {
            new WoWPoint(1917.989, 127.5877, 83.37553),
            new WoWPoint(1866.669, 226.6118, 76.641),
            new WoWPoint(1819.171, 212.0933, 71.44927),
        };

        private static bool CanRunLastRound()
        {
            TimeSpan elapsedTime = DateTime.Now - lastRoundCheckTime;
            if (elapsedTime.TotalMinutes > 30)
                return true;
            return false;
        }
        
        private static async Task<bool> LastRound()
        {
            if (!CanRunLastRound())
                return false;

            GarrisonBuddy.Log("Doing a last round to check if something was not too far to see before.");
            List<WoWPoint> myLastRoundPoints = Me.IsAlliance ? LastRoundWaypointsAlly : LastRoundWaypointsHorde;
            if (_lastRoundTemp > myLastRoundPoints.Count - 1)
            {
                _lastRoundTemp = 0;
                lastRoundCheckTime = DateTime.Now;
                return false;
            }
            if (await MoveTo(myLastRoundPoints[_lastRoundTemp]))
                return true;
            _lastRoundTemp++;
            return true;
        }
    }
}