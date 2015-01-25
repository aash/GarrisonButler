using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using Bots.Professionbuddy.Components;
using Bots.Quest.QuestOrder;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Objects
{
    public class ButlerQuest
    {
        public uint Entry { get; private set; }
        private string _questName;

        public string QuestName
        {
            get { return _questName ?? (_questName = Quest.FromId(Entry).Name); }
        }
        public ButlerPnj PnjPickUp { get; private set; }
        public ButlerPnj PnjTurnIn { get; private set; }

        private ForcedQuestPickUp _questPickUp;
        public ForcedQuestPickUp QuestPickUp
        {
            get
            {
                return _questPickUp ?? (_questPickUp = new ForcedQuestPickUp(Entry, QuestName, PnjPickUp.Entry, PnjPickUp.Name, PnjPickUp.KnownLocation,
                    QuestObjectType.GameObject, NavType.Run));
            }
        }

        public bool IsPickedUp
        {
            get
            {
                return QuestPickUp.IsDone;
                if (StyxWoW.Me == null)
                {
                    GarrisonButler.Diagnostic("Error in class ButlerQuest getting completedPreQuest - Me == null");
                    return false;
                }

                if (!StyxWoW.Me.IsValid)
                {
                    GarrisonButler.Diagnostic(
                        "Error in class ButlerQuest getting completedPreQuest - Me.IsValid = false");
                    return false;
                }

                if (Entry == 0)
                    return false;

                var helper = new ProfileHelperFunctionsBase();
                return helper.HasQuest(Entry);
            }
        }

        public bool IsCompleted
        {
            get
            {
                {
                    if (StyxWoW.Me == null)
                    {
                        GarrisonButler.Diagnostic("Error in class ButlerQuest getting completedPreQuest - Me == null");
                        return false;
                    }

                    if (!StyxWoW.Me.IsValid)
                    {
                        GarrisonButler.Diagnostic(
                            "Error in class ButlerQuest getting completedPreQuest - Me.IsValid = false");
                        return false;
                    }

                    if (Entry == 0)
                        return false;

                    var helper = new ProfileHelperFunctionsBase();
                    var returnValue = helper.IsQuestCompleted(Entry);
                    return returnValue;
                }
            }
        }


        public ActionHelpers.Action[] ObjectivesActions { get; private set; }
        public ButlerQuest(uint entry, ButlerPnj pnjPickUp, ButlerPnj pnjTurnIn, ActionHelpers.Action[] objectivesActions)
        {
            Entry = entry;
            PnjPickUp = pnjPickUp;
            PnjTurnIn = pnjTurnIn;
            ObjectivesActions = objectivesActions;
        }

        public async Task<bool> CanTryToComplete()
        {
            // Quest log not full for example

            // Condition to the completion of the quest
            for (int i = 0; i < ObjectivesActions.Length; i++)
            {
                var action = ObjectivesActions[i];
                if (!isComplete(i) && (await action.Condition()).Status != ActionResult.Running)
                return false;
            }
            //if ((await Condition()).Status == ActionResult.Running)
                return true;
        }

        public async Task<Result> Complete()
        {

            var pnj = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().GetEmptyIfNull().FirstOrDefault(u => u.Entry == PnjPickUp.Entry);
            if (!IsPickedUp)
            {
                if (pnj != default(WoWUnit))
                {
                    if ((await Coroutine.MoveToInteract(pnj)).Status == ActionResult.Running)
                        return new Result(ActionResult.Running);

                    pnj.Interact();
                    await CommonCoroutines.SleepForLagDuration();
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    // Pick Up quest
                    // Gossip and shit

                    if (GossipFrame.Instance == null)
                        return new Result(ActionResult.Failed);

                    var questGossip = GossipFrame.Instance.AvailableQuests.FirstOrDefault(q => q.Id == Entry);
                    if (questGossip == default(GossipQuestEntry))
                    {
                        GarrisonButler.Diagnostic("Couldn't find quest {0} at questgiver.", Entry);
                        return new Result(ActionResult.Failed);
                    }

                    GossipFrame.Instance.SelectAvailableQuest(questGossip.Index);
                    await CommonCoroutines.SleepForLagDuration();
                    await CommonCoroutines.SleepForRandomUiInteractionTime();

                    var questFrame = QuestFrame.Instance;
                    if (questFrame == null)
                    {
                        GarrisonButler.Diagnostic("Couldn't open questFrame, questId:{0} at questgiver.", Entry);
                        return new Result(ActionResult.Failed);
                    }

                    questFrame.AcceptQuest();
                    await CommonCoroutines.SleepForLagDuration();
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                }
                else
                {
                    var moveTo = await Coroutine.MoveTo(PnjPickUp.KnownLocation);
                    if (moveTo.Status == ActionResult.Running)
                        return new Result(ActionResult.Running);
                }
            }
            var quest = StyxWoW.Me.QuestLog.GetQuestById(Entry);
            for (int i = 0; i < quest.GetObjectives().Count; i++)
            {
                var questObjective = quest.GetObjectives()[i];
                var helper = new ProfileHelperFunctionsBase();
                var complete = isComplete(i);
                if (complete)
                {
                    GarrisonButler.Diagnostic("[ButlerX] Objective completed {0}: {1}", i, questObjective.Objective);
                    continue;
                }
                GarrisonButler.Diagnostic("[ButlerX] Running Objective {0}: {1}", i, questObjective.Objective);
                //return await ObjectivesActions[i].ExecuteAction();
                return new Result(ActionResult.Running);
            }
            return new Result(ActionResult.Done);
        }

        private bool isComplete(int i)
        {
            var quest = StyxWoW.Me.QuestLog.GetQuestById(Entry);
            GarrisonButler.Diagnostic("[ButlerX] quest: ");
            ObjectDumper.WriteToHb(quest,3);
            GarrisonButler.Diagnostic("[ButlerX] Objectives: ");
            foreach (var objective in quest.GetObjectives())
            {
                GarrisonButler.Diagnostic("[ButlerX] Objective: " + objective.Objective);
            }
            var questObjective = quest.GetObjectives()[i];
            var helper = new ProfileHelperFunctionsBase();
            var complete = helper.IsObjectiveComplete((int)questObjective.ID, quest.Id);
            return complete;
        }

        public override int GetHashCode()
        {
            return (int)Entry;
        }
    }

    public class ButlerPnj
    {
        public ButlerPnj(uint entry, WoWPoint knownLocation, string name)
        {
            Entry = entry;
            KnownLocation = knownLocation;
            Name = name;
        }

        public uint Entry { get; set; }
        public WoWPoint KnownLocation { get; set; }
        public string Name { get; set; }
    }

    public class ButlerQuestDb
    {
        public HashSet<ButlerQuest> Db;

        private static ButlerQuestDb _instance;
        public static ButlerQuestDb Instance
        {
            get { return _instance ?? (_instance = new ButlerQuestDb()); }
        }
        private ButlerQuestDb()
        {
            Db = Populate();
        }

        private static HashSet<ButlerQuest> Populate()
        {
            return new HashSet<ButlerQuest>()
            {

                // Inscription work order pre-quest horde
                new ButlerQuest(37572,
                    new ButlerPnj(79829,new WoWPoint(),"Urgra"),
                    new ButlerPnj(79831,new WoWPoint(),"Y'rogg"),
                    new ActionHelpers.Action[]
                    {
                        new ActionHelpers.ActionOnTimer(Coroutine.StartOneShipment(), Coroutine.CanStartShipmentQuest(new List<Buildings>()
                        {
                            Buildings.ScribeQuartersLvl1,
                            Buildings.ScribeQuartersLvl2,
                            Buildings.ScribeQuartersLvl3
                        })),
                        
                        new ActionHelpers.ActionOnTimerCached(Coroutine.PickUpShipment,
                            Coroutine.CanPickUpShipmentQuest(new List<Buildings>()
                                    {
                                        Buildings.ScribeQuartersLvl1,
                                        Buildings.ScribeQuartersLvl2,
                                        Buildings.ScribeQuartersLvl3
                                    }))
                    }),
                            // Mine

                //new ButlerQuest(34192, new ButlerPnj(77730, new WoWPoint(), "Timothy Leens"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(35154, new ButlerPnj(81688, new WoWPoint(), "Gorsol"), new ActionHelpers.ActionsSequence())),

                //new ButlerQuest(36404, new ButlerPnj(85344, new WoWPoint(), "Naron Bloomthistle"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(34193, new ButlerPnj(81981, new WoWPoint(), "Tarnon"), new ActionHelpers.ActionsSequence())),

                //new ButlerQuest(36271, new ButlerPnj(84524, new WoWPoint(), "Homer Stonefield"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(36345, new ButlerPnj(85048, new WoWPoint(), "Farmer Lok'lub"), new ActionHelpers.ActionsSequence())),

                //new ButlerQuest(36192, new ButlerPnj(84248, new WoWPoint(), "Justin Timberlord"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(36138, new ButlerPnj(84247, new WoWPoint(), "Lumber Lord Oktron"), new ActionHelpers.ActionsSequence())),

                //new ButlerQuest(37062, new ButlerPnj(0, new WoWPoint(), "0 0"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(37088, new ButlerPnj(0, new WoWPoint(), "0 0 0"), new ActionHelpers.ActionsSequence())),

                //new ButlerQuest(36641, new ButlerPnj(77363, new WoWPoint(), "Mary Kearie"), new ActionHelpers.ActionsSequence()),
                //new ButlerQuest(37568, new ButlerPnj(79813, new WoWPoint(), "Albert de Hyde"),  new ButlerPnj(79813, new WoWPoint(), "Albert de Hyde"), new ActionHelpers.ActionsSequence())),
            
            };
        }
    }
}