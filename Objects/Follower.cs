using System;
using System.Collections.Generic;

namespace GarrisonBuddy
{
    public class Follower
    {
        public String Class;
        public List<String> Counters;
        public String FollowerId;
        public int ILevel;
        public bool IsCollected;
        public int Level;
        public int LevelXp;
        public String Name;
        public String Quality;
        public String Status;
        public int Xp;

        public Follower(string followerId, string name, string status,
            string Class_, String quality, int level, bool isCollected,
            int iLevel, int xp, int levelXp, List<String> counters)
        {
            FollowerId = followerId;
            Name = name;
            Status = status;
            Class = Class_;
            Quality = quality;
            Level = level;
            IsCollected = isCollected;
            ILevel = iLevel;
            Xp = xp;
            LevelXp = levelXp;
            Counters = counters;
            //GarrisonBuddy.Diagnostic(ToString());
        }

        public override string ToString()
        {
            String follower = "";
            follower += "  FollowerId: " + FollowerId + "\n";
            follower += "  Name: " + Name + "\n";
            follower += "  Status: " + Status + "\n";
            follower += "  Class: " + Class + "\n";
            follower += "  Quality: " + Quality + "\n";
            follower += "  Level: " + Level + "\n";
            follower += "  IsCollected: " + IsCollected + "\n";
            follower += "  ILevel: " + ILevel + "\n";
            follower += "  Xp: " + Xp + "\n";
            follower += "  LevelXp: " + LevelXp + "\n";
            foreach (string counter in Counters)
            {
                follower += "   Counter: " + counter + "\n";
            }
            return follower;
        }
    }
}