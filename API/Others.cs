using System.Linq;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;
using System.Collections;
using System.Collections.Generic;

namespace GarrisonButler.API
{
    internal class ApiLua
    {
        internal static int GetMaxStackItem(uint itemId)
        {
            var lua =
                string.Format(
                    "local item = {0}; ", itemId) +
                @"local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(item);
                if not maxStack then
                    return tostring(0);
                end;
                return tostring(maxStack)";
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        internal static bool HasNewMail()
        {
            const string lua = @"local has = HasNewMail();
                if not has then
                    return tostring(false);
                end;
                return tostring(has);";
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        internal static bool IsUsableSpell(int id)
        {
            string lua = string.Format(@"local usable, nomana = IsUsableSpell({0});
                 return tostring(usable);", id);
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        internal static bool IsPlayerSpell(int id)
        {
            const string lua = @"local isKnown = IsPlayerSpell({0});
                 return tostring(isKnown);";
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        internal static string GetItemTypeString(int id)
        {
            string lua =
                string.Format(@"local _,_,_,_,_, itemType = GetItemInfo({0});
                  if not itemType then
                      return tostring(0);
                  else
                      return tostring(itemType);
                  end;", id);

            var result = Lua.GetReturnValues(lua);

            return result.GetEmptyIfNull().FirstOrDefault();
        }

        internal static string GetWeaponTypeString()
        {
            const string lua =
                @"local WEAPON, ARMOR = GetAuctionItemClasses();
                  if not WEAPON then
                      return tostring(0);
                  else
                      return tostring(WEAPON);
                  end;";

            var result = Lua.GetReturnValues(lua);

            return result.GetEmptyIfNull().FirstOrDefault();
        }

        internal static string GetArmorTypeString()
        {
            const string lua =
                @"local WEAPON, ARMOR = GetAuctionItemClasses();
                  if not ARMOR then
                      return tostring(0);
                  else
                      return tostring(ARMOR);
                  end;";

            var result = Lua.GetReturnValues(lua);

            return result.GetEmptyIfNull().FirstOrDefault();
        }

        internal static string GetNameFromItemLink(string link)
        {
            var lua = @"
            local firstPos = string.find('" + link + @"', '[', nil, true);
            local secondPos = string.find('" + link + @"', ']', nil, true);
            local itemName = string.sub('" + link + @"', firstPos + 1, -1*(string.len('" + link + @"')-secondPos+2));
            return itemName;
            ";
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault();
        }

        internal static string GetCurrencyItemLink(int currencyID)
        {
            var lua = @"
            return GetCurrencyLink('" + currencyID + @"');
            ";
            var result = Lua.GetReturnValues(lua);
            return result.GetEmptyIfNull().FirstOrDefault();
        }
    }
}