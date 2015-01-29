using System;
using System.Collections.Generic;
using Styx.Helpers;

namespace GarrisonButler
{
    public class MissionFollowersCombo : IEquatable<MissionFollowersCombo>
    {
        public readonly Mission _mission;
        public readonly List<Follower> _followers;

        public MissionFollowersCombo(Mission mission, List<Follower> followers)
        {
            _mission = mission;
            _followers = followers;
        }

        public int GetExperience()
        {
            return _mission.Xp.ToInt32();
        }

        public bool Equals(MissionFollowersCombo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _mission.Equals(other._mission) && _followers.Equals(other._followers);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MissionFollowersCombo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_mission.GetHashCode()*397) ^ _followers.GetHashCode();
            }
        }

        public static bool operator ==(MissionFollowersCombo left, MissionFollowersCombo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MissionFollowersCombo left, MissionFollowersCombo right)
        {
            return !Equals(left, right);
        }
    }
}