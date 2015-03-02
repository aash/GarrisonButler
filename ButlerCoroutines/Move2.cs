#region

using System;
using System.Diagnostics;
using System.Linq;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.WoWInternals;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    public class NavigationGaB : MeshNavigator
    {
        internal static WoWPoint CurrentDestination;
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
            ButlerCoroutine.CustomNavigationLoaded = false;
            base.OnRemoveAsCurrent();
        }

        public override MoveResult MovePath(MeshMovePath path)
        {
            if (StyxWoW.Me.Mounted)
            {
                const double pathPrecision = 5;
                var max = 0;
                //GarrisonButler.Diagnostic("Moved time: {0}", WoWMovement.ActiveMover.MovementInfo.TimeMoved);
                //GarrisonButler.Diagnostic("path.Index: {0}", path.Index);
                if(path.Index >= path.Path.Points.Length)
                {
                    GarrisonButler.Warning("[MovePath] Index outside bounds of array for Path.Points.Length={0} and path.Index={1}",
                        path.Path.Points.Length, path.Index);
                }
                while (path.Path.Points.Length >= path.Index && StyxWoW.Me.Location.Distance(path.Path.Points[path.Index]) < pathPrecision && path.Index > 3 &&
                       max < 5)
                {
                    path.Index++;
                    max++;
                }
            }
            ////GarrisonButler.Log("Native MovePath");
            //if (StyxWoW.Me.Location.Distance(path.Path.Points[path.Index]) < 1)
            //{
            //    path.Index++;
            //}
            var res = base.MovePath(path);
            return res;
        }

        public override void OnSetAsCurrent()
        {
            base.OnSetAsCurrent();
            _stuckHandlerGaB = new StuckHandlerGaB(ButlerCoroutine.NativeNavigation.StuckHandler);
            StuckHandler = new StuckHandlerDummy();
            ButlerCoroutine.CustomNavigationLoaded = true;
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

        private static WoWPoint GetDestination()
        {
            return CurrentDestination;
        }

        public override MoveResult MoveTo(WoWPoint location)
        {
            // If the location to move to hasn't changed and we already
            // Reached the destination, then no need to keep going.
            if (CurrentDestination == location
                && _lastMoveResult == MoveResult.ReachedDestination)
                return MoveResult.ReachedDestination;

            if (_lastMoveResult == MoveResult.PathGenerated
                || _lastMoveResult == MoveResult.ReachedDestination)
            {
                _stuckHandlerGaB.Reset();
            }
            CurrentDestination = location;
            if (location == WoWPoint.Zero)
            {
                GarrisonButler.Diagnostic("MoveTo Failed - location == WoWPoint.Zero");
                return MoveResult.Failed;
            }

            var activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
            {
                GarrisonButler.Diagnostic("MoveTo Failed - activeMover == null");
                return MoveResult.Failed;
            }

            var moverLocation = activeMover.Location;

            if (moverLocation.Distance(location) < 2.0f)
            {
                Clear();
                _lastMoveResult = MoveResult.ReachedDestination;
                return MoveResult.ReachedDestination;
            }
            _lastMoveResult = MoveResult.Failed;
            if (_stuckHandlerGaB.IsStuck())
            {
                GarrisonButler.Diagnostic("Is stuck :O ! ");
                _stuckHandlerGaB.Unstick();
                _lastMoveResult = MoveResult.UnstuckAttempt;
                return MoveResult.UnstuckAttempt;
            }
            if (moverLocation.Distance(ButlerCoroutine.Dijkstra.ClosestToNodes(location)) < 5f)
            {
                Navigator.PlayerMover.MoveTowards(location);
                _lastMoveResult = MoveResult.Moved;
                return MoveResult.Moved;
            }
            if (Mount.ShouldMount(location))
            {
                Mount.StateMount(GetDestination);
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
            var flag = _currentMovePath2 == null || _currentMovePath2.Path.End.DistanceSqr(location) > 9.0f;

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
                _lastMoveResult = MoveResult.PathGenerated;
                return _lastMoveResult;
            }
            var path2 = FindPath(moverLocation, location);
            if (!path2.Succeeded)
            {
                _lastMoveResult = MoveResult.PathGenerationFailed;
                return _lastMoveResult;
            }
            _currentMovePath2 = new MeshMovePath(path2);

            _lastMoveResult = MoveResult.PathGenerated;
            return _lastMoveResult;
        }


        private static PathFindResult FindPathInner(PathFindResult pathFindResult)
        {
            var startedAt = DateTime.Now;
            var points = ButlerCoroutine.Dijkstra.GetPath2(pathFindResult.Start, pathFindResult.End);
            GarrisonButler.DiagnosticLogTimeTaken("GetPath2 inside FindPathInner", startedAt);
            var abilities = new AbilityFlags[points.Count()];
            var polygonReferences = new PolygonReference[points.Count()];
            var straightpaths = new StraightPathFlags[points.Count()];
            var areaTypes = new AreaType[points.Count()];

            for (var index = 0; index < points.Length; index++)
            {
                straightpaths[index] = StraightPathFlags.None;
                polygonReferences[index] = new PolygonReference();
                abilities[index] = AbilityFlags.Run;
                areaTypes[index] = AreaType.Ground;
            }

            GarrisonButler.DiagnosticLogTimeTaken("FindPathInner", startedAt);

            return new PathFindResult
            {
                AbilityFlags = abilities,
                Aborted = false,
                Status = Status.Success,
                Flags = straightpaths,
                Points = points,
                Polygons = polygonReferences,
                PolyTypes = areaTypes,
                Start = pathFindResult.Start,
                End = pathFindResult.End,
                Elapsed = DateTime.Now - startedAt,
                IsPartialPath = false
            };
        }

        private new static PathFindResult FindPath(WoWPoint start, WoWPoint end)
        {
            GarrisonButler.Diagnostic("[Navigation] Find path from {0} to {1}", start, end);
            var obj = new PathFindResult {Start = start, End = end};
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
            PathFindResult toReturn = new PathFindResult();
            var startedAt = DateTime.Now;
            toReturn = FindPathInner(obj);
            
            GarrisonButler.DiagnosticLogTimeTaken("Fully creating path", startedAt);

            return toReturn;
        }

        public override WoWPoint[] GeneratePath(WoWPoint @from, WoWPoint to)
        {
            return ButlerCoroutine.Dijkstra.GetPathWoW(@from, to);
        }

        public override bool AtLocation(WoWPoint point1, WoWPoint point2)
        {
            return ButlerCoroutine.Dijkstra.ClosestToNodes(point1).Distance(ButlerCoroutine.Dijkstra.ClosestToNodes(point2)) < 3;
        }

        public class StuckHandlerDummy : StuckHandler
        {
            public override bool IsStuck()
            {
                return false;
            }

            public override void Unstick()
            {
            }
        }

        public class StuckHandlerGaB : StuckHandler
        {
            private readonly StuckHandler _native;
            private readonly Stopwatch _stopwatch = new Stopwatch();
            private WoWPoint _cacheDestination = WoWPoint.Empty;
            private int _cpt;
            private WoWPoint _lastCheckedLocation;

            private delegate void CopiedFunction();

            private readonly CopiedFunction _unstickCopy;

            public StuckHandlerGaB(StuckHandler native)
            {
                _native = native;
                _unstickCopy = native.Unstick;

                _lastCheckedLocation = StyxWoW.Me.Location;
                _stopwatch.Start();
            }

            public override bool IsStuck()
            {
                if (_stopwatch.ElapsedMilliseconds <= 2000) return false;
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
                GarrisonButler.Diagnostic("Calling native unstick : {0}", _cpt);
                for (var i = 0; i < _cpt; i++)
                {
                    _unstickCopy();
                }
            }
        }
    }
}