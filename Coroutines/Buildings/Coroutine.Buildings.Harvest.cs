using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using GarrisonBuddy.Config;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private async static Task<bool> HarvestWoWGameOject(WoWGameObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }
            //if (await MoveTo(toHarvest.Location))
            //    return true;

            //if (!await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsMoving || Me.Location.Distance(toHarvest.Location) < 1))
            //{
            //    WoWMovement.MoveStop();
            //}
            //await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();
            //GarrisonBuddy.Diagnostic("sleep");
            //await Buddy.Coroutines.Coroutine.Sleep(200);

            //if (!Me.IsMoving && !Me.IsCasting && BotPoi.Current.AsObject != toHarvest)
            //    toHarvest.Interact();
            if(node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);
            
            return true;
        }
    }
}