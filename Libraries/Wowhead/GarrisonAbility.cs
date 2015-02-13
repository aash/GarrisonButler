using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GarrisonButler.Libraries.Wowhead
{
    [DataContract]
    public class GarrisonAbility
    {
        [DataMember]
        public static List<int> amount1 { get; set; }
        [DataMember]
        public static List<int> amount2 { get; set; }
        [DataMember]
        public static List<int> amount3 { get; set; }
        [DataMember]
        public static List<int> amount4 { get; set; }
        [DataMember]
        public static int category { get; set; }
        [DataMember]
        public static List<int> counters { get; set; }
        [DataMember]
        public static string description { get; set; }
        [DataMember]
        public static List<int> followerclass { get; set; }
        [DataMember]
        public static List<int> hours { get; set; }
        [DataMember]
        public static string icon { get; set; }
        [DataMember]
        public static int id { get; set; }
        [DataMember]
        public static List<int> missionparty { get; set; }
        [DataMember]
        public static string name { get; set; }
        [DataMember]
        public static List<int> race { get; set; }
        [DataMember]
        public static string side { get; set; }
        [DataMember]
        public static bool trait { get; set; }
        [DataMember]
        public static List<int> type { get; set; } 
    }
}