using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Bots.Professionbuddy.Components;
using Styx.CommonBot;
using Styx.Helpers;

namespace GarrisonBuddy.Config
{
    public class BuildingSettings
    {
        public List<int> BuildingIds { get; set; }
        public string Name { get; set; }
        public bool CanStartOrder { get; set; }
        public int MaxCanStartOrder { get; set; }
        public bool CanCollectOrder { get; set; }

        public BuildingSettings(List<int> buildingIds, bool canStartOrder = false,
            int maxCanStartOrder = 0, bool canCollectOrder = false)
        {
            BuildingIds = buildingIds;
            CanStartOrder = canStartOrder;
            MaxCanStartOrder = maxCanStartOrder;
            CanCollectOrder = canCollectOrder;
            Name = nameFromBuildingID(buildingIds.First());
        }

        public BuildingSettings()
        {
            BuildingIds = new List<int>();
        }

        public override string ToString()
        {
            string ret = "Name:" + Name + " - [";
            for (int i = 0; i < BuildingIds.Count; i++)
            {
                if (i != 0)
                    ret += ", ";
                var Id = BuildingIds[i];
                ret += Id;
            }
            ret += "] - Collect: " + CanCollectOrder + " - Start: " + CanStartOrder + " #" + MaxCanStartOrder;
            return ret;
        }

        public static string nameFromBuildingID(int Id)
        {
            var tempName = Enum.GetName(typeof (buildings), Id);
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
    }
}