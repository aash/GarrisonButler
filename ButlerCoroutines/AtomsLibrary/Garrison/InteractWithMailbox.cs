using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Libraries;
using GarrisonButler.LuaObjects;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    class InteractWithMailbox : Atom
    {
        private static readonly WoWPoint AllyMailbox = new WoWPoint(1927.694, 294.151, 88.96585);
        private static readonly WoWPoint HordeMailbox = new WoWPoint(5580.682, 4570.392, 136.558);

        public override string Name()
        {
            return "[InteractWithMailbox]";
        }
        public InteractWithMailbox()
        {
            var mailboxLocation = StyxWoW.Me.IsAlliance ? AllyMailbox : HordeMailbox;
            GarrisonButler.Diagnostic("[Mailing] Moving to mailbox at default location" + mailboxLocation);
            Dependencies.Add(new MoveToObject(WoWGameObjectType.Mailbox, mailboxLocation, 1.0f));
        }
        /// <summary>
        /// Must have the building?
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return true;
        }

        /// <summary>
        /// Fulfilled when capacitive display frame opened
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            return MailFrame.Instance != null && MailFrame.Instance.IsVisible;
        }
        /// <summary>
        /// The dependency is to be next to the PNJ, so we just have to open the frame
        /// </summary>
        /// <returns></returns>
        public async override Task Action()
        {
            // Search mailbox item
            var mailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                .GetEmptyIfNull()
                .Where(o => o.SubType == WoWGameObjectType.Mailbox && o != default(WoWGameObject)).ToList();

            var mailbox = mailboxList.GetEmptyIfNull().FirstOrDefault();

            if (mailbox != default(WoWGameObject))
            {
                mailbox.Interact();
                await CommonCoroutines.SleepForLagDuration();
                await CommonCoroutines.SleepForRandomUiInteractionTime();
            }
        }
    }
}
