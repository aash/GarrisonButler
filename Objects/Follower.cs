#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace GarrisonButler
{
    public class Follower
    {
        public String Class;
        public List<String> Counters;
        public String FollowerId;
        public int ItemLevel;
        public bool IsCollected;
        public int Level;
        public int LevelXp;
        public String Name;
        public String Quality;
        public String Status;
        public int Xp;

        public Follower(string followerId, string name, string status,
            string Class_, String quality, int level, bool isCollected,
            int iItemLevel, int xp, int levelXp, List<String> counters)
        {
            FollowerId = followerId;
            Name = name;
            Status = status;
            Class = Class_;
            Quality = quality;
            Level = level;
            IsCollected = isCollected;
            ItemLevel = iItemLevel;
            Xp = xp;
            LevelXp = levelXp;
            Counters = counters;
            //GarrisonButler.Diagnostic(ToString());
        }

        public override string ToString()
        {
            var follower = "";
            follower += "  FollowerId: " + FollowerId + "\n";
            follower += "  Name: " + Name + "\n";
            follower += "  Status: " + Status + "\n";
            follower += "  Class: " + Class + "\n";
            follower += "  Quality: " + Quality + "\n";
            follower += "  Level: " + Level + "\n";
            follower += "  IsCollected: " + IsCollected + "\n";
            follower += "  ItemLevel: " + ItemLevel + "\n";
            follower += "  Xp: " + Xp + "\n";
            follower += "  LevelXp: " + LevelXp + "\n";
            return Counters.Aggregate(follower, (current, counter) => current + ("   Counter: " + counter + "\n"));
        }
    }
}