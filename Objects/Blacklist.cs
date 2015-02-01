using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Objects
{
    class Blacklist
    {
        private static HashSet<int> HashGuidBlacklisted
        {
            get { return _hashGuidBlacklisted ?? (_hashGuidBlacklisted = new HashSet<int>()); }
            set { _hashGuidBlacklisted = value; }
        }

        private static Blacklist _instance;
        private static HashSet<int> _hashGuidBlacklisted;

        public static Blacklist Instance
        {
            get { return _instance ?? (_instance = new Blacklist()); }            
        }

        private Blacklist()
        {
            // Initialization of blacklist
            HashGuidBlacklisted = new HashSet<int>();
        }

        public static bool IsBlacklisted(WoWGameObject gameObject)
        {
            var isBlacklisted = HashGuidBlacklisted.Contains(gameObject.Guid.GetHashCode());
            GarrisonButler.Diagnostic("Object {0}, blacklisted? {1}", gameObject.Name, isBlacklisted);
            return isBlacklisted;
        }

        public static void Add(WoWGameObject gameObject)
        {
            GarrisonButler.Diagnostic("Adding object to blacklist: {0}", gameObject.Name);
            HashGuidBlacklisted.Add(gameObject.Guid.GetHashCode());
        }
        public static void Remove(WoWGameObject gameObject)
        {
            GarrisonButler.Diagnostic("Removing object from blacklist: {0}", gameObject.Name);
            HashGuidBlacklisted.Remove(gameObject.Guid.GetHashCode());
        }

        public static void Clear()
        {
            HashGuidBlacklisted.Clear();
        }
    }
}
