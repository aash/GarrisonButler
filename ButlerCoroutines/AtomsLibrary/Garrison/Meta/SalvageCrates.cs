using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class SalvageCrates:Atom
    {
        // Locations in building referential 
        private static WoWPoint _allyLocation = new WoWPoint(3.320689, -0.4705446, 0.7602997);
        private static WoWPoint _hordeLocation = new WoWPoint(3.320689, -0.4705446, 0.7602997);
        private WoWPoint _location = default(WoWPoint); 
        private Building _salvageBuilding = null;

        private static List<uint> _salvageCratesIds = new List<uint>
        {
            114120, // Big Crate of Salvage lvl 3
            114119, // Crate of Salvage lvl 2
            114116 // Bag of Salvaged Goods lvl 1
        };


        public SalvageCrates()
        {
            _salvageBuilding = ButlerCoroutine._buildings.FirstOrDefault(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
            if (_salvageBuilding == null)
                return;

            var buildingAsObject = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().GetEmptyIfNull().FirstOrDefault(o => _salvageBuilding.buildingIDs.Contains(o.Entry));

            if (buildingAsObject != default(WoWGameObject))
            {
                // Calculate transformations
                var matrix = buildingAsObject.GetWorldMatrix();
                GarrisonButler.Diagnostic("[NavigationData] World Matrix: " + matrix);
                _location = Vector3.Transform(StyxWoW.Me.IsAlliance ? _allyLocation : _hordeLocation, matrix);
                Dependencies.Add(new MoveTo(_location,1));
            }
        }
        public override bool RequirementsMet()
        {
            return _location != default(WoWPoint);
        }

        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().SalvageCrates)
                return true;

            if (_salvageBuilding == null)
                return true;

            var salvageCratesFound = HbApi.GetItemsInBags(_salvageCratesIds);
            if (!salvageCratesFound.Any())
                return true;

            return false; 
        }

        public async override Task Action()
        {
            if (StyxWoW.Me.Mounted)
            {
                await CommonCoroutines.Dismount("Salvaging");
                await CommonCoroutines.SleepForLagDuration();
            }

            var salvageCratesFound = HbApi.GetItemsInBags(_salvageCratesIds);
            foreach (var salvageCrate in salvageCratesFound)
            {
                salvageCrate.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(5000, () => !StyxWoW.Me.IsCasting);
                await Buddy.Coroutines.Coroutine.Yield();
            }
        }

        public override string Name()
        {
            return "[Salvaging]";
        }
    }
}
