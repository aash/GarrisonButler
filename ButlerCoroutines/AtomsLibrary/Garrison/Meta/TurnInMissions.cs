using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class TurnInMissions : Atom
    {
        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        private static readonly List<uint> CommandTables = new List<uint>
        {
            81661,
            82507,
            82601,
            81546,
            80432,
            84224,
            86062,
            86031,
            84698,
            82600,
            81649,
            85805
        };

        public static WoWObject GetCommandTableOrDefault()
        {
            return
                ObjectManager.ObjectList.GetEmptyIfNull()
                    .FirstOrDefault(o => CommandTables.GetEmptyIfNull().Contains(o.Entry));
        }
        public TurnInMissions()
        {
            ShouldRepeat = true;
            var tableForLoc = MissionLua.GetCommandTableOrDefault();
            Dependencies.Add(new MoveToObject(CommandTables, WoWObjectTypeFlag.Unit, StyxWoW.Me.IsAlliance ? TableAlliance : TableHorde));
        }
        public override bool RequirementsMet()
        {
            return GaBSettings.Get().CompletedMissions;
        }

        public override bool IsFulfilled()
        {
            return MissionLua.GetNumberCompletedMissions() == 0;
        }

        public async override Task Action()
        {
            GarrisonButler.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");

            var table = GetCommandTableOrDefault();
            if (table == default(WoWObject))
                return;

            table.Interact();
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            MissionLua.TurnInAllCompletedMissions();
            await CommonCoroutines.SleepForRandomUiInteractionTime();

            // Restore UI
            Lua.DoString("GarrisonMissionFrame.MissionTab.MissionList.CompleteDialog:Hide();" +
                         "GarrisonMissionFrame.MissionComplete:Hide();" +
                         "GarrisonMissionFrame.MissionCompleteBackground:Hide();" +
                         "GarrisonMissionFrame.MissionComplete.currentIndex = nil;" +
                         "GarrisonMissionFrame.MissionTab:Show();" +
                         "GarrisonMissionList_UpdateMissions();");

            ButlerCoroutine.RefreshMissions(true);
        }

        public override string Name()
        {
            return "[TurnInMissions]";
        }
    }
}
