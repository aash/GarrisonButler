#region

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Vector3 = Tripper.Tools.Math.Vector3;

#endregion

namespace GarrisonButler
{
    public class NavigationGaB : MeshNavigator
    {
        internal static WoWPoint CurrentDestination;
        private static readonly Stopwatch StuckWatch = new Stopwatch();
        private readonly WaitTimer waitTimer1 = new WaitTimer(TimeSpan.FromSeconds(1.0));
        private readonly WaitTimer waitTimer2 = WaitTimer.FiveSeconds;
        private MeshMovePath CurrentMovePath2;
        private StuckHandlerGaB stuckHandlerGaB;


        public override float PathPrecision { get; set; }

        public override void OnRemoveAsCurrent()
        {
            GarrisonButler.Log("Custom navigation System removed!");
            base.OnRemoveAsCurrent();
        }

        public override float? PathDistance(WoWPoint @from, WoWPoint to, float maxDistance = (float) 3.402823E+38)
        {
            return base.PathDistance(@from, to, maxDistance);
        }

        public override MoveResult MovePath(MeshMovePath path)
        {
            if (StyxWoW.Me.Mounted)
            {
                if (path == null || path.Path == null || (path.Index < 0 || path.Index >= path.Path.Points.Length))
                    return MoveResult.Failed;

                WoWUnit activeMover = WoWMovement.ActiveMover;
                if ((WoWObject) activeMover == (WoWObject) null)
                    return MoveResult.Failed;
                double pathPrecision = StyxWoW.Me.Mounted ? 5 : 3;
                if (StyxWoW.Me.Location.Distance(path.Path.Points[path.Index]) < pathPrecision)
                {
                    ++path.Index;
                    return MoveResult.Moved;
                }

                Tripper.Tools.Math.Vector3 vector3 = path.Path.Points[path.Index];
                Navigator.PlayerMover.MoveTowards((WoWPoint) vector3);
                return MoveResult.Moved;
            }

            MoveResult res = base.MovePath(path);
            return res;
        }

        public override void OnSetAsCurrent()
        {
            base.OnSetAsCurrent();
            stuckHandlerGaB = new StuckHandlerGaB(Coroutine.oldNavigation.StuckHandler);
            GarrisonButler.Log("Custom navigation System activated!");
        }

        public override bool CanNavigateWithin(WoWPoint @from, WoWPoint to, float distanceTolerancy)
        {
            return true;
        }

        public override bool CanNavigateFully(WoWPoint @from, WoWPoint to)
        {
            return true;
        }

        private WoWPoint getDestination()
        {
            return CurrentDestination;
        }

        public override MoveResult MoveTo(WoWPoint location)
        {
            CurrentDestination = location;
            if (location == WoWPoint.Zero)
                return MoveResult.Failed;

            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
                return MoveResult.Failed;

            WoWPoint MoverLocation = activeMover.Location;

            if (stuckHandlerGaB.IsStuck())
            {
                GarrisonButler.Diagnostic("Is stuck :O ! ");
                stuckHandlerGaB.Unstick();
                stuckHandlerGaB.Reset();
                return MoveResult.UnstuckAttempt;
            }
            if (MoverLocation.Distance(location) < 2.4f)
            {
                Clear();
                stuckHandlerGaB.Reset();
                return MoveResult.ReachedDestination;
            }
            if (MoverLocation.Distance(Coroutine.Dijkstra.ClosestToNodes(location)) < 8f)
            {
                Navigator.PlayerMover.MoveTowards(location);
                return MoveResult.Moved;
            }
            if (Mount.ShouldMount(location))
            {
                Mount.StateMount(getDestination);
            }
            if (waitTimer1.IsFinished)
            {
                WoWGameObject wowgameObject =
                    ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
                        .FirstOrDefault(param0 =>
                        {
                            if (param0.SubType == WoWGameObjectType.Door && ((WoWDoor) param0.SubObj).IsClosed &&
                                (!param0.Locked && param0.WithinInteractRange) && param0.CanUse())
                                return param0.CanUseNow();
                            return false;
                        });
                if (wowgameObject != null)
                {
                    wowgameObject.Interact();
                }
                waitTimer1.Reset();
            }
            bool flag = CurrentMovePath2 == null || CurrentMovePath2.Path.End.DistanceSqr(location) > 9.0f;

            //else if (waitTimer2.IsFinished && Unnamed2(CurrentMovePath2, MoverLocation))
            //{
            //    WoWMovement.MoveStop();
            //    flag = true;
            //    waitTimer2.Reset();
            //}
            if (!flag)
            {
                return MovePath(CurrentMovePath2);
            }

            WoWPoint startFp;
            WoWPoint endFp;
            stuckHandlerGaB.Reset();
            if (MoverLocation.DistanceSqr(location) > 100000 &&
                FlightPaths.ShouldTakeFlightpath(MoverLocation, location, activeMover.MovementInfo.RunSpeed) &&
                FlightPaths.SetFlightPathUsage(MoverLocation, location, out startFp, out endFp))
                return MoveResult.PathGenerated;
            PathFindResult path2 = FindPath(MoverLocation, location);
            if (!path2.Succeeded)
            {
                return MoveResult.PathGenerationFailed;
            }
            CurrentMovePath2 = new MeshMovePath(path2);
            return MoveResult.PathGenerated;
        }

        private bool Unnamed2(MeshMovePath param0, Vector3 param1)
        {
            if ((WoWMovement.ActiveMover ?? StyxWoW.Me).IsFalling || param0.Index <= 0 ||
                (param0.Index >= param0.Path.Points.Length))
                return true;
            return false;
        }


        private PathFindResult FindPathInner(PathFindResult pathFindResult)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Vector3[] points = Coroutine.Dijkstra.GetPath2(pathFindResult.Start, pathFindResult.End);
            stopWatch.Stop();
            var abilities = new AbilityFlags[points.Count()];
            var PolygonReferences = new PolygonReference[points.Count()];
            var straightpaths = new StraightPathFlags[points.Count()];
            var AreaTypes = new AreaType[points.Count()];

            for (int index = 0; index < points.Length; index++)
            {
                straightpaths[index] = StraightPathFlags.None;
                PolygonReferences[index] = new PolygonReference();
                abilities[index] = AbilityFlags.Run;
                AreaTypes[index] = AreaType.Ground;
            }


            return new PathFindResult
            {
                AbilityFlags = abilities,
                Aborted = false,
                Status = Status.Success,
                Flags = straightpaths,
                Points = points,
                Polygons = PolygonReferences,
                PolyTypes = AreaTypes,
                Start = pathFindResult.Start,
                End = pathFindResult.End,
                Elapsed = stopWatch.Elapsed,
                IsPartialPath = false
            };
        }

        private PathFindResult FindPath(WoWPoint start, WoWPoint end)
        {
            var obj = new PathFindResult();
            obj.Start = start;
            obj.End = end;
            if (TreeRoot.State == TreeRootState.Stopping)
            {
                return new PathFindResult
                {
                    AbilityFlags = new AbilityFlags[0],
                    Aborted = true,
                    Status = Status.Failure,
                    Flags = new StraightPathFlags[0],
                    Points = new Vector3[0],
                    Polygons = new PolygonReference[0],
                    PolyTypes = new AreaType[0],
                    Start = obj.Start,
                    End = obj.End
                };
            }
            //this.\u0001 = false;
            // ISSUE: reference to a compiler-generated method
            //TODO - JUSTIN - This is causing graphical lag / FPS drops
            Task<PathFindResult> task = Task<PathFindResult>.Factory.StartNew(() => FindPathInner(obj));
            DateTime startedAt = DateTime.Now;
            try
            {
                //StyxWoW.Memory.ReleaseFrame();
                //while it is not done with timeout 
                while ((DateTime.Now - startedAt).TotalMilliseconds < 10000/TreeRoot.TicksPerSecond || !task.IsCompleted)
                {
                    try
                    {
                        //StyxWoW.Memory.AcquireFrame();
                        ObjectManager.Update();
                        WoWMovement.Pulse();
                        StyxWoW.ResetAfk();
                        //StyxWoW.Memory.ReleaseFrame();
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                    }
                }
                
                GarrisonButler.Log("Took " + (DateTime.Now - startedAt).TotalMilliseconds.ToString() + "ms to fully create path.");
                //StyxWoW.Memory.AcquireFrame();
                return task.Result;
            }
            catch (AggregateException ex)
            {
                Logging.WriteException(ex);
            }
            finally
            {
                task.Dispose();
            }

            return obj;
        }

        public override WoWPoint[] GeneratePath(WoWPoint @from, WoWPoint to)
        {
            return Coroutine.Dijkstra.GetPathWoW(@from, to);
        }

        public override bool AtLocation(WoWPoint point1, WoWPoint point2)
        {
            return Coroutine.Dijkstra.ClosestToNodes(point1).Distance(Coroutine.Dijkstra.ClosestToNodes(point2)) < 3;
        }

        public class StuckHandlerGaB : StuckHandler
        {
            private readonly StuckHandler Native;
            private readonly Stopwatch stopwatch = new Stopwatch();
            private WoWPoint cacheDestination = WoWPoint.Empty;
            private int cpt;
            private WoWPoint lastCheckedLocation = new WoWPoint(0, 0, 0);

            public StuckHandlerGaB(StuckHandler native)
            {
                Native = native;
                lastCheckedLocation = StyxWoW.Me.Location;
                stopwatch.Start();
            }

            public override bool IsStuck()
            {
                if (stopwatch.ElapsedMilliseconds > 4000)
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    if (CurrentDestination != cacheDestination)
                    {
                        lastCheckedLocation = StyxWoW.Me.Location;
                        cacheDestination = CurrentDestination;
                        return false;
                    }
                    if (StyxWoW.Me.Location.Distance(lastCheckedLocation) < 3)
                    {
                        cpt++;
                        return true;
                    }
                    lastCheckedLocation = StyxWoW.Me.Location;
                }
                return false;
            }

            public override void Reset()
            {
                stopwatch.Reset();
                stopwatch.Start();
                cpt = 0;
                cacheDestination = CurrentDestination;
            }

            public override void Unstick()
            {
                for (int i = 0; i < cpt; i++)
                {
                    Native.Unstick();
                }
            }
        }
    }
}