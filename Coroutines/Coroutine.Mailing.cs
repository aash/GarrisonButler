#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using GarrisonLua;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static Tuple<bool, MailItem> CanMailItem()
        {
            IEnumerable<MailItem> toMail =
                GaBSettings.Get().MailItems.Where(m => Me.BagItems.Any(i => i.Entry == m.ItemId));
            if (!toMail.Any())
            {
                GarrisonButler.Diagnostic("[Mailing] No items to mail.");
                return new Tuple<bool, MailItem>(false, null);
            }
            return new Tuple<bool, MailItem>(true, toMail.First());
        }

        public static async Task<bool> MailItem(MailItem mailItem)
        {
            if (!MailFrame.Instance.IsVisible)
            {
                List<WoWGameObject> MailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

                WoWGameObject mailbox = MailboxList.FirstOrDefault();
                if (mailbox == default(WoWGameObject))
                {
                    WoWPoint mailboxLocation = Me.IsAlliance ? allyMailbox : hordeMailbox;
                    return await MoveTo(mailboxLocation, "[Mailing] Moving to mailbox at " + mailboxLocation);
                }

                if (Me.Location.Distance(mailbox.Location) > mailbox.InteractRange)
                    if (await MoveToInteract(mailbox))
                        return true;

                mailbox.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }

            MailFrame mailFrame = MailFrame.Instance;
            foreach (MailItem mail in GaBSettings.Get().MailItems)
            {
                WoWItem item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == mail.ItemId);
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