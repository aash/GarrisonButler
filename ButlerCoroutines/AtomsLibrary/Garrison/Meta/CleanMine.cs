using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class CleanMine : Atom
    {
        private Atom _currentAction;
        private WoWGameObject _currentTarget;
        private WaitTimer _timer = new WaitTimer(TimeSpan.FromMilliseconds(10000));
        
        private static readonly List<uint> MineItems = new List<uint>
        {
            232541, // Mine cart
            232542, // Blackrock Deposit 
            232543, // Rich Blackrock Deposit 
            232544, // True iron deposit
            232545 // Rich True iron deposit
        };

        private static readonly List<uint> OresMine = new List<uint>
        {
            232542, // Blackrock Deposit 
            232543, // Rich Blackrock Deposit 
            232544, // True iron deposit
            232545 // Rich True iron deposit
        };

        internal static readonly List<uint> MinesId = new List<uint>
        {
            7324, //ally 1
            7325, // ally 2
            7326, // ally 3
            7327, // horde 1
            7328, // horde 2
            7329 // horde 3
        };
        
        public CleanMine()
        {
            ShouldRepeat = true;
            Dependencies.Add(new UseMinerCoffee());
            Dependencies.Add(new UseMiningPick());
        }


        /// <summary>
        /// IS there any ores to harvest
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return true;
        }

        /// <summary>
        /// Is there no ore left
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            // Settings
            if (!GaBSettings.Get().HarvestMine)
            {
                GarrisonButler.Diagnostic("[Mine] Deactivated in user settings.");
                return true;
            }

            return !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Any(o => OresMine.Contains(o.Entry));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async override Task Action()
        {
            if (_currentAction == null 
                || _currentAction.IsFulfilled() 
                || _currentTarget == null 
                || (_timer.IsFinished && _currentTarget.Location.Distance(StyxWoW.Me.Location) > 25))
            {
                var allObjects =
                    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .GetEmptyIfNull()
                        .Where(o => MineItems.Contains(o.Entry) && !Objects.Blacklist.IsBlacklisted(o))
                        .ToArray();

                if (!allObjects.Any(o => OresMine.Contains(o.Entry)))
                    return;

                _currentTarget = GetClosestObjectSalesmanHb(StyxWoW.Me.Location, allObjects);
                _currentAction = new HarvestObject(_currentTarget);
                _timer.Reset();
            }
            await _currentAction.Execute();
        }

        public override string Name()
        {
            return "[CleanMine]";
        }   private static readonly Stopwatch PathGenerationStopwatch = new Stopwatch();

        public static WoWGameObject GetClosestObjectSalesmanHb(WoWPoint from, WoWGameObject[] objectsToArray)
        {
            PathGenerationStopwatch.Reset();
            PathGenerationStopwatch.Start();

            GarrisonButler.Diagnostic("Starting salesman algorithm.");

            //Generating data
            var objectsTo = objectsToArray.OrderBy(o => from.Distance(o.Location)).Take(5).ToArray();
            var objectsCount = objectsTo.Count();
            var vertics = new int[objectsCount + 1];
            var matrix = new double[objectsCount + 1, objectsCount + 1];

            // Adding starting point
            vertics[0] = 0;
            matrix[0, 0] = 0;
            // Adding distance from starting point to all objects
            for (int index = 0; index < objectsTo.Length; index++)
            {
                var gameObject = objectsTo[index];
                var distance = Navigator.PathDistance(@from, gameObject.Location) ?? float.MaxValue;
                matrix[0, index + 1] = (float)distance;
                matrix[index + 1, 0] = (float)distance;
            }

            // Adding distances from every points to all others
            for (int index1 = 0; index1 < objectsTo.Length; index1++)
            {
                vertics[index1 + 1] = index1 + 1;

                for (int index2 = index1; index2 < objectsTo.Length; index2++)
                {
                    if (index1 == index2)
                        matrix[index1 + 1, index2 + 1] = 0.0;
                    else
                    {
                        var distance = Navigator.PathDistance(@from, objectsTo[index2].Location) ?? float.MaxValue;
                        matrix[index1 + 1, index2 + 1] = distance;
                        matrix[index2 + 1, index1 + 1] = distance;
                    }
                }
                GarrisonButler.Diagnostic("[Salesman] Processed node in {0}ms", PathGenerationStopwatch.ElapsedMilliseconds);
            }
            double cost;
            var salesman = new Salesman(vertics, matrix);
            var route = salesman.Solve(out cost).ToArray();

            PathGenerationStopwatch.Stop();
            GarrisonButler.Diagnostic("[Salesman] Tour found in {0}ms, cost={1}, route:", PathGenerationStopwatch.ElapsedMilliseconds, cost);
            ObjectDumper.WriteToHb(route, 3);

            return objectsTo[route[1] - 1];
        }

    }
}
