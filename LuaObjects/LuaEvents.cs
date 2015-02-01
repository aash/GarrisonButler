using System.Collections.Generic;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.LuaObjects
{
    public class LuaEvents
    {
        public static bool LootIsOpen { get; private set; }
        public static int LootHash { get; private set; }
        public static int LootCount { get; private set; }

        public const int MaxLootCount = 3;

        internal static void LootClosed(object sender, LuaEventArgs args)
        {
            LootIsOpen = false;
        }

        internal static void LootOpened(object sender, LuaEventArgs args)
        {
            var currenPoi = BotPoi.Current.AsObject as WoWGameObject;
            if (currenPoi != null && currenPoi.IsValid)
            {
                var lootHash = currenPoi.Guid.GetHashCode();
                if (LootHash != lootHash)
                {
                    GarrisonButler.Log("[Loot] Looting {0}.", currenPoi.Name);
                    LootCount = 0;
                    LootHash = lootHash;
                }
                else
                {
                    GarrisonButler.Diagnostic("[Loot] Looting frame of {0} opened for the {1} time.", currenPoi.Name, LootCount);
                    LootCount++;
                }
                if (LootCount > MaxLootCount)
                {
                    GarrisonButler.Warning("[Loot] Tried to loot {0}, {1} times => blacklisting.", currenPoi.Name, LootCount);
                    Objects.Blacklist.Add(currenPoi);
                }
            }

            //var lootFrame = LootFrame.Instance;
            //if (lootFrame != null)
            //{
            //    for (int i = 0; i < lootFrame.LootItems; i++)
            //    {
            //        GarrisonButler.Diagnostic("[Loot] Found LootName {0}.", lootFrame.LootInfo(i).LootName);
            //        GarrisonButler.Diagnostic("[Loot] Found LootIcon {0}.", lootFrame.LootInfo(i).LootIcon);
            //        GarrisonButler.Diagnostic("[Loot] Found LootQuantity {0}.", lootFrame.LootInfo(i).LootQuantity);
            //        GarrisonButler.Diagnostic("[Loot] Found LootRarity {0}.", lootFrame.LootInfo(i).LootRarity);
            //        GarrisonButler.Diagnostic("[Loot] Found Locked {0}.", lootFrame.LootInfo(i).Locked);
            //    }
            //}
            LootIsOpen = true;
        }
    }
}