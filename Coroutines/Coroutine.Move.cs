#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static readonly List<WoWPoint> CurrentWaypointsList = new List<WoWPoint>();
        private static string oldDestMessage;
        private static MoveResult _lastMoveResult;

        public static async Task<bool> MoveTo(WoWPoint destination, string destinationMessage = null)
        {
            if (destinationMessage != null && destinationMessage != oldDestMessage)
            {
                oldDestMessage = destinationMessage;
                GarrisonButler.Log(destinationMessage);                
            }

            _lastMoveResult = Navigator.MoveTo(destination);

            Navigator.GetRunStatusFromMoveResult(_lastMoveResult);
            switch (_lastMoveResult)
            {
                case MoveResult.UnstuckAttempt:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: UnstuckAttempt.");
                    await Buddy.Coroutines.Coroutine.Sleep(500);
                    break;

                case MoveResult.Failed:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: Failed.");
                    return false;

                case MoveResult.ReachedDestination:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination.");
                    return false;
            }
            return true;
        }
        public static async Task<bool> MoveToInteract(WoWObject woWObject)
        {
            if (woWObject.WithinInteractRange)
            {
                GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination to interact with " + woWObject.SafeName);
                return false;
            }
            return await MoveTo(woWObject.Location, "[Navigation] Moving to interact with " + woWObject.SafeName);
        }
        
        private static async Task<List<WoWPoint>> GetFurthestWaypoint([NotNull] List<WoWPoint> waypoints)
        {
            if (waypoints == null) throw new ArgumentNullException("waypoints");
            if (!waypoints.Any()) throw new ArgumentException("waypoints empty");
            if (waypoints.Count == 1) return waypoints;

            var left = new List<WoWPoint>();
            int maxTry = Math.Min(waypoints.Count, 3);
            int indexToKeep = 0;
            for (int index = 0; index < maxTry; index++)
            {
                indexToKeep = index;
                WoWPoint waypoint = waypoints[index];
                await Buddy.Coroutines.Coroutine.Yield();
                if (!IsValidWaypoint(Me.Location, waypoint))
                    break;
            }
            indexToKeep -= 1;
            left.AddRange(waypoints.Skip(indexToKeep));
            GarrisonButler.Diagnostic("ENS skipped " + (waypoints.Count - left.Count) + " waypoints.");
            return left.Any() ? left : waypoints;
        }

        private static async Task<List<WoWPoint>> GetFurthestWaypoint2(List<WoWPoint> waypoints)
        {
            if (waypoints == null) throw new ArgumentNullException("waypoints");
            if (waypoints.Count < 3) return waypoints;
            int cpt = 0;
            var res = new List<WoWPoint>(waypoints);
            for (int index = 2; index < res.Count - 1; index++)
            {
                WoWPoint waypoint = res[index];
                WoWPoint old = res[index - 2];
                if (MinimalIsValidWaypoint(old, waypoint))
                {
                    if (IsValidWaypoint(old, waypoint))
                    {
                        res.RemoveAt(index - 1);
                        index--;
                        cpt++;
                    }
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            GarrisonButler.Diagnostic("ENS2 skipped " + cpt + " waypoints.");
            return res;
        }

        private static double Slope(WoWPoint p1, WoWPoint p2)
        {
            double m = p2.Z - p1.Z;
            double xy = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            return m/xy;
        }

        private static double RadianToDegree(double angle)
        {
            return angle*(180.0/Math.PI);
        }

        private static bool MinimalIsValidWaypoint(WoWPoint from, WoWPoint waypoint)
        {
            GarrisonButler.Diagnostic("height:" + Me.BoundingHeight);
            float height = Me.BoundingHeight/2;
            WoWPoint tempFrom = from + new WoWPoint(0, 0, height);
            WoWPoint tempWaypoint = waypoint + new WoWPoint(0, 0, height);

            bool quickTest = GameWorld.TraceLine(tempFrom, tempWaypoint, TraceLineHitFlags.Collision);
            return !quickTest && Slope(from, waypoint) < 1.2;
        }

        private static bool IsValidWaypoint(WoWPoint from, WoWPoint waypoint)
        {
            var lines = new List<WorldLine>();
            float height = Me.BoundingHeight + Me.BoundingHeight/10;
            float width = Me.BoundingRadius + Me.BoundingHeight/10;
            for (float p = 0; p <= 1; p += (1/(from.Distance(waypoint))))
            {
                WoWPoint pOrigin = from + (waypoint - from)*p;
                for (float i = height/3; i <= height; i += height/10)
                {
                    WoWPoint pTo = waypoint + new WoWPoint(0, 0, i);
                    pOrigin.Z = GetGroundZ(pTo) + i;

                    for (float j = -width; j <= width; j += width/10)
                    {
                        WoWPoint perpTo = GetPerpPointsBeginning(pTo, pOrigin, j);
                        WoWPoint perpOrigin = GetPerpPointsBeginning(pOrigin, pTo, j);
                        perpOrigin.Z = GetGroundZ(perpTo);
                        lines.Add(new WorldLine(perpOrigin, perpTo));
                    }
                }
            }
            bool[] resTrace;
            GameWorld.MassTraceLine(lines.ToArray(), TraceLineHitFlags.Collision, out resTrace);
            if (resTrace.Any(res => res)) return false;

            return Slope(lines.First().Start, lines.First().End) < 1.2;
        }

        private static WoWPoint GetPerpPointsBeginning(WoWPoint p1, WoWPoint p2, double distance)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double a = dy/dx;
            double b = p1.Y - a*p1.X;
            double a2 = -a;
            double b2 = p1.Y - a2*p1.X;
            double magnitude = Math.Sqrt(Math.Pow(1, 2) + Math.Pow(a2, 2));

            var N = new Vector2(1/(float) magnitude, (float) a2/(float) magnitude);
            var res1 = new WoWPoint(p1.X + distance*N.X, p1.Y + distance*N.Y, p1.Z);
            return res1;
        }

        public static float GetGroundZ(WoWPoint p)
        {
            WoWPoint ground;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + 0.5)), new WoWPoint(p.X, p.Y, (p.Z - 5)),
                TraceLineHitFlags.Collision, out ground);
            return ground != WoWPoint.Empty ? ground.Z : float.MinValue;
        }
    }
}