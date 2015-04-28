using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Frames;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class GetMails : Atom
    {
        private const int CheckIntervalWhileStillWaiting = 65;
        private static bool _checkedMailbox;
        private static int _numMailsOnLastCheck;
        private static readonly Stopwatch MailboxCheckTimer = new Stopwatch();
        // Only check mail every 60s if no items exist in mail frame
        private static bool _allowMailTimerStart = true;
        private static int _mailCheckInterval = CheckIntervalWhileStillWaiting; // in seconds

        private bool _init = false;
        public GetMails()
        {
            Dependencies.Add(new InteractWithMailbox());
        }
        public override bool RequirementsMet()
        {
            if (!GaBSettings.Get().RetrieveMail)
            {
                GarrisonButler.Diagnostic("[Mail] Checking mail deactivated in user settings.");
                return false;
            }
            return true;
        }

        public override bool IsFulfilled()
        {
            if (!_checkedMailbox)
                return false;

            // Only check mail every 5 minutes by default
            // If check interval is 65s, it will return true for HasMails()
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval != CheckIntervalWhileStillWaiting))
            {
                // Timer has not finished yet, indicate "no mail"
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    return true;
                }
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
            }


            return !ApiLua.HasNewMail();
        }

        public async override Task Action()
        {
            // If check interval is 65s, we must wait for the wow server to allow another refresh
            if ((MailboxCheckTimer.IsRunning) && (_mailCheckInterval == CheckIntervalWhileStillWaiting))
            {
                // Need to keep waiting for timer
                if (MailboxCheckTimer.Elapsed.TotalSeconds < (_mailCheckInterval))
                {
                    Status = new Result(ActionResult.Running, "Waiting for timer.");
                    return;
                }
                GarrisonButler.Diagnostic("[Mail] " + _mailCheckInterval + "s mailbox timer finished inside GetMails()");
                MailboxCheckTimer.Reset();
                MailboxCheckTimer.Stop();
            }
            if (!_init)
            {
                // Wait for server to load mails after opening mailbox
                GarrisonButler.Diagnostic("[Mail] Waiting 5s for WoW to load mail.");
                await Buddy.Coroutines.Coroutine.Sleep(5000);
                _init = true; 
            }
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
            while (mailFrame.GetAllMails().Any(m => !m.WasRead || ((m.ItemCount > 0 || m.Copper > 0) && m.CODAmount <= 0)) && !timeout.IsFinished)
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
                Status = new Result(ActionResult.Running, "Waiting for mails to refresh.");
                return;
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
            _mailCheckInterval = 60 * 5; // 5 minutes
            MailboxCheckTimer.Reset();
            MailboxCheckTimer.Start();
            _init = false; 
        }

        public override string Name()
        {
            return "[GetMail]";
        }
    }
}
