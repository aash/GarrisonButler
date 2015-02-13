using System.Collections.Generic;
using Styx.Helpers;

namespace GarrisonButler.Libraries.Wowhead
{
    public class FollowerInfo
    {
        public int follower { get; set; }
        public List<int> abilities { get; set; }
        public int avgilvl { get; set; }
        public int level { get; set; }
        public int quality { get; set; }

        public FollowerInfo(Follower f)
        {
            follower = f.FollowerId.ToInt32();
            abilities = f.Abilities;
            avgilvl = f.ItemLevel;
            level = f.Level;
            quality = f.Quality.ToInt32();
        }
    }
}