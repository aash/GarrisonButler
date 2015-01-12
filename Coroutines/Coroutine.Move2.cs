#region

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly WaitTimer _waitTimer1 = new WaitTimer(TimeSpan.FromSeconds(1.0));
        private readonly WaitTimer _waitTimer2 = WaitTimer.FiveSeconds;
        private MeshMovePath _currentMovePath2;
        private StuckHandlerGaB _stuckHandlerGaB;
        private MoveResult _lastMoveResult;
        public override float PathPrecision { get; set; }

        public WaitTimer WaitTimer2
        {
            get { return _waitTimer2; }
        }

        public override void OnRemoveAsCurrent()
        {
            GarrisonButler.Log("Custom navigation System removed!");
            base.OnRemoveAsCurrent();
        }

        public override float? PathDistance(WoWPoint @from, WoWPoint to, float maxDistance = (float) 3.402823E+38)
        {
            return base.PathDistance(from, to, maxDistance);
        }


        public override MoveResult MovePath(MeshMovePath path)
        {
            if (StyxWoW.Me.Mounted)
            {
            //    if (path == null || path.Path == null || (path.Index < 0 || path.Index >= path.Path.Points.Length))
            //        return MoveResult.Failed;

            //    WoWUnit activeMover = WoWMovement.ActiveMover;
            //    if (activeMover == null)
            //        return MoveResult.Failed;
                double pathPrecision = StyxWoW.Me.Mounted ? 5 : 3;
                int max = 0;
                while (StyxWoW.Me.Location.Distance(path.Path.Points[path.Index]) < pathPrecision && max < 5)
                {
                    ++path.Index;
                    max++;
                    //return MoveResult.Moved;
                }

            //    Vector3 vector3 = path.Path.Points[path.Index];
            //    Navigator.PlayerMover.MoveTowards(vector3);
            //    return MoveResult.Moved;
            }
            //GarrisonButler.Log("Native MovePath");

            MoveResult res = base.MovePath(path);
            return res;
        }

        public override void OnSetAsCurrent()
        {
            base.OnSetAsCurrent();
            _stuckHandlerGaB = new StuckHandlerGaB(Coroutine.nativeNavigation.StuckHandler);
            this.StuckHandler = new StuckHandlerDummy();
            GarrisonButler.Log("Custom navigation System activated!");
        }

        public override bool CanNavigateWithin(WoWPoint @from, WoWPoint to, float distanceTolerance)
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
            // If the location to move to hasn't changed and we already
            // Reached the destination, then no need to keep going.
            if (CurrentDestination == location
                && _lastMoveResult != null // always true
                && _lastMoveResult == MoveResult.ReachedDestination)
                return MoveResult.ReachedDestination;

            if (_lastMoveResult == MoveResult.PathGenerated)
            {
                _stuckHandlerGaB.Reset();
            }
            CurrentDestination = location;
            if (location == WoWPoint.Zero)
                return MoveResult.Failed;

            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
                return MoveResult.Failed;

            WoWPoint moverLocation = activeMover.Location;

            if (moverLocation.Distance(location) < 4.0f)
            {
                Clear();
                _stuckHandlerGaB.Reset();
                _lastMoveResult = MoveResult.ReachedDestination;
                return MoveResult.ReachedDestination;
            }
            _lastMoveResult = MoveResult.Failed;
            if (_stuckHandlerGaB.IsStuck())
            {
                GarrisonButler.Diagnostic("Is stuck :O ! ");
                _stuckHandlerGaB.Unstick();
                _stuckHandlerGaB.Reset();
                _lastMoveResult = MoveResult.UnstuckAttempt;
                return MoveResult.UnstuckAttempt;
            }
            if (moverLocation.Distance(Coroutine.Dijkstra.ClosestToNodes(location)) < 5f)
            {
                Navigator.PlayerMover.MoveTowards(location);
                _lastMoveResult = MoveResult.Moved;
                return MoveResult.Moved;
            }
            if (Mount.ShouldMount(location))
            {
                Mount.StateMount(getDestination);
            }
            //if (_waitTimer1.IsFinished)
            //{
            //    WoWGameObject wowgameObject =
            //        ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
            //            .FirstOrDefault(param0 =>
            //            {
            //                if (param0.SubType == WoWGameObjectType.Door && ((WoWDoor) param0.SubObj).IsClosed &&
            //                    (!param0.Locked && param0.WithinInteractRange) && param0.CanUse())
            //                    return param0.CanUseNow();
            //                return false;
            //            });
            //    if (wowgameObject != null)
            //    {
            //        wowgameObject.Interact();
            //    }
            //    _waitTimer1.Reset();
            //}
            bool flag = _currentMovePath2 == null || _currentMovePath2.Path.End.DistanceSqr(location) > 9.0f;

            //else if (waitTimer2.IsFinished && Unnamed2(CurrentMovePath2, MoverLocation))
            //{
            //    WoWMovement.MoveStop();
            //    flag = true;
            //    waitTimer2.Reset();
            //}
            if (!flag)
            {
                _lastMoveResult = MovePath(_currentMovePath2);
                return _lastMoveResult;
            }

            WoWPoint startFp;
            WoWPoint endFp;
            if (moverLocation.DistanceSqr(location) > 100000 &&
                FlightPaths.ShouldTakeFlightpath(moverLocation, location, activeMover.MovementInfo.RunSpeed) &&
                FlightPaths.SetFlightPathUsage(moverLocation, location, out startFp, out endFp))
            {
                _stuckHandlerGaB.Reset();
                _lastMoveResult = MoveResult.PathGenerated;
                return _lastMoveResult;
            }
            PathFindResult path2 = FindPath(moverLocation, location);
            if (!path2.Succeeded)
            {
                _lastMoveResult = MoveResult.PathGenerationFailed;
                return _lastMoveResult;
            }
            _currentMovePath2 = new MeshMovePath(path2);
            _stuckHandlerGaB.Reset();

            _lastMoveResult = MoveResult.PathGenerated;
            return _lastMoveResult;
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
            DateTime startedAt = DateTime.Now;
            Vector3[] points = Coroutine.Dijkstra.GetPath2(pathFindResult.Start, pathFindResult.End);
            GarrisonButler.DiagnosticLogTimeTaken("GetPath2 inside FindPathInner", startedAt);
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

            GarrisonButler.DiagnosticLogTimeTaken("FindPathInner", startedAt);

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
                Elapsed = DateTime.Now - startedAt,
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

            DateTime startedAt = DateTime.Now;
            PathFindResult toReturn = FindPathInner(obj);
            GarrisonButler.DiagnosticLogTimeTaken("Fully creating path", startedAt);

            return toReturn;

            //this.\u0001 = false;
            // ISSUE: reference to a compiler-generated method
            //Task<PathFindResult> task = Task<PathFindResult>.Factory.StartNew(() => FindPathInner(obj));
            //DateTime startedAt = DateTime.Now;
            //try
            //{
            //    //StyxWoW.Memory.ReleaseFrame();
            //    //while it is not done with timeout 
            //    while ((DateTime.Now - startedAt).TotalMilliseconds < 10000/TreeRoot.TicksPerSecond || !task.IsCompleted)
            //    {
            //        try
            //        {
            //            //StyxWoW.Memory.AcquireFrame();
            //            ObjectManager.Update();
            //            WoWMovement.Pulse();
            //            StyxWoW.ResetAfk();
            //            //StyxWoW.Memory.ReleaseFrame();
            //        }
            //        catch (Exception ex)
            //        {
            //            Logging.WriteException(ex);
            //        }
            //    }

            //    GarrisonButler.DiagnosticLogTimeTaken("Fully creating path", startedAt);
            //    //StyxWoW.Memory.AcquireFrame();
            //    return task.Result;
            //}
            //catch (AggregateException ex)
            //{
            //    Logging.WriteException(ex);
            //}
            //finally
            //{
            //    task.Dispose();
            //}

            //return obj;
        }

        public override WoWPoint[] GeneratePath(WoWPoint @from, WoWPoint to)
        {
            return Coroutine.Dijkstra.GetPathWoW(@from, to);
        }

        public override bool AtLocation(WoWPoint point1, WoWPoint point2)
        {
            return Coroutine.Dijkstra.ClosestToNodes(point1).Distance(Coroutine.Dijkstra.ClosestToNodes(point2)) < 3;
        }

        public class StuckHandlerDummy : StuckHandler
        {
            public override bool IsStuck()
            {
                return false;
            }

            public override void Unstick()
            {
                return;
            }
        }

        public class StuckHandlerGaB : StuckHandler
        {
            private readonly StuckHandler _native;
            private readonly Stopwatch _stopwatch = new Stopwatch();
            private WoWPoint _cacheDestination = WoWPoint.Empty;
            private int _cpt;
            private WoWPoint _lastCheckedLocation = new WoWPoint(0, 0, 0);
            private delegate void CopiedFunction();
            private CopiedFunction UnstickCopy;
            public StuckHandlerGaB(StuckHandler native)
            {
                _native = native;
                UnstickCopy = native.Unstick;

                _lastCheckedLocation = StyxWoW.Me.Location;
                _stopwatch.Start();
            }

            public override bool IsStuck()
            {
                if (_stopwatch.ElapsedMilliseconds > 2000)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();
                    if (CurrentDestination != _cacheDestination)
                    {
                        _lastCheckedLocation = StyxWoW.Me.Location;
                        _cacheDestination = CurrentDestination;
                        return false;
                    }
                    if (StyxWoW.Me.Location.Distance(_lastCheckedLocation) < 3)
                    {
                        _cpt++;
                        return true;
                    }
                    _lastCheckedLocation = StyxWoW.Me.Location;
                }
                return false;
            }

            public override void Reset()
            {
                _stopwatch.Reset();
                _stopwatch.Start();
                _cpt = 0;
                _cacheDestination = CurrentDestination;
                _native.Reset();
            }

            public override void Unstick()
            {
                GarrisonButler.Diagnostic("Calling native unstick.");
                for (int i = 0; i < _cpt; i++)
                {
                    UnstickCopy();
                }
            }
        }
    }
}