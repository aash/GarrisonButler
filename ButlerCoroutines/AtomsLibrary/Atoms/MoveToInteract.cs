#region

using System.Threading.Tasks;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class MoveToInteract : MoveTo
    {
        private readonly WoWObject _object;

        public MoveToInteract(WoWUnit npc)
            : base(npc.Location, npc.InteractRange)
        {
            // Should check the unit!
            if (!npc.IsValid)
            {
                Status = new Result(ActionResult.Failed, "Unit is not valid");
                GarrisonButler.Diagnostic("Creating MoveToInteract Failed npc is not valid");
                return;
            }

            GarrisonButler.Diagnostic("Creating MoveToInteract with npc: {0}", npc.SafeName);
            if (npc.Location == default(WoWPoint))
            {
                Status = new Result(ActionResult.Failed,
                    "Unit (" + _object.SafeName + ") location is equal to default value.");
                return;
            }
            _object = npc;
            Status = new Result(ActionResult.Init);
        }

        public MoveToInteract(WoWObject obj)
            : base(obj.Location, obj.InteractRange)
        {
            // Should check the unit!
            if (!obj.IsValid)
            {
                Status = new Result(ActionResult.Failed, "Object is not valid");
                GarrisonButler.Diagnostic("Creating MoveToInteract Failed Object is not valid");
                return;
            }

            GarrisonButler.Diagnostic("Creating MoveToInteract with WoWObject: {0}", obj.SafeName);
            if (obj.Location == default(WoWPoint))
            {
                Status = new Result(ActionResult.Failed,
                    "Object (" + _object.SafeName + ") location is equal to default value.");
                return;
            }
            _object = obj;
            Status = new Result(ActionResult.Init);
        }

        /// <summary>
        /// Can the player move to the NPC?
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            GarrisonButler.Diagnostic("Checking requirement for MoveToInteract with {0}", _object.SafeName);
            //Should test to generate path
            if (Navigator.CanNavigateWithin(StyxWoW.Me.Location, Location, _object.InteractRange))
                return true;
            return false;
        }

        /// <summary>
        /// Should be within interaction range of the player.
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            GarrisonButler.Diagnostic("Checking IsFulfilled for MoveToInteract with {0}", _object.SafeName);
            return _object.WithinInteractRange;
        }

        /// <summary>
        /// See base method
        /// </summary>
        /// <returns></returns>
        public override async Task Action()
        {
            GarrisonButler.Diagnostic("Running Action for MoveToInteract with {0}", _object.SafeName);
            await base.Action();
        }
    }
}