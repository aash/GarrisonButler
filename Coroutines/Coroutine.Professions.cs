#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using Styx.CommonBot.Coroutines;
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
            if(_detectedDailyProfessions == null)
                _detectedDailyProfessions = new List<DailyProfession>();

            IEnumerable<DailyProfession> dailyActivated =
                DailyProfession.AllDailies.Where(
                    d => GaBSettings.Get().DailySettings.FirstOrDefault(d2 => d2.ItemId == d.ItemId).Activated);

            //GarrisonButler.Diagnostic("[Profession] AllDailies:");
            //ObjectDumper.WriteToHB(DailyProfession.AllDailies, 1); 
            //GarrisonButler.Diagnostic("[Profession] _detectedDailyProfessions:");
            //ObjectDumper.WriteToHB(_detectedDailyProfessions, 1);

            if (!dailyActivated.Any())
            {
                GarrisonButler.Diagnostic("[Profession] No daily activated in settings.");
                return;
            }

            foreach (DailyProfession daily in dailyActivated)
            {
                if (!_detectedDailyProfessions.Any(d => d.ItemId == daily.ItemId && d.Spell == null))
                {
                    daily.Initialize();
                    if (daily.Spell != null)
                    {
                        GarrisonButler.Log("Adding daily CD: {0} - {1}", daily.TradeskillId, daily.Spell.Name);
                        _detectedDailyProfessions.Add(daily);
                    }
                }
            }
        }

        private static bool ShouldRunDailies()
        {
            return CanRunDailies().Item1;
        }

        private static Tuple<bool, DailyProfession> CanRunDailies()
        {
            // Check
            InitializeDailies();

            if (!_detectedDailyProfessions.Any())
            {
                GarrisonButler.Diagnostic("[Profession] No daily profession CD detected.");
                return new Tuple<bool, DailyProfession>(false, null);
            }

            //if (_detectedDailyProfessions == null)
            //{
            //    GarrisonButler.Diagnostic("[Profession] DetectedDailyProfessions not initialized.");
            //    return new Tuple<bool, DailyProfession>(false, null);
            //}

            IEnumerable<DailyProfession> possibleDailies =
                _detectedDailyProfessions.Where(d => Math.Abs(d.Spell.CooldownTimeLeft.TotalSeconds) < 0.1)
                    .Where(d => d.GetMaxRepeat() > 0).OrderBy(d => d.TradeskillId);

            if (possibleDailies.Any())
            {
                DailyProfession daily = possibleDailies.First();
                GarrisonButler.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1} - #{2}",
                    daily.TradeskillId, daily.Spell.Name, daily.GetMaxRepeat());
                return new Tuple<bool, DailyProfession>(true, daily);
            }
            GarrisonButler.Diagnostic("[Profession] No possible daily CD found.");
            return new Tuple<bool, DailyProfession>(false, null);
        }

        public static async Task<bool> DoDailyCd(DailyProfession daily)
        {
            if (daily.needAnvil())
            {
                return await FindAnvilAndDoCd(daily);
            }
            if (await DoCd(daily))
                return true;

            DailiesTriggered = false;
            return true;
        }

        private static async Task<bool> FindAnvilAndDoCd(DailyProfession daily)
        {
            WoWGameObject anvil =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => o.SpellFocus == WoWSpellFocus.Anvil)
                    .OrderBy(o => o.Location.DistanceSqr(Dijkstra.ClosestToNodes(o.Location)))
                    // The closest to a known waypoint
                    .FirstOrDefault();
            if (anvil == null)
            {
                GarrisonButler.Diagnostic("Can't find an Anvil around, skipping for now.");
            }
            else
            {
                GarrisonButler.Log("[Profession] Current CD requires an anvil, moving to the safest one.");
                if (await MoveTo(anvil.Location))
                    return true;

                if (await DoCd(daily))
                    return true;
            }
            return false;
        }

        private static async Task<bool> DoCd(DailyProfession daily)
        {
            GarrisonButler.Log("[Profession] Realizing daily CD: " + daily.Spell.Name);
            if (Me.IsMoving)
                WoWMovement.MoveStop();
            if (Me.Mounted)
                await CommonCoroutines.LandAndDismount();
            await CommonCoroutines.SleepForLagDuration();
            daily.Spell.Cast();
            await CommonCoroutines.SleepForLagDuration();
            await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);
            return true;
        }
    }
}