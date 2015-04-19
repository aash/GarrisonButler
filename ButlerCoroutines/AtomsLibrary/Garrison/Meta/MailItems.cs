using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    internal class MailItems : Atom
    {
        private const int PreserverdMiningPickItemId = 118903;
        private const int MinersCofeeItemId = 118897;

        public MailItems()
        {
            Dependencies.Add(new InteractWithMailbox());
        }
        public override bool RequirementsMet()
        {
            return true;
        }

        // This version is not fast enough! 
        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().SendMail)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending mail deactivated in user settings.");
                return true;
            }

            if (GaBSettings.Get().MailItems.GetEmptyIfNull().Any(m => m.CanMail()))
                return false;

            return !AnyGreenToMail();
        }

        public async override Task Action()
        {
            var mailItems =
                GaBSettings.Get().MailItems.GetEmptyIfNull().Where(m => m.CanMail()).ToList();

            var greensToMail = CanMailGreens();

            if (greensToMail.Item2 != null)
                mailItems.AddRange(greensToMail.Item2);

            if (!mailItems.GetEmptyIfNull().Any())
            {
                Status = new Result(ActionResult.Failed, "No mails to send could be found.");
                return;
            }

            var mailFrame = MailFrame.Instance;

            if (!mailFrame.IsVisible)
            {
                Status = new Result(ActionResult.Failed, "Mail Frame not visible.");
                return; 
            }


            //Splitting list based on recipients
            var mailsPerRecipient = mailItems
                .GetEmptyIfNull()
                .GroupBy(i => i.Recipient.Value)
                .Select(x => x.Select(i => i))
                .ToList();

            if (!mailsPerRecipient.Any())
            {
                Status = new Result(ActionResult.Failed, "No mail per recipient found.");
                return;
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
                        return;
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
            Status = new Result(ActionResult.Done, "All mail sent.");
        }

        public override string Name()
        {
            return "[MailItems]";
        }

        private bool AnyGreenToMail()
        {
            if (!GaBSettings.Get().SendDisenchantableGreens)
            {
                GarrisonButler.Diagnostic("[Mailing] Sending greens deactivated in user settings.");
                return false;
            }

            var returnList = new List<MailItem>();
            var sendTo = GaBSettings.Get().GreensToChar;

            if (sendTo.Length <= 0)
            {
                GarrisonButler.Warning("[Mailing] Sending greens enabled but send to character is invalid");
                return false;
            }

            var items = HbApi.GetItemsInBags(i => i != null && i.IsValid).ToList();
            // TODO remove later as this list is just used for diagnostic
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
                return true;
            }
            return false;
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
            // TODO remove later as this list is just used for diagnostic
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
    }
}
