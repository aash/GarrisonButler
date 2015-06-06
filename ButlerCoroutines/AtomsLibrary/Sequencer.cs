using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta;
using Styx.Common.Helpers;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary
{
    /// <summary>
    /// The sequencer is responsible of the list of current actions to execute and how to execute them (by priority, decreasing order).
    /// </summary>
    class Sequencer
    {
        // Sorted set because actions are sorted by priority and actions are unique. Same actions can't be added twice.
        private static SortedSet<Molecule> _actions;
        private static Sequencer _instance;
        private static WaitTimer _timer;
        public static Sequencer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                else
                {
                    _instance = new Sequencer();
                    return _instance;
                }
            }
        }

        private Sequencer()
        {
            _timer = new WaitTimer(TimeSpan.FromMilliseconds(100));
            _toDelete = new List<Molecule>();
            // Activate buildings ????????



            // Creating a new list for the actions
            _actions = new SortedSet<Molecule>();
            _actions.Add(new Molecule(new UseGarrisonHearthstoneIfNeeded(), 0));

            if (GarrisonButler.IsIceVersion())
                _actions.Add(new Molecule(new GetMails(), 100));
            _actions.Add(new Molecule(new ActivateBuildings(), 150));
            //_actions.Add(new Molecule(new UseMinerCoffee(), 200));
            //_actions.Add(new Molecule(new UseMiningPick(), 201));
            _actions.Add(new Molecule(new CleanMine(), 202));
            _actions.Add(new Molecule(new PickUpOrderMine(), 203));
            _actions.Add(new Molecule(new StartOrdersMine(), 204));
            _actions.Add(new Molecule(new CleanGarden(), 300));
            _actions.Add(new Molecule(new PickUpOrderGarden(), 301));
            _actions.Add(new Molecule(new StartOrderGarden(), 302));


            if (GarrisonButler.IsIceVersion()) 
                _actions.Add(new Molecule(new DisenchantItems(), 400));

            _actions.Add(new Molecule(new CraftAllDailies(), 500));
                
            int priority = 1000;
            foreach (var building in ButlerCoroutine._buildings)
            {
                _actions.Add(new Molecule(new PickUpWorkOrders(building), priority));
                priority += 1;
                _actions.Add(new Molecule(new StartWorkOrders(building), priority)); 
                priority += 1;
            }



            if (GarrisonButler.IsIceVersion())
            {
                _actions.Add(new Molecule(new UseGearArmorToken(), 2000));
                _actions.Add(new Molecule(new UseGearWeaponToken(), 2100));
            }
            _actions.Add(new Molecule(new HarvestCache(), 2150));
            _actions.Add(new Molecule(new TurnInMissions(), 2200));
            _actions.Add(new Molecule(new StartMissions(), 2300));
            _actions.Add(new Molecule(new SalvageCrates(), 2400));
            _actions.Add(new Molecule(new SellJunk(), 2500));

            if (GarrisonButler.IsIceVersion())
                _actions.Add(new Molecule(new DisenchantItems(), 2550));

            if (GarrisonButler.IsIceVersion()) 
                _actions.Add(new Molecule(new MailItems(), 2600));

            _actions.Add(new Molecule(new LastRound(), 2700));
            _actions.Add(new Molecule(new Waiting(), 2800));
        }

        private List<Molecule> _toDelete;

        private Atom _currentAction = null; 
        public async Task<bool> Execute()
        {
            // Timer anti spam
            if (!_timer.IsFinished)
                return true;
            _timer.Reset();


            // Clean list of actions from fullfilled not repeated
            if (_toDelete.Any())
            {
                foreach (var molecule in _toDelete)
                {
                    _actions.Remove(molecule);
                }
                _toDelete.Clear();
            }


            // NEW ---------------------
            if (_currentAction != null)
            {
                switch (_currentAction.Status.State)
                {
                    case ActionResult.Init:
                    case ActionResult.Running:
                    case ActionResult.Refresh:
                        await _currentAction.Execute();
                        GarrisonButler.Diagnostic("[Sequencer] Pulsed Action {0}, status => {1}", _currentAction.Name(), _currentAction.Status);
                        return true;

                    case ActionResult.Done:
                    case ActionResult.Failed:
                        _currentAction = null;
                        break;
                }
            }


            // Find new action to perform
            foreach (var mol in _actions)
            {
                var action = mol.BigAction;
                if (action.IsFulfilled())
                {
                    //GarrisonButler.Diagnostic("[Sequencer] Skipping Action {0} since Fullfilled ****************************************************** ", action.Name());

                    if (!action.ShouldRepeat)
                    {
                        GarrisonButler.Diagnostic("[Sequencer] Action {0} will be deleted since Fullfilled ****************************************************** ", action.Name());
                        _toDelete.Add(mol);
                    }
                    continue;
                }

                if (!action.RequirementsMet())
                {
                    //GarrisonButler.Diagnostic("[Sequencer] Skipping Action {0} since Requirements not met ****************************************************** ", action.Name());
                    continue;
                }

                //GarrisonButler.Diagnostic("[Sequencer] Executing Action {0}, Status: ", action.Name());
                //await action.Execute();

                _currentAction = action;
                GarrisonButler.Diagnostic("[Sequencer] new Action {0}", action.Name());
                return true; 
            }

            // NEW FIN ++++++++++++++++++++++

            //// Main pulse
            ////GarrisonButler.Diagnostic("[Sequencer] ************************************ Beginning Cycle ************************************ ");
            //foreach (var mol in _actions)
            //{
            //    var action = mol.BigAction;
            //    if (action.IsFulfilled())
            //    {
            //        //GarrisonButler.Diagnostic("[Sequencer] Skipping Action {0} since Fullfilled ****************************************************** ", action.Name());

            //        if (!action.ShouldRepeat)
            //        {
            //            GarrisonButler.Diagnostic("[Sequencer] Action {0} will be deleted since Fullfilled ****************************************************** ", action.Name());
            //            _toDelete.Add(mol);
            //        }
            //        continue;
            //    }

            //    if (!action.RequirementsMet())
            //    {
            //        //GarrisonButler.Diagnostic("[Sequencer] Skipping Action {0} since Requirements not met ****************************************************** ", action.Name());
            //        continue;
            //    }

            //    //GarrisonButler.Diagnostic("[Sequencer] Executing Action {0}, Status: ", action.Name());
            //    await action.Execute();
            //    GarrisonButler.Diagnostic("[Sequencer] Pulsed {0}, Status: {1}", action.Name(), action.Status);

            //    if (action.Status.State != ActionResult.Failed)
            //    {
            //        return true;
            //    }
            //} 
            GarrisonButler.Log("[Sequencer] All actions fulfilled, Nothing left to do.");
            return false;
        }

        public class Molecule : IComparable
        {
            public Atom BigAction;
            public int Priority;

            public Molecule(Atom bigAction, int priority)
            {
                BigAction = bigAction;
                Priority = priority;
            }

            public int Compare(object x, object y)
            {
                Molecule c1 = (Molecule)x;
                Molecule c2 = (Molecule)y;
                if (c1.Priority > c2.Priority)
                    return 1;
                if (c1.Priority < c2.Priority)
                    return -1;
                else
                    return 0;
            }

            public int CompareTo(object obj)
            {
                if (obj == null) return 1;

                Molecule m = obj as Molecule;
                if (m != null)
                    return this.Priority.CompareTo(m.Priority);
                else
                    throw new ArgumentException("Object is not a Molecule");
            }
        }
    }
}
