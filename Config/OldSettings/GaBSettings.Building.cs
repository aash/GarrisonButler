#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarrisonButler.Libraries;

#endregion

namespace GarrisonButler.Config.OldSettings
{
    public class BuildingSettings
    {
        public BuildingSettings(List<int> buildingIds, bool canStartOrder = false,
            int maxCanStartOrder = 0, bool canCollectOrder = false)
        {
            BuildingIds = buildingIds;
            CanStartOrder = canStartOrder;
            MaxCanStartOrder = maxCanStartOrder;
            CanCollectOrder = canCollectOrder;
            Name = NameFromBuildingId(buildingIds.GetEmptyIfNull().FirstOrDefault());
        }

        public BuildingSettings()
        {
            BuildingIds = new List<int>();
        }

        public List<int> BuildingIds { get; set; }
        public string Name { get; set; }
        public bool CanStartOrder { get; set; }
        public int MaxCanStartOrder { get; set; }
        public bool CanCollectOrder { get; set; }

        public override string ToString()
        {
            var ret = "Name:" + Name + " - [";
            for (var i = 0; i < BuildingIds.Count; i++)
            {
                if (i != 0)
                    ret += ", ";
                var id = BuildingIds[i];
                ret += id;
            }
            ret += "] - Collect: " + CanCollectOrder + " - Start: " + CanStartOrder + " #" + MaxCanStartOrder;
            return ret;
        }

        public static string NameFromBuildingId(int id)
        {
            var tempName = Enum.GetName(typeof (Buildings), id);
            if (tempName == null) return null;
            var withoutNumbers = new String(tempName.Where(c => c != '-' && (c < '0' || c > '9')).ToArray());
            var withoutLevel = withoutNumbers.Replace("Lvl", "");
            var withSpaces = AddSpacesToSentence(withoutLevel);
            return withSpaces;
        }

        private static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            var newText = new StringBuilder(text.Length*2);
            newText.Append(text[0]);
            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public Config.BuildingSettings FromOld()
        {
            var newSettings = new Config.BuildingSettings(BuildingIds, CanStartOrder, MaxCanStartOrder, CanCollectOrder);
            return newSettings;
        }
    }
}