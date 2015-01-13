#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;

#endregion

namespace GarrisonButler
{
    public class Mission
    {
        public int Cost;
        public String Description;
        public int DurationSeconds;
        public List<String> Enemies;
        public string Environment;
        public int ItemLevel;
        public bool IsRare;
        public int Level;
        public String Location;
        public int MaterialMultiplier;
        public String MissionId;
        public String Name;
        public int NumFollowers;
        public int NumRewards;
        public int State;
        public bool Success;


        public string SuccessChance;
        public String Type;
        public string Xp;
        public int XpBonus;

        public Mission(int cost, string description, int durationSeconds, List<String> enemies, int level,
            int iItemLevel,
            bool isRare, string location, string missionId, string name, int numFollowers, int numRewards, int state,
            string type, string xp, string environment)
        {
            Cost = cost;
            Description = description;
            DurationSeconds = durationSeconds;
            Enemies = enemies;
            ItemLevel = iItemLevel;
            IsRare = isRare;
            Level = level;
            Location = location;
            MissionId = missionId;
            Name = name;
            NumFollowers = numFollowers;
            NumRewards = numRewards;
            State = state;
            Type = type;
            Xp = xp;
            Environment = environment;
            //GarrisonButler.Diagnostic(ToString());
        }

        public Mission(int cost, string description, int durationSeconds, List<String> enemies, int level,
            int iItemLevel,
            bool isRare, string location, string missionId, string name, int numFollowers, int numRewards, int state,
            string type, int xp, int material, string succesChance, int xpBonus, bool success)
        {
            Cost = cost;
            Description = description;
            DurationSeconds = durationSeconds;
            Enemies = enemies;
            ItemLevel = iItemLevel;
            IsRare = isRare;
            Level = level;
            Location = location;
            MissionId = missionId;
            Name = name;
            NumFollowers = numFollowers;
            NumRewards = numRewards;
            State = state;
            Type = type;
            Xp = xp.ToString(CultureInfo.CurrentCulture);
            MaterialMultiplier = material;
            SuccessChance = succesChance;
            XpBonus = xpBonus;
            Success = success;
        }

        public override string ToString()
        {
            var mission = "";
            mission += "  MissionId: " + MissionId + "\n";
            mission += "  Name: " + Name + "\n";
            mission += "  Cost: " + Cost + "\n";
            mission += "  Xp: " + Xp + "\n";
            mission += "  XpBonus: " + XpBonus + "\n";
            mission += "  Level: " + Level + "\n";
            mission += "  ItemLevel: " + ItemLevel + "\n";
            mission += "  Success: " + Success + "\n";
            mission += "  State: " + State + "\n";
            mission += "  Type: " + Type + "\n";
            mission += "  IsRare: " + IsRare + "\n";
            mission += "  DurationSeconds :" + DurationSeconds + "\n";
            mission += "  Location: " + Location + "\n";
            mission += "  NumFollowers: " + NumFollowers + "\n";
            mission += "  NumRewards: " + NumRewards + "\n";
            mission += "  Description: " + Description + "\n";
            mission += "  successChance: " + SuccessChance + "\n";
            mission += "  materialMultiplier: " + MaterialMultiplier + "\n";
            mission += "  Enemies: " + "\n";
            return Enemies.Aggregate(mission, (current, enemy) => current + ("    " + enemy + "\n"));
        }

        public void PrintCompletedMission()
        {
            string report;
            if (Success)
            {
                report = "MISSION SUCCESS\n" + ToString();
            }
            else
            {
                report = "MISSION FAILED\n" + ToString();
            }
            GarrisonButler.Log(report);
        }

        public Follower[] FindMatch(List<Follower> followers)
        {
            var match = new Follower[NumFollowers];
            // select only follower with a matching ability and a level >= to mission level
            var filteredFollowers =
                followers.Where(f => f.Level >= Level).ToList();
            if (Level == 100) filteredFollowers = filteredFollowers.Where(f => f.ItemLevel >= ItemLevel).ToList();
            var possibleSlot1 = filteredFollowers;

            foreach (var f1 in possibleSlot1)
            {
                match[0] = f1;
                if (NumFollowers > 1)
                {
                    var f4 = f1;
                    foreach (var f2 in possibleSlot1.Where(f2 => f2 != f4))
                    {
                        match[1] = f2;
                        if (NumFollowers > 2)
                        {
                            var f5 = f2;
                            foreach (var f3 in possibleSlot1.Where(f3 => f3 != f5 && f3 != f4))
                            {
                                match[2] = f3;
                                // Check the comp
                                if (IsMatch(match))
                                    return match;
                            }
                        }
                        else
                        {
                            // Check the comp
                            if (IsMatch(match))
                                return match;
                        }
                    }
                }
                else
                {
                    // Check the comp
                    if (IsMatch(match))
                        return match;
                }
            }
            return null;
        }

        private bool IsMatch(IEnumerable<Follower> possibleMatch)
        {
            var counters = possibleMatch.Select(m => m.Counters).SelectMany(x => x).ToList();
            foreach (var ability in Enemies)
            {
                if (counters.Contains(ability))
                    counters.Remove(ability);
                else
                {
                    return false;
                }
            }
            return true;
        }

        public async Task AddFollowersToMission(List<Follower> followers)
        {
            await InterfaceLua.AddFollowersToMission(MissionId, followers.Select(f => f.FollowerId).ToList());
        }

        public class CompletedMission
        {
            public int MaterialMultiplier;
            public String MissionId;
            public String Name;
            public bool Succes = false;
            public int SuccessChance;
            public int Xp;
            public int XpBonus;

            public CompletedMission(string missionId, string name, int xp, int materialMultiplier, int successChance,
                int xpBonus)
            {
                Xp = xp;
                MissionId = missionId;
                Name = name;
                MaterialMultiplier = materialMultiplier;
                SuccessChance = successChance;
                XpBonus = xpBonus;
                GarrisonButler.Diagnostic(ToString());
            }

            public override sealed string ToString()
            {
                var mission = "";
                mission += "  MissionId: " + MissionId + "\n";
                mission += "  Name: " + Name + "\n";
                mission += "  Xp: " + Xp + "\n";
                mission += "  successChance: " + SuccessChance + "\n";
                mission += "  materialMultiplier: " + MaterialMultiplier + "\n";
                mission += "  Succes: " + Succes + "\n";
                return mission;
            }
        }
    }
}