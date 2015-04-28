using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    class UseGarrisonHearthstoneIfNeeded : UseItem
    {
        private const uint GarrisonHearthstone = 110560;

        public UseGarrisonHearthstoneIfNeeded()
            : base(GarrisonHearthstone, () => StyxWoW.Me.IsInGarrison())
        {}

        public async override Task Action()
        {
            await base.Action();
            await Coroutine.Wait(15000, () => !StyxWoW.Me.IsCasting);
            await Coroutine.Wait(60000, () => !StyxWoW.IsInGame);
        }

        public override string Name()
        {
            return "[UseGarrisonHearthstone]";
        }
    }
}
