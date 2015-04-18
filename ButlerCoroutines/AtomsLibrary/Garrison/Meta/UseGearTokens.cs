using System;
using System.Collections.Generic;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    // Weapon: http://www.wowhead.com/item=120302/weapon-enhancement-token
    public class UseGearWeaponToken : UseItem
    {
        private const uint TokenId = 120302;

        public UseGearWeaponToken()
            : base(TokenId)
        {
            ShouldRepeat = true;
        }
    }
    // Armor : http://www.wowhead.com/item=120301/armor-enhancement-token
    public class UseGearArmorToken : UseItem
    {
        private const uint TokenId = 120301;

        public UseGearArmorToken()
            : base(TokenId)
        {
            ShouldRepeat = true;
        }
    }
}