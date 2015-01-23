#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Objects;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static List<DailyProfession> _detectedDailyProfessions;

        private static void InitializeDailies()
        {
            if (_detectedDailyProfessions != null)
                return;

            _detectedDailyProfessions = new List<DailyProfession>();

            IEnumerable<DailyProfession> dailyActivated =
                DailyProfession.AllDailies.Where(
                    d =>
                    {
                        var dailyProfession = GaBSettings.Get()
                            .DailySettings.FirstOrDefault(d2 => d2.ItemId == d.ItemId);
                        return dailyProfession != null && dailyProfession.Activated;
                    }).ToList();

            //GarrisonButler.Diagnostic("[Profession] AllDailies:");
            //ObjectDumper.WriteToHB(DailyProfession.AllDailies, 1); 
            //GarrisonButler.Diagnostic("[Profession] _detectedDailyProfessions:");
            //ObjectDumper.WriteToHB(_detectedDailyProfessions, 1);

            if (!dailyActivated.Any())
            {
                GarrisonButler.Diagnostic("[Profession] No daily activated in settings.");
                return;
            }

            foreach (
                var daily in
                    dailyActivated.Where(
                        daily => !_detectedDailyProfessions.Any(d => d.ItemId == daily.ItemId && d.Spell == null)))
            {
                daily.Initialize();
                if (daily.Spell == null)
                    continue;

                GarrisonButler.Log("Adding daily CD: {0} - {1}", daily.TradeskillId, daily.Name);
                _detectedDailyProfessions.Add(daily);
            }
        }

        private static async Task<Result> CanRunDailies()
        {
            // Check
            InitializeDailies();

            if (!_detectedDailyProfessions.Any())
            {
                GarrisonButler.Diagnostic("[Profession] No daily profession CD detected.");
                return new Result(ActionResult.Failed);
            }

            //if (_detectedDailyProfessions == null)
            //{
            //    GarrisonButler.Diagnostic("[Profession] DetectedDailyProfessions not initialized.");
            //    return new Tuple<bool, DailyProfession>(false, null);
            //}

            IEnumerable<DailyProfession> possibleDailies =
                _detectedDailyProfessions
                    .Where(d => d.CanCast() && d.GetMaxRepeat() > 0).OrderBy(d => d.TradeskillId);

            if (possibleDailies.Any())
            {
                var daily = possibleDailies.First();
                GarrisonButler.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1} - #{2}",
                    daily.TradeskillId, daily.Spell.Name, daily.GetMaxRepeat());
                return new Result(ActionResult.Running, daily);
            }
            GarrisonButler.Diagnostic("[Profession] No possible daily CD found.");
            return new Result(ActionResult.Failed);
        }

        public static async Task<Result> DoDailyCd(object obj)
        {
            var daily = obj as DailyProfession;
            if (daily == null)
                return new Result(ActionResult.Failed);

            if (daily.NeedAnvil())
            {
                return await FindAnvilAndDoCd(daily);
            }
            if (await DoCd(daily))
                return new Result(ActionResult.Running);

            _dailiesTriggered = false;
            return new Result(ActionResult.Refresh);
        }

        private static async Task<Result> FindAnvilAndDoCd(DailyProfession daily)
        {
            var anvil =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => o.SpellFocus == WoWSpellFocus.Anvil)
                    .OrderBy(o => o.Location.DistanceSqr(Dijkstra.ClosestToNodes(o.Location)))
                    // The closest to a known waypoint
                    .FirstOrDefault();
            if (anvil == null)
            {
                GarrisonButler.Diagnostic("Can't find an Anvil around, moving inside Garrison.");
                return new Result(await MoveToMine());
            }
            GarrisonButler.Log("[Profession] Current CD requires an anvil, moving to the safest one.");
            if ((await MoveToInteract(anvil)).Status == ActionResult.Running)
                return new Result(ActionResult.Running);

            if (await DoCd(daily))
                return new Result(ActionResult.Running);

            return new Result(ActionResult.Refresh);
        }

        public static async Task<Result> MoveToMine()
        {
            var locationToLookAt = Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
            return await MoveTo(locationToLookAt, "[Profession] Moving to mine to search for an Anvil.");
        }

        private static async Task<bool> DoCd(DailyProfession daily)
        {
            if (GarrisonButler.IsIceVersion())
            {
                await daily.PreCraftOperations();
            }
            GarrisonButler.Log("[Profession] Realizing daily CD: " + daily.Spell.Name);
            await HbApi.CastSpell(daily.Spell);
            return false;
        }
    }
}