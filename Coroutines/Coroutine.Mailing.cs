#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using GarrisonButler.Libraries;
using GarrisonLua;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Common;
using Styx.Common.Helpers;
using System.Diagnostics;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static bool checkedMailbox = false;
        private static int checkCycles = 0;
        private static Stopwatch mailboxCheckTimer = new Stopwatch();   // Only check mail every 60s if no items exist in mail frame
        private static Tuple<bool, int> HasMails()
        {
            // Only check mail every 60s
            if (mailboxCheckTimer.IsRunning)
            {
                if (mailboxCheckTimer.ElapsedMilliseconds < 60000)
                {
                    return new Tuple<bool, int>(false, 0);
                }
                else
                {
                    mailboxCheckTimer.Reset();
                    mailboxCheckTimer.Stop();
                }
            }

            if (API.ApiLua.HasNewMail())
                return new Tuple<bool, int>(true, 0);
            else
                return new Tuple<bool, int>(false, 0);

            //if (!checkedMailbox) return new Tuple<bool, int>(true, 0);            

            //var mailFrame = MailFrame.Instance;
            //if (mailFrame != null)
            //    if (mailFrame.HasNewMail)
            //        return new Tuple<bool, int>(true, 0);

            //return new Tuple<bool, int>(false, 0);
        }

        private async static Task<bool> MoveAndInteractWithMailbox()
        {
            List<WoWGameObject> MailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

            WoWGameObject mailbox = MailboxList.GetEmptyIfNull().FirstOrDefault();
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
            return true;
        }
        public static async Task<bool> GetMails(int osef)
        {
            if (!MailFrame.Instance.IsVisible)
            {
                return await MoveAndInteractWithMailbox();
            }

            // Wait for server to load mails
            await Buddy.Coroutines.Coroutine.Sleep(5000);

            MailFrame mailFrame = MailFrame.Instance;

            LogLevel oldLogLevel = GarrisonButler.CurrentHonorbuddyLog.LoggingLevel;
            LogLevel oldFileLogLevel = GarrisonButler.CurrentHonorbuddyLog.LogFileLevel;
            bool oldFileLoggingFlag = GarrisonButler.CurrentHonorbuddyLog.FileLogging;

            // Attempt to fix writing out character names
            // OpenAllMailCoroutine() will print out debug info
            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = false;

            IEnumerable<MailFrame.InboxMailItem> allMails = mailFrame.GetAllMails();

            bool openAllMailCoroutineResult = await mailFrame.OpenAllMailCoroutine();            

            // Reset setings before OpenAllMailCoroutine()
            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = oldLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = oldFileLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = true;

            // Wait for logging changes to take effect / mail icon
            await Buddy.Coroutines.Coroutine.Sleep(1000);

            //GarrisonButler.Diagnostic("OpenAllMailCoroutine Result = " + openAllMailCoroutineResult.ToString());
            //GarrisonButler.Diagnostic("GetAllMails() returned count=" + mailFrame.GetAllMails().GetEmptyIfNull().Count());

            if (!mailFrame.GetAllMails().GetEmptyIfNull().Any())
            {
                if (mailFrame.IsVisible)
                {
                    InterfaceLua.ClickCloseMailButton();
                    mailFrame.Close();
                }

                checkedMailbox = true;

                // Wait 60s before checking mail again
                if (!mailboxCheckTimer.IsRunning)
                {
                    mailboxCheckTimer.Reset();
                    mailboxCheckTimer.Start();
                }
                return false;
            }
            
            return true;
        }

        private static Tuple<bool, List<MailItem>> CanMailItem()
        {
            List<MailItem> toMail =
                GaBSettings.Get().MailItems.GetEmptyIfNull().Where(m => m.CanMail()).ToList();

            if (!toMail.GetEmptyIfNull().Any())
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
                    .GetEmptyIfNull()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

                WoWGameObject mailbox = MailboxList.GetEmptyIfNull().FirstOrDefault();
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
            var MailsPerRecipient = mailItems
                .GetEmptyIfNull()
                .GroupBy(i => i.Recipient)
                .Select(x=> x.Select(i=> i))
                .ToList();
            foreach (var mailsRecipient in MailsPerRecipient)
            {
                // list all items to send to this recipient
                var listItems = new List<WoWItem>();
                foreach (MailItem mail in mailsRecipient)
                {
                    listItems.AddRange(await mail.GetItemsToSend());
                }
                // send if any to send
                if (listItems.GetEmptyIfNull().Any())
                {
                    if(mailsRecipient.GetEmptyIfNull().FirstOrDefault() == default(MailItem))
                        return false;

                    await mailFrame.SendMailWithManyAttachmentsCoroutine(mailsRecipient
                        .GetEmptyIfNull()
                        .FirstOrDefault()
                        .Recipient, listItems.GetEmptyIfNull().ToArray());
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    InterfaceLua.ClickSendMail();
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return false;
        }
    }
}