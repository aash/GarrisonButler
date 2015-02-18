#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace GarrisonButler
{
    public class Follower : IEquatable<Follower>, IComparable<Follower>
    {
        public bool Equals(Follower other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Class, other.Class)
                && Counters.Equals(other.Counters)
                && string.Equals(FollowerId, other.FollowerId)
                && ItemLevel == other.ItemLevel
                //&& IsCollected.Equals(other.IsCollected)
                && Level == other.Level
                && LevelXp == other.LevelXp
                && string.Equals(Name, other.Name)
                && string.Equals(Quality, other.Quality)
                && string.Equals(Status, other.Status)
                && Xp == other.Xp
                && Abilities.Equals(other.Abilities);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Follower) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Class.GetHashCode();
                hashCode = (hashCode*397) ^ Counters.GetHashCode();
                hashCode = (hashCode*397) ^ FollowerId.GetHashCode();
                hashCode = (hashCode*397) ^ ItemLevel;
                //hashCode = (hashCode*397) ^ IsCollected.GetHashCode();
                hashCode = (hashCode*397) ^ Level;
                hashCode = (hashCode*397) ^ LevelXp;
                hashCode = (hashCode*397) ^ Name.GetHashCode();
                hashCode = (hashCode*397) ^ Quality.GetHashCode();
                hashCode = (hashCode*397) ^ Status.GetHashCode();
                hashCode = (hashCode*397) ^ Xp;
                hashCode = (hashCode*397) ^ Abilities.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Follower left, Follower right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Follower left, Follower right)
        {
            return !Equals(left, right);
        }

        public readonly String Class;
        public readonly List<String> Counters;
        public readonly String FollowerId;
        public readonly String UniqueId;
        public readonly int ItemLevel;
        public readonly bool IsCollected;
        public readonly int Level;
        public readonly int LevelXp;
        public readonly String Name;
        public readonly String Quality;
        public readonly String Status;
        public readonly int Xp;
        public readonly List<int> Abilities;

        public Follower(string followerId, string uniqueId, string name, string status,
            string Class_, String quality, int level, bool isCollected,
            int iItemLevel, int xp, int levelXp, List<String> counters, List<int> abilities )
        {
            FollowerId = followerId;
            UniqueId = uniqueId;
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
            Abilities = abilities;
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

        int IComparable<Follower>.CompareTo(Follower other)
        {
            return this.FollowerId.CompareTo(other.FollowerId);
        }
    }
}