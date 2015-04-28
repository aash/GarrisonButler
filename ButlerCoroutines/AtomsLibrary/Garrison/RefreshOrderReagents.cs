#region

using System.Threading.Tasks;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    internal class RefreshOrderReagents : Atom
    {
        private readonly Building _building;

        public RefreshOrderReagents(Building building)
        {
            _building = building;
            Dependencies.Add(new InteractWithOrderNpc(_building));
        }

        public override bool RequirementsMet()
        {
            return true;
        }

        public override bool IsFulfilled()
        {
            return !_building.IsActionForRefreshNeeded();
        }

        public override async Task Action()
        {
            _building.RefreshOrderTradingPost();
        }

        public override string Name()
        {
            return "[RefreshOrderReagents|" + _building.Name + "]";
        }
    }
}