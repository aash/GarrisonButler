#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.GatherBuddy;
using Bots.Professionbuddy.Components;
using Bots.Professionbuddy.Dynamic;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using GarrisonLua;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static Tuple<bool, MailItem> CanMailItem()
        {
            var toMail = GaBSettings.Get().MailItems.Where(m => Me.BagItems.Any(i => i.Entry == m.ItemId));
            if (!toMail.Any())
            {
                GarrisonButler.Diagnostic("[Mailing] No items to mail.");
                return new Tuple<bool, MailItem>(false, null);
            }
                return new Tuple<bool, MailItem>(true, toMail.First());
        }

        public static async Task<bool> MailItem(MailItem mailItem)
        {
            GarrisonButler.Diagnostic("[Mailing] Running ...");

            if (!Styx.CommonBot.Frames.MailFrame.Instance.IsVisible)
            {
                List<WoWGameObject> MailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

                var mailbox = MailboxList.FirstOrDefault();
                if (mailbox == default(WoWGameObject))
                {
                    var mailboxLocation = Me.IsAlliance ? allyMailbox : hordeMailbox;
                    return await MoveTo(mailboxLocation);
                }

                if (Me.Location.Distance(mailbox.Location) > mailbox.InteractRange)
                    if (await MoveToInteract(mailbox))
                        return true;

                mailbox.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }

            var mailFrame = Styx.CommonBot.Frames.MailFrame.Instance;
            foreach (var mail in GaBSettings.Get().MailItems)
            {
                var item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == mail.ItemId);
                if (item == default(WoWItem))
                {
                    GarrisonButler.Diagnostic("Error, Item null: {0}", mail.ItemId);
                    continue;
                }
                mailFrame.SendMail(mail.Recipient, "", "", 0, item);
                await CommonCoroutines.SleepForRandomUiInteractionTime();
                InterfaceLua.ClickSendMail();
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return true;
        }
        
    }
}