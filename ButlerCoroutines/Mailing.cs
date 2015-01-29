#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {

        private const int CheckIntervalWhileStillWaiting = 65;
        private static bool _checkedMailbox;
        private static int _numMailsOnLastCheck;
        private static readonly Stopwatch MailboxCheckTimer = new Stopwatch();
        // Only check mail every 60s if no items exist in mail frame
        private static bool _allowMailTimerStart = true;

        private async static Task<Result> HasMails()
        {
            if (!GaBSettings.Get().RetrieveMail)
            {
                GarrisonButler.Diagnostic("[Mail] Checking mail deactivated in user settings.");
                return new Result(ActionResult.Failed);
            }

            // Only check mail every 5 minutes by default
            // If check interval is 65s, it will return true for HasMails()
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval != CheckIntervalWhileStillWaiting))
            {
                // Timer has not finished yet, indicate "no mail"
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    return new Result(ActionResult.Failed);
                }
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
            }

            if (!_checkedMailbox)
                return new Result(ActionResult.Running);
            
            return ApiLua.HasNewMail() 
                ? new Result(ActionResult.Running) : new Result(ActionResult.Failed);
        }

        private static async Task<Result> MoveAndInteractWithMailbox()
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
                if ((await MoveToInteract(mailbox)).Status == ActionResult.Running)
                    return new Result(ActionResult.Running);

            mailbox.Interact();
            await CommonCoroutines.SleepForLagDuration();
            return new Result(ActionResult.Running);
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

        public static async Task<Result> GetMails(object o)
        {
            // If check interval is 65s, we must wait for the wow server to allow another refresh
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval == CheckIntervalWhileStillWaiting))
            {
                // Need to keep waiting for timer
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    return new Result(ActionResult.Running);
                }
                GarrisonButler.Diagnostic("[Mail] " + _mailCheckInterval + "s mailbox timer finished inside GetMails()");
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
            }

            // Get to the mailbox
            if (!MailFrame.Instance.IsVisible)
            {
                if ((await MoveAndInteractWithMailbox()).Status == ActionResult.Running)
                    return new Result(ActionResult.Running);
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

            bool openAllMailCoroutineResult = false;
            var timeout = new WaitTimer(TimeSpan.FromMilliseconds(90000));

            openAllMailCoroutineResult = await mailFrame.OpenAllMailCoroutine();
            while (mailFrame.GetAllMails().Any(m=> !m.WasRead || ((m.ItemCount > 0 || m.Copper > 0) && m.CODAmount <= 0)) && !timeout.IsFinished)
            {
                openAllMailCoroutineResult = await mailFrame.OpenAllMailCoroutine();
                await Buddy.Coroutines.Coroutine.Yield();
            }
           
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
            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                mailFrame.GetAllMails().GetEmptyIfNull().ForEach(m => InterfaceLua.MarkMailAsRead(m.Index));
            }

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
                return new Result(ActionResult.Running);
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
            return new Result(ActionResult.Done);
        }

        private async static Task<Result> CanMailItem()
        {
            if (!GaBSettings.Get().SendMail)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending mail deactivated in user settings.");
                return new Result(ActionResult.Failed);
            }

            var toMail =
                GaBSettings.Get().MailItems.GetEmptyIfNull().Where(m => m.CanMail()).ToList();

            var greensToMail =
                CanMailGreens();

            if (greensToMail.Item2 != null)
                toMail.AddRange(greensToMail.Item2);

            if (toMail.GetEmptyIfNull().Any())
                return new Result(ActionResult.Running, toMail);
            
            GarrisonButler.Diagnostic("[Mailing] No items to mail.");
            return new Result(ActionResult.Failed);
        }

        private static Tuple<bool, List<MailItem>> CanMailGreens()
        {
            if (!GaBSettings.Get().SendMail)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending mail deactivated in user settings, can't send greens.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }

            if (!GaBSettings.Get().SendDisenchantableGreens)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending greens deactivated in user settings.");
                return new Tuple<bool, List<MailItem>>(false, null);
            }

            var returnList = new List<MailItem>();
            var sendTo = GaBSettings.Get().GreensToChar;

            if (sendTo.Length <= 0)
            {
                GarrisonButler.Warning("[Mailing] Sending greens enabled but send to character is invalid");
                return new Tuple<bool, List<MailItem>>(false, null);
            }

            var items = HbApi.GetItemsInBags(i => i != null && i.IsValid).ToList();
            //TODO remove later as this list is just used for diagnostic
            var greenItems = new List<WoWItem>();
            var skippedNonGreens = 0;
            var skippedNonMailable = 0;
            var skippedNonDisenchantable = 0;
            var skippedMinerCoffee = 0;
            var skippedMiningPick = 0;

            foreach (var curItem in items)
            {
                if (curItem == null)
                {
                    continue;
                }
                var itemid = curItem.Entry.ToString();
                var name = curItem.Name;


                if (curItem.Quality != WoWItemQuality.Uncommon)
                {
                    //GarrisonButler.Diagnostic("[Mailing] Skipping item because it is not green: itemid={0} name={1}", itemid, name);
                    skippedNonGreens++;
                    continue;
                }

                if (!curItem.IsMailable())
                {
                    //GarrisonButler.Diagnostic("[Mailing] Skipping item because it is not mailable: itemid={0} name={1}", itemid, name);
                    skippedNonMailable++;
                    continue;
                }

                if (curItem.Entry == PreserverdMiningPickItemId)
                {
                    //GarrisonButler.Diagnostic("[Mailing] Skipping item because it is Miner's Coffee: itemid={0} name={1}", itemid, name);
                    skippedMiningPick++;
                    continue;
                }

                if (curItem.Entry == MinersCofeeItemId)
                {
                    //GarrisonButler.Diagnostic("[Mailing] Skipping item because it is Preserved Mining Pick: itemid={0} name={1}", itemid, name);
                    skippedMinerCoffee++;
                    continue;
                }

                if (!curItem.IsDisenchantable())
                {
                    //GarrisonButler.Diagnostic("[Mailing] Skipping item because it is not disenchantable: itemid={0} name={1}", itemid, name);
                    skippedNonDisenchantable++;
                    continue;
                }

                GarrisonButler.Diagnostic("[Mailing] Adding green with itemid={0} name={1} to mailing collection",
                    itemid, name);
                returnList.Add(new MailItem(curItem.Entry, sendTo.ToString(),
                    new MailCondition(MailCondition.Conditions.NumberInBagsSuperiorOrEqualTo, 0), 0));
                greenItems.Add(curItem);
            }

            GarrisonButler.Diagnostic(
                "[Mailing] Searching for greens skipped {0} non-greens, {1} non-mailable, {2} non-disenchantable, {3} stack Miner's Coffee and {4} stack Preserved Mining Pick",
                skippedNonGreens, skippedNonMailable, skippedNonDisenchantable, skippedMinerCoffee, skippedMiningPick);

            if (returnList.Count > 0)
            {
                GarrisonButler.Diagnostic("[Mailing] Found {0} greens to mail.", returnList.Count);
                greenItems.ForEach(d => GarrisonButler.Diagnostic("  -id: {0} name: {1}", d.Entry, d.Name));
                return new Tuple<bool, List<MailItem>>(true, returnList);
            }

            GarrisonButler.Diagnostic("[Mailing] No greens to mail.");
            return new Tuple<bool, List<MailItem>>(false, null);
        }

        public static async Task<Result> MailItem(object obj)
        {
            var mailFrame = MailFrame.Instance;
            var mailItems = obj as List<MailItem>;
            if (mailItems == null)
                return new Result(ActionResult.Failed);

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
                    if ((await MoveToInteract(mailbox)).Status == ActionResult.Running)
                        return new Result(ActionResult.Running);

                mailbox.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }


            //Splitting list based on recipients
            var mailsPerRecipient = mailItems
                .GetEmptyIfNull()
                .GroupBy(i => i.Recipient.Value)
                .Select(x => x.Select(i => i))
                .ToList();

            if (!mailsPerRecipient.Any())
            {
                GarrisonButler.Diagnostic("[Mailing] No mail per recipient found.");
                return new Result(ActionResult.Done);
            }

            foreach (var mailsRecipient in mailsPerRecipient)
            {
                var listItems = new List<WoWItem>();

                var items = mailsRecipient.ToList();
                if (!items.Any())
                {
                    GarrisonButler.Diagnostic("[Mailing] No mailItem found for the current recipient");
                    continue;
                }

                // list all items to send to this recipient
                foreach (var mail in items)
                {
                    var itemsTosend = (await mail.GetItemsToSend()).ToList();

                    if (!itemsTosend.Any())
                        GarrisonButler.Diagnostic("[Mailing] No item to send for ItemId:{0}.", mail.ItemId);
                    else
                        listItems.AddRange(itemsTosend);
                }


                // send if any to send
                if (listItems.GetEmptyIfNull().Any())
                {
                    if (items.GetEmptyIfNull().FirstOrDefault() == default(MailItem))
                    {
                        return new Result(ActionResult.Done);
                    }

                    var firstOrDefault = items.GetEmptyIfNull().FirstOrDefault();
                    if (firstOrDefault != null)
                    {
                        GarrisonButler.Diagnostic("[Mailing] Send - Adding to mail ItemId:{0}.", firstOrDefault.ItemId);
                        await
                            mailFrame.SendMailWithManyAttachmentsCoroutine(firstOrDefault.Recipient.ToString(),
                                listItems.GetEmptyIfNull().ToArray());
                        await CommonCoroutines.SleepForRandomUiInteractionTime();
                        InterfaceLua.ClickSendMail();
                        GarrisonButler.Diagnostic("[Mailing] Send - Sent ItemId:{0}.", firstOrDefault.ItemId);
                    }
                }
                else
                    GarrisonButler.Diagnostic("[Mailing] List to send empty.");
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return new Result(ActionResult.Done);
        }

        private static int _mailCheckInterval = CheckIntervalWhileStillWaiting; // in seconds
    }
}