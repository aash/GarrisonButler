#region

using System.Threading.Tasks;
using Bots.Grind;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    internal class Combat
    {
        private static Composite _combatBehavior;

        private static Composite CombatBehavior
        {
            get { return _combatBehavior ?? (_combatBehavior = LevelBot.CreateCombatBehavior()); }
        }

        public static async Task<bool> CombatRoutine()
        {
            if (!StyxWoW.Me.IsFlying && StyxWoW.Me.IsActuallyInCombat)
            {
                if (await CombatBehavior.ExecuteCoroutine())
                    return true;
            }

            if (BotPoi.Current.Type == PoiType.Kill)
            {
                var unit = BotPoi.Current.AsObject as WoWUnit;
                if (unit == null)
                    BotPoi.Clear("Current target is null.");
                else if (unit.IsDead)
                    BotPoi.Clear("Current target is dead");
            }

            return false;
        }
    }
}