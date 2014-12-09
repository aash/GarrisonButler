﻿using System;
using System.Collections.Generic;
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

namespace GarrisonBuddy
{
    public class NavigationGaB : MeshNavigator
    {
        private readonly WaitTimer waitTimer1 = new WaitTimer(TimeSpan.FromSeconds(1.0));
        private readonly WaitTimer waitTimer2 = WaitTimer.FiveSeconds;
        private WoWPoint CurrentDestination;
        private MeshMovePath CurrentMovePath2;
        private StuckHandler stuckHandlerGaB;
        public NavigationGaB()
        {

        }
        public override float PathPrecision { get; set; }

        public override void OnRemoveAsCurrent()
        {
            base.OnRemoveAsCurrent();
        }

        public override float? PathDistance(WoWPoint @from, WoWPoint to, float maxDistance = (float) 3.402823E+38)
        {
            return base.PathDistance(@from, to, maxDistance);
        }

        public override MoveResult MovePath(MeshMovePath path)
        {
            var res = base.MovePath(path);
            return res;
        }

        public override void OnSetAsCurrent()
        {
            base.OnSetAsCurrent();
            stuckHandlerGaB = this.StuckHandler;
            stuckHandlerGaB.Reset();
        }

        public override bool CanNavigateWithin(WoWPoint @from, WoWPoint to, float distanceTolerancy)
        {
            return base.CanNavigateWithin(@from, to, distanceTolerancy);
        }

        public override bool CanNavigateFully(WoWPoint @from, WoWPoint to)
        {

            return base.CanNavigateFully(@from, to);
        }

        private WoWPoint getDestination()
        {
            return CurrentDestination;
        }

        private static readonly Stopwatch StuckWatch = new Stopwatch();

        public override MoveResult MoveTo(WoWPoint location)
        {

            CurrentDestination = location;
            if (location == WoWPoint.Zero)
                return MoveResult.Failed;

            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
                return MoveResult.Failed;

            WoWPoint MoverLocation = activeMover.Location;
            //if (location1.Distance2DSqr(current) < PathPrecision * PathPrecision && location1.Z - current.Z < 4.5f)

            if (stuckHandlerGaB.IsStuck())
            {
                GarrisonBuddy.Diagnostic("Is stuck! ");
                stuckHandlerGaB.Unstick();
                return MoveResult.UnstuckAttempt;
            }
            if (MoverLocation.Distance2DSqr(Coroutine.Dijkstra.ClosestToNodes(location)) < 1f)
            //PathPrecision * PathPrecision && MoverLocation.Z - current.Z < 4.5f)
            {
                //Navigator.PlayerMover.MoveTowards(current);
                Clear();
                stuckHandlerGaB.Reset();
                StuckWatch.Reset();
                return MoveResult.ReachedDestination;
            }
            if (MoverLocation.Distance2DSqr(Coroutine.Dijkstra.ClosestToNodes(location)) < 3f)
            //PathPrecision * PathPrecision && MoverLocation.Z - current.Z < 4.5f)
            {
                //Navigator.PlayerMover.MoveTowards(current);
                Navigator.PlayerMover.MoveTowards(location);
                return MoveResult.Moved;
            }
            // ISSUE: reference to a compiler-generated field
            if (Mount.ShouldMount(location))
            {
                // ISSUE: reference to a compiler-generated method
                Mount.StateMount(getDestination);
            }
            if (waitTimer1.IsFinished) // Some kind of timer for moving object or interactions
            {
                WoWGameObject woWgameObject =
                    ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
                        .FirstOrDefault(param0 =>
                        {
                            if (param0.SubType == WoWGameObjectType.Door && ((WoWDoor) param0.SubObj).IsClosed &&
                                (!param0.Locked && param0.WithinInteractRange) && param0.CanUse())
                                return param0.CanUseNow();
                            return false;
                        });
                if (woWgameObject != null)
                {
                    //Logging.WriteDiagnostic(MeshNavigator.(148289),
                    //    new object[2] // Moving to game object to interact
                    //    {
                    //        (object) woWgameObject.Name,
                    //        (object) (bool) (woWgameObject.Locked ? 1 : 0)
                    //    });
                    woWgameObject.Interact();
                }
                waitTimer1.Reset();
            }
            bool flag = false;
            // ISSUE: reference to a compiler-generated field
            if (CurrentMovePath2 == null || CurrentMovePath2.Path.End.DistanceSqr(location) > 9.0f)
            {
                flag = true;
            }

            else if (waitTimer2.IsFinished && Unnamed2(CurrentMovePath2, MoverLocation))
                // about not being on the path? 
            {
                WoWMovement.MoveStop();
                flag = true;
                waitTimer2.Reset();
            }
            if (!flag)
            {
                stuckHandlerGaB.Reset();
                return MovePath(CurrentMovePath2);
            }
            if (flag)
            {
                WoWPoint startFp;
                WoWPoint endFp;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                if (MoverLocation.DistanceSqr(location) > 160000.0 &&
                    FlightPaths.ShouldTakeFlightpath(MoverLocation, location, activeMover.MovementInfo.RunSpeed) &&
                    FlightPaths.SetFlightPathUsage(MoverLocation, location, out startFp, out endFp))
                    return MoveResult.PathGenerated;
                // ISSUE: reference to a compiler-generated field
                PathFindResult path = FindPath(MoverLocation, location);
                if (!path.Succeeded)
                {
                    //if (path.Aborted)
                    //    Logging.WriteDiagnostic(\u0086\u0002.\u0088\u0010(), MeshNavigator.\u0010(148427),
                    //        (object) path.Start, (object) path.End, (object) \u000F.\u0001(path.Elapsed),
                    //        TreeRoot.State == TreeRootState.Stopping
                    //            ? (object) MeshNavigator.\u0010(148529)
                    //            : (object) MeshNavigator.\u0010(148520));
                    //else
                    //    Logging.WriteDiagnostic(\u0086\u0002.\u0088\u0010(), MeshNavigator.\u0010(148546),
                    //        (object) path.Start, (object) path.End, (object) LoadedMapNames,
                    //        (object) \u000F.\u0001(path.Elapsed), (object) path.FailStep);
                    stuckHandlerGaB.Reset();
                    return MoveResult.PathGenerationFailed;
                }
                //if (path.IsPartialPath)
                //    Logging.WriteDiagnostic(\u0086\u0002.\u008E\u0010(), MeshNavigator.\u0010(23194),
                //        (object) path.Start, (object) path.End, (object) \u000F.\u0001(path.Elapsed));
                //else if (path.Elapsed.TotalMilliseconds > 50.0)
                //    Logging.WriteDiagnostic(MeshNavigator.\u0010(23279), (object) path.Start, (object) path.End,
                //        (object) \u000F.\u0001(path.Elapsed));
                CurrentMovePath2 = new MeshMovePath(path);
                stuckHandlerGaB.Reset();
                //this.\u0001(CurrentMovePath, (Vector3) location1);
                stuckHandlerGaB.Reset();
                return MoveResult.PathGenerated;
            }
            stuckHandlerGaB.Reset();
            return MovePath(CurrentMovePath2);
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
            Task<PathFindResult> task = Task<PathFindResult>.Factory.StartNew(() => FindPathInner(obj));
            DateTime startedAt = DateTime.Now;
            try
            {
                //Start generation of path as async
                //if (\u001F\u0003.\u007E\u009F\u0012((object) task, 10))
                //   return task.Result;
                //FrameLockRelease frameLockRelease = GreyMagic.ExternalReadCache(.\u001E\u0010.\u007E\u001A\u001E((object) StyxWoW.Memory, true);

                //while it is not done with timeout 
                while ((DateTime.Now - startedAt).TotalMilliseconds < 1000/TreeRoot.TicksPerSecond || task.IsCompleted)
                    //(!\u001F\u0003.\u007E\u009F\u0012((object) task, 1000/(int) TreeRoot.TicksPerSecond))
                {
                    try
                    {
                        //FrameLock frameLock = GreyMagic.; // \u0008\u0004.\u007E\u001D\u0014((object) StyxWoW.Memory);
                        try
                        {
                            ObjectManager.Update();
                            WoWMovement.Pulse();
                        }
                        finally
                        {
                            //if (frameLock != null)
                            //\u0008.\u007E\u000E\u0003((object) frameLock);
                        }
                        //this.\u0001 = StyxWoW.Me.IsActuallyInCombat;
                        StyxWoW.ResetAfk();
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                    }
                }
                return task.Result;
            }
            catch (AggregateException ex)
            {
                //throw new Exception(MeshNavigator.\u0010(148705), \u0019\u0003.\u007E\u0095\u0012((object) ex));
            }
            finally
            {
                task.Dispose(); //\u0008.\u007E\u0010\u0004((object) task);
            }

            return obj;
        }

        public override WoWPoint[] GeneratePath(WoWPoint @from, WoWPoint to)
        {
            Logging.Write("TEST GENERATE PATH");
            return Coroutine.Dijkstra.GetPathWoW(@from, to);
        }

        public override bool AtLocation(WoWPoint point1, WoWPoint point2)
        {
            Logging.Write("TEST AtLocation");
            return Coroutine.Dijkstra.ClosestToNodes(point1).Distance(Coroutine.Dijkstra.ClosestToNodes(point2)) < 3;
        }
    }
}