using System.Linq;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;

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
            var t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToInt32();
        }

        internal static bool HasNewMail()
        {
            const string lua = @"local has = HasNewMail();
                if not has then
                    return tostring(false);
                end;
                return tostring(has);";
            var t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        internal static bool IsUsableSpell(int id)
        {
            const string lua = @"local usable, nomana = IsUsableSpell({0});
                 return tostring(usable);";
            var t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        internal static bool IsPlayerSpell(int id)
        {
            const string lua = @"local isKnown = IsPlayerSpell({0});
                 return tostring(isKnown);";
            var t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }
    }
}