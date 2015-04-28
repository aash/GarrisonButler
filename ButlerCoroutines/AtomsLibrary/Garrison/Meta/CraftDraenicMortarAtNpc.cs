using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class CraftDraenicMortarAtNpc : Atom
    {
        private WoWUnit _npc;
        public CraftDraenicMortarAtNpc()
        {
            var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().GetEmptyIfNull()
                       .FirstOrDefault(u => u.Entry == (StyxWoW.Me.IsAlliance ? 77372 : 79829));
            if (unit == null)
            {
                Status = new Result(ActionResult.Failed, "[CraftDraenicMortarAtNpc] Unit not found.");
                return;
            }

            _npc = unit;
            Dependencies = new List<Atom>()
            {
                new MoveToInteract(_npc)
            };
        }
        public override bool RequirementsMet()
        {
            var oreInBags = HbApi.GetNumberItemInBags(109118);
            var oreInBank = HbApi.GetNumberItemInReagentBank(109118);
            if (oreInBags + oreInBank < 5)
            {
                GarrisonButler.Diagnostic("[CraftDraenicMortarAtNpc] Not enough Blackrock ore to craft a mortar. InBags={0}, InReagentBank={1}.",
                    oreInBags, oreInBank);
                return false;
            }
            return true;
        }

        public override bool IsFulfilled()
        {
            return HbApi.GetNumberItemInBags(114942) > 0;
        }

        public async override Task Action()
        {
            if (!(ButlerLua.IsTradeSkillFrameOpenN()))
            {
                _npc.Interact();
                GarrisonButler.Diagnostic("[MillBeforeOrder] TradeSkillFrame not open.");
                return;
            }

            if (!await ButlerLua.CraftDraenicMortar())
            {
                GarrisonButler.Diagnostic("[MillBeforeOrder] CraftDraenicMortar error in lua craft.");
                return;
            }

            // wait for cast
            await Coroutine.Wait(10000, () =>
            {
                ObjectManager.Update();
                return HbApi.GetNumberItemInBags(114942) > 0;
            });
            await CommonCoroutines.SleepForLagDuration();
        }

        public override string Name()
        {
            return "[CraftDraenicMortarAtNpc]";
        }
    }
}
