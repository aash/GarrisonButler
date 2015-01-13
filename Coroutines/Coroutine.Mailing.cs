#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static bool _checkedMailbox;
        private static int _numMailsOnLastCheck;

        private static readonly Stopwatch MailboxCheckTimer = new Stopwatch();
            // Only check mail every 60s if no items exist in mail frame

        private const int CheckIntervalWhileStillWaiting = 65;
        private static int _mailCheckInterval = CheckIntervalWhileStillWaiting; // in seconds
        private static bool _allowMailTimerStart = true;

        private static Tuple<bool, int> HasMails()
        {
            if (!GaBSettings.Get().RetrieveMail)
            {
                GarrisonButler.Diagnostic("[Mail] Checking mail deactivated in user settings.");
                return new Tuple<bool, int>(false, 0);
            }

            // Only check mail every 5 minutes by default
            // If check interval is 65s, it will return true for HasMails()
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval != CheckIntervalWhileStillWaiting))
            {
                // Timer has not finished yet, indicate "no mail"
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    return new Tuple<bool, int>(false, 0);
                }
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
            }

            if (!_checkedMailbox) return new Tuple<bool, int>(true, 0);

            return ApiLua.HasNewMail() ? new Tuple<bool, int>(true, 0) : new Tuple<bool, int>(false, 0);
        }

        private static async Task<ActionResult> MoveAndInteractWithMailbox()
        {
            var mailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                .GetEmptyIfNull()
                .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

            var mailbox = mailboxList.GetEmptyIfNull().FirstOrDefault();
            if (mailbox == default(WoWGameObject))
            {
                var mailboxLocation = Me.IsAlliance ? AllyMailbox : HordeMailbox;
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
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval == CheckIntervalWhileStillWaiting))
            {
                // Need to keep waiting for timer
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    return ActionResult.Running;
                }
                GarrisonButler.Diagnostic("[Mail] " + _mailCheckInterval + "s mailbox timer finished inside GetMails()");
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
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

            var mailFrame = MailFrame.Instance;

            var numMail = InterfaceLua.GetInboxMailCountInPlayerInbox();
            var totalMail = InterfaceLua.GetInboxMailCountOnServer();

            GarrisonButler.Diagnostic("[Mail] LUA returned VisibleMail=" + numMail);
            GarrisonButler.Diagnostic("[Mail] LUA returned server TotalMail=" + totalMail);

            await Buddy.Coroutines.Coroutine.Sleep(1000);

            //WorkAroundHBMailLoggingBugStart();
            var hbMailLoggingBugOriginalLogLevel = GarrisonButler.CurrentHonorbuddyLog.LoggingLevel;
            var hbMailLoggingBugOriginalFileLogLevel = GarrisonButler.CurrentHonorbuddyLog.LogFileLevel;
            var hbMailLoggingBugOriginalFileLoggingFlag = GarrisonButler.CurrentHonorbuddyLog.FileLogging;

            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = LogLevel.None;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = false;

            var openAllMailCoroutineResult = await mailFrame.OpenAllMailCoroutine();

            _checkedMailbox = true;

            GarrisonButler.CurrentHonorbuddyLog.LoggingLevel = hbMailLoggingBugOriginalLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.LogFileLevel = hbMailLoggingBugOriginalFileLogLevel;
            GarrisonButler.CurrentHonorbuddyLog.FileLogging = hbMailLoggingBugOriginalFileLoggingFlag;
            //WorkAroundHBMailLoggingBugEnd();

            // Wait for logging changes to take effect / mail icon
            await Buddy.Coroutines.Coroutine.Sleep(1000);
            GarrisonButler.Diagnostic("[Mail] OpenAllMailCoroutine() returned " + openAllMailCoroutineResult);

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
            GarrisonButler.Diagnostic("[Mail] numMailsOnLastCheck=" + _numMailsOnLastCheck);
            var condition1 = (numMail >= 50) && (_numMailsOnLastCheck != totalMail);
            GarrisonButler.Diagnostic("[Mail] condition1=" + condition1);

            // Allow for a 2nd check when fresh bot run or after a 5min timer refresh
            var condition2 = (numMail >= 50) && (_allowMailTimerStart);
            GarrisonButler.Diagnostic("[Mail] condition2=" + condition2);

            if (condition1 || condition2)
            {
                GarrisonButler.Log("[Mail] More mail to check, waiting 65 seconds");
                _allowMailTimerStart = false;
                _numMailsOnLastCheck = totalMail;
                if (mailFrame.IsVisible)
                {
                    mailFrame.Close();

                    // Wait for mail icon to update after closing mail frame
                    await Buddy.Coroutines.Coroutine.Sleep(1000);
                }
                _mailCheckInterval = 65;
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Start();
                return ActionResult.Running;
            }
            GarrisonButler.Log("[Mail] Waiting 5 minutes to check mail again.");
            _numMailsOnLastCheck = 0;
            _allowMailTimerStart = true;
            if (mailFrame.IsVisible)
            {
                mailFrame.Close();

                // Wait for mail icon to update after closing mail frame
                await Buddy.Coroutines.Coroutine.Sleep(1000);
            }
            _mailCheckInterval = 60*5; // 5 minutes
            MailboxCheckTimer.Reset();
            MailboxCheckTimer.Start();
            return ActionResult.Done;
        }

        private static Tuple<bool, List<MailItem>> CanMailItem()
        {
            if (!GaBSettings.Get().SendMail)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending mail deactivated in user settings.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }

            var toMail =
                GaBSettings.Get().MailItems.GetEmptyIfNull().Where(m => m.CanMail()).ToList();

            if (toMail.GetEmptyIfNull().Any()) return new Tuple<bool, List<MailItem>>(true, toMail);
            GarrisonButler.Diagnostic("[Mailing] No items to mail.");
            return new Tuple<bool, List<MailItem>>(false, null);
        }

        public static async Task<ActionResult> MailItem(List<MailItem> mailItems)
        {
            var mailFrame = MailFrame.Instance;

            if (!mailFrame.IsVisible)
            {
                var mailboxList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => o.SubType == WoWGameObjectType.Mailbox).ToList();

                var mailbox = mailboxList.GetEmptyIfNull().FirstOrDefault();
                if (mailbox == default(WoWGameObject))
                {
                    var mailboxLocation = Me.IsAlliance ? AllyMailbox : HordeMailbox;
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
                .Select(x => x.Select(i => i))
                .ToList();
            foreach (var mailsRecipient in mailsPerRecipient)
            {
                var items = mailsRecipient.ToList();
                // list all items to send to this recipient
                var listItems = new List<WoWItem>();
                foreach (var mail in items)
                {
                    listItems.AddRange(await mail.GetItemsToSend());
                }
                // send if any to send
                if (listItems.GetEmptyIfNull().Any())
                {
                    if (items.GetEmptyIfNull().FirstOrDefault() == default(MailItem))
                        return ActionResult.Done;

                    var firstOrDefault = items.GetEmptyIfNull().FirstOrDefault();
                    if (firstOrDefault != null)
                    {
                        await
                            mailFrame.SendMailWithManyAttachmentsCoroutine(firstOrDefault.Recipient.ToString(),
                                listItems.GetEmptyIfNull().ToArray());
                        await CommonCoroutines.SleepForRandomUiInteractionTime();
                        InterfaceLua.ClickSendMail();
                    }
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return ActionResult.Done;
        }
    }
}