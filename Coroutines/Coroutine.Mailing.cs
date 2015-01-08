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
        private static Tuple<bool, List<MailItem>> CanMailItem()
        {
            List<MailItem> toMail =
                GaBSettings.Get().MailItems.Where(m => m.CanMail()).ToList();

            if (!toMail.Any())
            {
                GarrisonButler.Diagnostic("[Mailing] No items to mail.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }
            return new Tuple<bool, List<MailItem>>(true, toMail);
        }

        public static async Task<bool> MailItem(List<MailItem> mailItems)
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

            //Splitting list based on recipients
            var MailsPerRecipient = mailItems.GroupBy(i => i.Recipient).Select(x=> x.Select(i=> i)).ToList();
            foreach (var mailsRecipient in MailsPerRecipient)
            {
                // list all items to send to this recipient
                var listItems = new List<WoWItem>();
                foreach (MailItem mail in mailsRecipient)
                {
                    listItems.AddRange(await mail.GetItemsToSend());
                }
                // send if any to send
                if (listItems.Any())
                {
                    await mailFrame.SendMailWithManyAttachmentsCoroutine(mailsRecipient.First().Recipient, listItems.ToArray());
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    InterfaceLua.ClickSendMail();
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return false;
        }
    }
}