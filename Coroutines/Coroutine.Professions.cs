using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonBuddy.Config;
using GarrisonBuddy.Objects;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static bool lastCheckCd = false;
        private static List<Helpers.TradeskillHelper> tradeskillHelpers;

        private static List<Tuple<int,DailyProfession.tradeskillID,WoWSpell>> DailyProfessionCD;


        private static List<DailyProfession> _detectedDailyProfessions; 
       
           
        private static void InitializeDailies()
        {
            if (_detectedDailyProfessions != null) return;
            
            GarrisonBuddy.Log("Loading Professions dailies, please wait.");
            _detectedDailyProfessions = new List<DailyProfession>();

            foreach (var daily in DailyProfession.AllDailies)
            {
                daily.Initialize();
                if (daily.Spell != null)
                {
                    GarrisonBuddy.Log("Adding daily CD: {0} - {1}", daily.TradeskillId, daily.Spell.Name);
                    _detectedDailyProfessions.Add(daily);
                }
            }

            GarrisonBuddy.Log("Loading Professions dailies done.");
        }

        private static bool ShouldRunDailies()
        {
            return CanRunDailies().Item1;
        }

        private static Tuple<bool,DailyProfession> CanRunDailies()
        {
            // Check
            InitializeDailies();

            if (!_detectedDailyProfessions.Any())
            {
                GarrisonBuddy.Diagnostic("[Profession] No daily profession CD detected and/or activated."); 
                return new Tuple<bool, DailyProfession>(false, null);
            }

            if (_detectedDailyProfessions == null)
            {
                GarrisonBuddy.Diagnostic("[Profession] DetectedDailyProfessions not initialized.");
                return new Tuple<bool, DailyProfession>(false,null);
            }

            foreach (DailyProfession daily in _detectedDailyProfessions) //Tuple<int, tradeskillID, WoWSpell> spellProfession
            {
                //ADD CHECK WITH OPTIONS, can use itemID
                if (Math.Abs(daily.Spell.CooldownTimeLeft.TotalSeconds) < 0.1)
                {
                    if (daily.GetMaxRepeat() <= 0)
                        continue;

                    GarrisonBuddy.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1} - #{2}", daily.TradeskillId, daily.Spell.Name, daily.GetMaxRepeat());
                    return new Tuple<bool, DailyProfession>(true, daily);
                }
            }
            GarrisonBuddy.Diagnostic("[Profession] No possible daily CD found.");
            return new Tuple<bool, DailyProfession>(false, null);
        }

        public static async Task<bool> DoDailyCd(DailyProfession daily)
        {

            if (daily.needAnvil())
            {
                if (await FindAnvilAndDoCd(daily))
                    return true;
            }
            else
            {
                if (await DoCd(daily))
                    return true;
            }

            DailiesTriggered = false;
            return true;
        }

        private static async Task<bool> FindAnvilAndDoCd(DailyProfession daily)
        {
            WoWGameObject anvil =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => o.SpellFocus == WoWSpellFocus.Anvil)
                    .OrderBy(o => o.Location.DistanceSqr(Dijkstra.ClosestToNodes(o.Location))) // The closest to a known waypoint
                    .FirstOrDefault();
            if (anvil == null)
            {
                GarrisonBuddy.Warning("Can't find an Anvil around, skipping for now.");
            }
            else
            {
                GarrisonBuddy.Warning("[Profession] Current CD requires an anvil, moving to the safest one.");
                if (await MoveTo(anvil.Location))
                    return true;

                if (await DoCd(daily))
                    return true;
            }
            return false;
        }

        private static async Task<bool> DoCd(DailyProfession daily)
        {
            GarrisonBuddy.Log("[Profession] Realizing daily CD: " + daily.Spell.Name);
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