using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.WoWInternals.Garrison;

namespace GarrisonButler.Libraries
{
    public static class GarrisonMissionExtension
    {
        public static int Level(this GarrisonMission source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrisonMissionExtension.Level: !!! Passed in GarrisonMission was null !!!");
                return -1;
            }

            var luaMissionObject = API.MissionLua.GetMissionById(source.Id.ToString());
            if (luaMissionObject == null)
            {
                GarrisonButler.Diagnostic("GarrisonMissionExtension.Level: !!! LUA returned NULL for MissionId={0} !!!",
                    source.Id);
                return -1;
            }

            return luaMissionObject.Level;
        }
    }
}
