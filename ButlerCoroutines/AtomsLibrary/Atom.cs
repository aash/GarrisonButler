#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Styx.Common.Helpers;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary
{
    /// <summary>
    /// An Atom is the smallest action possible
    /// An Atom is defined by the action it represents and the condition to execute it.
    /// 
    /// </summary>
    public abstract class Atom
    {

        /// <summary>
        /// Current status of the atome, it represents the state of the atome (running, done, init, failed)
        /// </summary>
        public Result Status { get; set; }

        /// <summary>
        /// This is the condition to run this action, the "can" to run the action or not. Needs to be defined for every action model.
        /// </summary>
        /// <returns></returns>
        public abstract bool RequirementsMet();

        /// <summary>
        /// Is the job done?
        /// </summary>
        /// <returns></returns>
        public abstract bool IsFulfilled();

        /// <summary>
        /// The list of atoms than needs to be fulfilled before we can engage our own job.
        /// </summary>
        public List<Atom> Dependencies { get; set; }

        /// <summary>
        /// The line of code executed by the atome, represents the action part. 
        /// </summary>
        /// <returns></returns>
        public abstract Task Action();

        private bool Running = false;
        private WaitTimer timer;
        public bool ShouldRepeat = false;

        public abstract string Name();
        public async Task Execute()
        {
            //GarrisonButler.Diagnostic("{0} Execute Called.", Name()); 

            // If job done
            if (IsFulfilled())
            {
                GarrisonButler.Diagnostic("{0} is fulfilled. Setting Atom as Done.", Name());
                Status = new Result(ActionResult.Done, "Fulfilled");
                Running = false;
                return; 
            }

            // if not job done and requirements met
            
            if (timer.IsFinished)
            {
                timer.Reset();

                if (!RequirementsMet())
                {
                    GarrisonButler.Diagnostic("{0} Requirements not met, setting status as Failed. State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
                    Status = new Result(ActionResult.Failed, "Requirements not met.");
                    return;
                }

                //GarrisonButler.Diagnostic("{0} Requirements met, setting status as Running. State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
                Status = new Result(ActionResult.Running, "Running");
                Running = true;
            }
            if (Running)
            {
                // Do Dependencies first
                foreach (var dependency in Dependencies)
                {
                    if (!dependency.IsFulfilled())
                    {
                        await dependency.Execute();
                        //GarrisonButler.Diagnostic("{0} Dependencies - Executing {1} Done, State: {2} - Reason: {3}", Name(), dependency.Name(), dependency.Status.State, dependency.Status.Content);

                        switch (dependency.Status.State)
                        {
                            case ActionResult.Failed:
                                return;
                            case ActionResult.Running:
                                Status = dependency.Status;
                                return;
                        }
                    }
                }

                // Do action, now the action is in charge of the status
                //GarrisonButler.Diagnostic("{0} Executing Action", Name());
                await Action();
                //GarrisonButler.Diagnostic("{0} Executing Action - Done, State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
            }
            
        }
        public async Task ExecuteOld()
        {
            //GarrisonButler.Diagnostic("{0} Execute Called.", Name()); 

            // If job done
            if (IsFulfilled())
            {
                GarrisonButler.Diagnostic("{0} is fulfilled. Setting Atom as Done.", Name());
                Status = new Result(ActionResult.Done, "Fulfilled");
                Running = false;
            }

            //COMMENTED OUT BECAUSE: Requirements are checked externally to know if a task should be added to the current list of task. But once added, it is kept until failed or done. 

            //{
            //    State = new Result(ActionResult.Failed);
            //}

            // if not job done and requirements met
            else
            {
                if (timer.IsFinished)
                {
                    timer.Reset();

                    if (!RequirementsMet())
                    {
                        GarrisonButler.Diagnostic("{0} Requirements not met, setting status as Failed. State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
                        Status = new Result(ActionResult.Failed, "Requirements not met.");
                        return;
                    }

                    GarrisonButler.Diagnostic("{0} Requirements met, setting status as Running. State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
                    Status = new Result(ActionResult.Running, "Running");
                    Running = true;
                }
                if (Running)
                {
                    // Do Dependencies first
                    foreach (var dependency in Dependencies)
                    {
                        GarrisonButler.Diagnostic("{0} Dependencies - Executing {1}", Name(), dependency.Name());
                        await dependency.Execute();
                        GarrisonButler.Diagnostic("{0} Dependencies - Executing {1} Done, State: {2} - Reason: {3}", Name(), dependency.Name(), dependency.Status.State, dependency.Status.Content);

                        switch (dependency.Status.State)
                        {
                            case ActionResult.Failed:
                                return;
                            case ActionResult.Running:
                                Status = dependency.Status;
                                return;
                        }
                    }

                    // Do action, now the action is in charge of the status
                    GarrisonButler.Diagnostic("{0} Executing Action", Name());
                    await Action();
                    GarrisonButler.Diagnostic("{0} Executing Action - Done, State: {1} - Reason: {2}", Name(), Status.State, Status.Content);
                }
            }
        }

        public Atom()
        {
            Status = new Result(ActionResult.Init, "Initialized");
            Dependencies = new List<Atom>();
            timer = new WaitTimer(TimeSpan.FromMilliseconds(5000));
        }
    }
}