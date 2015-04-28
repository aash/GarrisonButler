#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using Styx;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    internal class CraftAllDailies : Atom
    {
        private static List<DailyProfession> _detectedDailyProfessions;

        public CraftAllDailies()
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


        public override bool RequirementsMet()
        {
            return true;
        }

        public override bool IsFulfilled()
        {
            if (!_detectedDailyProfessions.Any())
            {
                GarrisonButler.Diagnostic("[Profession] No daily profession CD detected.");
                return true;
            }

            IEnumerable<DailyProfession> possibleDailies =
                _detectedDailyProfessions
                    .Where(d => d.CanCast() && d.GetMaxRepeat() > 0).OrderBy(d => d.TradeskillId);

            if (possibleDailies.Any())
            {
                var daily = possibleDailies.First();
                GarrisonButler.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1} - #{2}",
                    daily.TradeskillId, daily.Spell.Name, daily.GetMaxRepeat());
                return false;
            }
            GarrisonButler.Diagnostic("[Profession] No possible daily CD found.");
            return true;
        }

        private DailyProfession _currentDaily;
        private CraftItem _currentAction;

        public override async Task Action()
        {
            IEnumerable<DailyProfession> possibleDailies =
                _detectedDailyProfessions
                    .Where(d => d.CanCast() && d.GetMaxRepeat() > 0).OrderBy(d => d.TradeskillId);

            DailyProfession daily = null;
            if (possibleDailies.Any())
            {
                daily = possibleDailies.First();
                GarrisonButler.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1} - #{2}",
                    daily.TradeskillId, daily.Spell.Name, daily.GetMaxRepeat());
            }


            if (daily == null)
            {
                Status = new Result(ActionResult.Failed, "Daily is null");
                return;
            }

            if (_currentDaily == null || _currentDaily != daily)
            {
                _currentDaily = daily;
                _currentAction = new CraftItem(_currentDaily.ItemId, (SkillLine) _currentDaily.TradeskillId, _currentDaily.SpellId);
            }

            if (_currentAction != null)
            {
                await _currentAction.Execute();
            }
        }

        public override string Name()
        {
            return "[DoAllDailies]";
        }
    }
}