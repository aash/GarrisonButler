using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonLua;

namespace GarrisonBuddy
{
    public class Mission
    {
        public int Cost;
        public String Description;
        public int DurationSeconds;
        public List<String> Enemies;
        public string Environment;
        public int ILevel;
        public bool IsRare;
        public int Level;
        public String Location;
        public int MaterialMultiplier;
        public String MissionId;
        public String Name;
        public int NumFollowers;
        public int NumRewards;
        public int State;
        public bool Success = false;


        public string SuccessChance;
        public String Type;
        public string Xp;
        public int XpBonus;

        public Mission(int cost, string description, int durationSeconds, List<String> enemies, int level, int iLevel,
            bool isRare, string location, string missionId, string name, int numFollowers, int numRewards, int state,
            string type, string xp, string environment)
        {
            Cost = cost;
            Description = description;
            DurationSeconds = durationSeconds;
            Enemies = enemies;
            ILevel = iLevel;
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
            GarrisonBuddy.Diagnostic(ToString());
        }

        public Mission(int cost, string description, int durationSeconds, List<String> enemies, int level, int iLevel,
            bool isRare, string location, string missionId, string name, int numFollowers, int numRewards, int state,
            string type, int xp, int material, string succesChance, int xpBonus, bool success)
        {
            Cost = cost;
            Description = description;
            DurationSeconds = durationSeconds;
            Enemies = enemies;
            ILevel = iLevel;
            IsRare = isRare;
            Level = level;
            Location = location;
            MissionId = missionId;
            Name = name;
            NumFollowers = numFollowers;
            NumRewards = numRewards;
            State = state;
            Type = type;
            Xp = xp.ToString();
            MaterialMultiplier = material;
            SuccessChance = succesChance;
            XpBonus = xpBonus;
            Success = success;
        }

        public override string ToString()
        {
            String mission = "";
            mission += "  MissionId: " + MissionId + "\n";
            mission += "  Name: " + Name + "\n";
            mission += "  Cost: " + Cost + "\n";
            mission += "  Xp: " + Xp + "\n";
            mission += "  XpBonus: " + XpBonus + "\n";
            mission += "  Level: " + Level + "\n";
            mission += "  ILevel: " + ILevel + "\n";
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
            foreach (string enemy in Enemies)
            {
                mission += "    " + enemy + "\n";
            }
            return mission;
        }

        public void PrintCompletedMission()
        {
            string report = "";
            if (Success)
            {
                report = "MISSION SUCCESS\n" + ToString();
            }
            else
            {
                report = "MISSION FAILED\n" + ToString();
            }
            GarrisonBuddy.Log(report);
        }

        public Follower[] FindMatch(List<Follower> followers)
        {
            var match = new Follower[NumFollowers];
            // select only follower with a matching ability and a level >= to mission level
            List<Follower> filteredFollowers =
                followers.Where(f => f.Level >= Level).ToList();
            if (Level == 100) filteredFollowers = filteredFollowers.Where(f => f.ILevel >= ILevel).ToList();
            List<Follower> possibleSlot1 = filteredFollowers;

            foreach (Follower f1 in possibleSlot1)
            {
                match[0] = f1;
                if (NumFollowers > 1)
                {
                    foreach (Follower f2 in possibleSlot1)
                    {
                        if (f2 == f1) continue;
                        match[1] = f2;
                        if (NumFollowers > 2)
                        {
                            foreach (Follower f3 in possibleSlot1)
                            {
                                if (f3 == f2 || f3 == f1) continue;
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
            List<String> counters = possibleMatch.Select(m => m.Counters).SelectMany(x => x).ToList();
            foreach (string ability in Enemies)
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

        public void AddFollowersToMission(List<Follower> followers)
        {
            InterfaceLua.AddFollowersToMission(MissionId, followers.Select(f => f.FollowerId).ToList());
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
                GarrisonBuddy.Diagnostic(ToString());
            }

            public override string ToString()
            {
                String mission = "";
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