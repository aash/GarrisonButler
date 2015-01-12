#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Objects;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Common;
using Styx.Common.Helpers;
using System.Diagnostics;
using System.Timers;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static bool checkedMailbox = false;
        private static int numMailsOnLastCheck = 0;
        private static Stopwatch mailboxCheckTimer = new Stopwatch();   // Only check mail every 60s if no items exist in mail frame
        private static int mailCheckInterval = checkIntervalWhileStillWaiting; // in seconds
        private static int checkIntervalWhileStillWaiting = 65;
        private static bool allowMailTimerStart = true;
        private static Tuple<bool, int> HasMails()
        {
            if (!GaBSettings.Get().RetrieveMail)
            {
                GarrisonButler.Diagnostic("[Mail] Checking mail deactivated in user settings.");
                return new Tuple<bool, int>(false, 0);
            }

            // Only check mail every 5 minutes by default
            // If check interval is 65s, it will return true for HasMails()
            if ((mailboxCheckTimer.IsRunning) && (mailCheckInterval != checkIntervalWhileStillWaiting))
            {
                // Timer has not finished yet, indicate "no mail"
                if (mailboxCheckTimer.Elapsed.TotalSeconds < (mailCheckInterval))
                {
                    return new Tuple<bool, int>(false, 0);
                }
                else
                {
                    mailboxCheckTimer.Reset();
                    mailboxCheckTimer.Stop();
                }
            }

            if (!checkedMailbox) return new Tuple<bool, int>(true, 0);

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

        private async static Task<ActionResult> MoveAndInteractWithMailbox()
        {
            List<WoWGameObject> MailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

            WoWGameObject mailbox = MailboxList.GetEmptyIfNull().FirstOrDefault();
            if (mailbox == default(WoWGameObject))
            {
                WoWPoint mailboxLocation = Me.IsAlliance ? AllyMailbox : HordeMailbox;
                return await MoveTo(mailboxLocation, "[Mailing] Moving to mailbox at " + mailboxLocation);
            }

            if (Me.Location.Distance(mailbox.Location) > mailbox.InteractRange)
                if (await MoveToInteract(mailbox) == ActionResult.Running)
                    return ActionResult.Running;

            mailbox.Interact();
            await CommonCoroutines.SleepForLagDuration();
            return ActionResult.Running;
        }

        //static LogLevel HBMailLoggingBugOriginalLogLevel;
        //static LogLevel HBMailLoggingBugOriginalFileLogLevel;
        //static bool HBMailLoggingBugOriginalFileLoggingFlag;
        //private static void WorkAroundHBMailLoggingBugStart()
        //{
        //    HBMailLoggingBugOriginalLogLevel = GarrisonButler.CurrentHonorbuddyLog.LoggingLevel;
        //    HBMailLoggingBugOriginalFileLogLevel = GarrisonButler.CurrentHonorbuddyLog.LogFileLevel;
        //    HBMailLoggingBugOriginalFileLoggingFlag = GarrisonButler.CurrentHonorbuddyLog.FileLogging;

        //    GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = LogLevel.None;
        //    GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = LogLevel.None;
        //    GarrisonButler.CurrentHonorbuddyLog.FileLogging = false;
        //}

        //private static void WorkAroundHBMailLoggingBugEnd()
        //{
        //    GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = HBMailLoggingBugOriginalLogLevel;
        //    GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = HBMailLoggingBugOriginalFileLogLevel;
        //    GarrisonButler.CurrentHonorbuddyLog.FileLogging = HBMailLoggingBugOriginalFileLoggingFlag;
        //}

        public static async Task<ActionResult> GetMails(int osef)
        {
            // If check interval is 65s, we must wait for the wow server to allow another refresh
            if ((mailboxCheckTimer.IsRunning) && (mailCheckInterval == checkIntervalWhileStillWaiting))
            {
                // Need to keep waiting for timer
                if (mailboxCheckTimer.Elapsed.TotalSeconds < (mailCheckInterval))
                {
                    return ActionResult.Running;
                }
                else
                {
                    GarrisonButler.Diagnostic("[Mail] " + mailCheckInterval + "s mailbox timer finished inside GetMails()");
                    mailboxCheckTimer.Reset();
                    mailboxCheckTimer.Stop();
                }
            }

            // Get to the mailbox
            if (!MailFrame.Instance.IsVisible)
            {
                if (await MoveAndInteractWithMailbox() == ActionResult.Running)
                    return ActionResult.Running;
            }

            // Wait for server to load mails after opening mailbox
            GarrisonButler.Diagnostic("[Mail] Waiting 5s for WoW to load mail.");
            await Buddy.Coroutines.Coroutine.Sleep(5000);

            MailFrame mailFrame = MailFrame.Instance;

            int numMail = InterfaceLua.GetInboxMailCountInPlayerInbox();
            int totalMail = InterfaceLua.GetInboxMailCountOnServer();

            GarrisonButler.Diagnostic("[Mail] LUA returned VisibleMail=" + numMail);
            GarrisonButler.Diagnostic("[Mail] LUA returned server TotalMail=" + totalMail);

            //WorkAroundHBMailLoggingBugStart();
            LogLevel HBMailLoggingBugOriginalLogLevel;
            LogLevel HBMailLoggingBugOriginalFileLogLevel;
            bool HBMailLoggingBugOriginalFileLoggingFlag;
            HBMailLoggingBugOriginalLogLevel = GarrisonButler.CurrentHonorbuddyLog.LoggingLevel;
            HBMailLoggingBugOriginalFileLogLevel = GarrisonButler.CurrentHonorbuddyLog.LogFileLevel;
            HBMailLoggingBugOriginalFileLoggingFlag = GarrisonButler.CurrentHonorbuddyLog.FileLogging;

            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = false;

            bool openAllMailCoroutineResult = await mailFrame.OpenAllMailCoroutine();

            checkedMailbox = true;

            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = HBMailLoggingBugOriginalLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = HBMailLoggingBugOriginalFileLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = HBMailLoggingBugOriginalFileLoggingFlag;
            //WorkAroundHBMailLoggingBugEnd();

            // Wait for logging changes to take effect / mail icon
            await Buddy.Coroutines.Coroutine.Sleep(1000);
            GarrisonButler.Diagnostic("[Mail] OpenAllMailCoroutine() returned " + openAllMailCoroutineResult.ToString());

            // "Read" all mails, even ones with only text in them
            // This turns the mail "grey" to get rid of the mail icon
            // OpenAllMailCoroutine() from the Honorbuddy base does NOT turn the mail
            // to "grey" when checking mail with ONLY text in it.
            mailFrame.GetAllMails().GetEmptyIfNull().ForEach(m => InterfaceLua.MarkMailAsRead(m.Index));

            // Allow for a 2nd check if the server returned more mails than were shown in the inbox
            // AND
            // We reduced the number of total mail (on the server) from the last check
            // If total mail didn't change between checks, that means the user's inbox is
            // stuck possibly due to "read" messages that contain only text or their inventory
            // is full
            GarrisonButler.Diagnostic("[Mail] numMailsOnLastCheck=" + numMailsOnLastCheck);
            bool condition1 = (numMail >= 50) && (numMailsOnLastCheck != totalMail);
            GarrisonButler.Diagnostic("[Mail] condition1=" + condition1.ToString());

            // Allow for a 2nd check when fresh bot run or after a 5min timer refresh
            bool condition2 = (numMail >= 50) && (allowMailTimerStart);
            GarrisonButler.Diagnostic("[Mail] condition2=" + condition2.ToString());

            if(condition1 || condition2)
            {
                GarrisonButler.Log("[Mail] More mail to check, waiting 65 seconds");
                allowMailTimerStart = false;
                numMailsOnLastCheck = totalMail;
                if (mailFrame.IsVisible)
                {
                    mailFrame.Close();

                    // Wait for mail icon to update after closing mail frame
                    await Buddy.Coroutines.Coroutine.Sleep(1000);
                }
                mailCheckInterval = 65;
                mailboxCheckTimer.Reset();
                mailboxCheckTimer.Start();
                return ActionResult.Running;
            }
            else
            {
                GarrisonButler.Log("[Mail] Waiting 5 minutes to check mail again.");
                numMailsOnLastCheck = 0;
                allowMailTimerStart = true;
                if (mailFrame.IsVisible)
                {
                    mailFrame.Close();

                    // Wait for mail icon to update after closing mail frame
                    await Buddy.Coroutines.Coroutine.Sleep(1000);
                }
                mailCheckInterval = 60 * 5; // 5 minutes
                mailboxCheckTimer.Reset();
                mailboxCheckTimer.Start();
                return ActionResult.Done;
            }
        }

        private static Tuple<bool, List<MailItem>> CanMailItem()
        {
            if (!GaBSettings.Get().SendMail)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending mail deactivated in user settings.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }

            List<MailItem> toMail =
                GaBSettings.Get().MailItems.GetEmptyIfNull().Where(m => m.CanMail()).ToList();

            if (!toMail.GetEmptyIfNull().Any())
            {
                GarrisonButler.Diagnostic("[Mailing] No items to mail.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }
            return new Tuple<bool, List<MailItem>>(true, toMail);
        }

        public static async Task<ActionResult> MailItem(List<MailItem> mailItems)
        {
            MailFrame mailFrame = MailFrame.Instance;

            if (!mailFrame.IsVisible)
            {
                List<WoWGameObject> MailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

                WoWGameObject mailbox = MailboxList.GetEmptyIfNull().FirstOrDefault();
                if (mailbox == default(WoWGameObject))
                {
                    WoWPoint mailboxLocation = Me.IsAlliance ? AllyMailbox : HordeMailbox;
                    return await MoveTo(mailboxLocation, "[Mailing] Moving to mailbox at " + mailboxLocation);
                }

                if (Me.Location.Distance(mailbox.Location) > mailbox.InteractRange)
                    if (await MoveToInteract(mailbox) == ActionResult.Running)
                        return ActionResult.Running;

                mailbox.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }

            

            //Splitting list based on recipients
            var mailsPerRecipient = mailItems
                .GetEmptyIfNull()
                .GroupBy(i => i.Recipient.Value)
                .Select(x=> x.Select(i=> i))
                .ToList();
            foreach (var mailsRecipient in mailsPerRecipient)
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
                        return ActionResult.Done;

                    await mailFrame.SendMailWithManyAttachmentsCoroutine(mailsRecipient
                        .GetEmptyIfNull()
                        .FirstOrDefault()
                        .Recipient.ToString(), listItems.GetEmptyIfNull().ToArray());
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    InterfaceLua.ClickSendMail();
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return ActionResult.Done;
        }
    }
}