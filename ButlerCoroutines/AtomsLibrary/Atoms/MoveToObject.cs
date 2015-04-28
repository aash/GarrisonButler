#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class MoveToObject : MoveTo
    {
        private readonly uint _objectEntry;
        private readonly List<uint> _objectsEntries;
        private readonly WoWObjectTypeFlag _objectTypeFlag;
        private readonly WoWGameObjectType _gameObjectType;
        private readonly WoWSpellFocus _spellFocus;
        private WoWObject _target; 

        public MoveToObject(WoWGameObjectType objectType, WoWPoint defaultLocation, float precision = 0.0f)
            : base(defaultLocation, precision)
        {
            _gameObjectType = objectType;
        }
        public MoveToObject(uint objectEntry, WoWObjectTypeFlag objectTypeFlag, WoWPoint defaultLocation, float precision = 0.0f)
            : base(defaultLocation, precision)
        {
            _objectEntry = objectEntry;
            _objectTypeFlag = objectTypeFlag;
        }
        public MoveToObject(List<uint> objectsEntries, WoWObjectTypeFlag objectTypeFlag, WoWPoint defaultLocation, float precision = 0.0f)
            : base(defaultLocation, precision)
        {
            _objectsEntries = objectsEntries;
            _objectTypeFlag = objectTypeFlag;
            
            GarrisonButler.Diagnostic("Creating MoveTo with default location {0} (p={1}) and entries:", defaultLocation, precision);
            ObjectDumper.WriteToHb(_objectsEntries, 3);
        }

        public MoveToObject(WoWSpellFocus spellFocus, WoWPoint defaultLocation)
            : base(defaultLocation)
        {
            _spellFocus = spellFocus;
        }

        private List<WoWObject> FindObjects<T>() where T : WoWObject
        {
            var list =
                _objectsEntries != null
                ? ObjectManager.GetObjectsOfTypeFast<T>().Where(o => _objectsEntries.Contains(o.Entry)).ToList()
                : ObjectManager.GetObjectsOfTypeFast<T>().Where(o => o.Entry == _objectEntry).ToList();

            var sorted = list.ToList()
                .ConvertAll(i => i as WoWObject)
                .OrderBy(o => o.Location.Distance(StyxWoW.Me.Location));
            return sorted.ToList(); 
        }

        private List<WoWObject> FindObjects()
        {
            if (_gameObjectType != default(WoWGameObjectType))
            {
                var objects = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => o.SubType == _gameObjectType);
                var sorted = objects.ToList()
                    .ConvertAll(i => i as WoWObject)
                    .OrderBy(o => o.Location.Distance(StyxWoW.Me.Location));
                return sorted.ToList();
            }
            
            if (_spellFocus != default(WoWSpellFocus))
            {
                var objects = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => o.SpellFocus == _spellFocus);
                var sorted = objects.ToList()
                    .ConvertAll(i => i as WoWObject)
                    .OrderBy(o => o.Location.Distance(StyxWoW.Me.Location));
                return sorted.ToList();
            }

            if (_objectEntry != default(uint)
                || (_objectsEntries != null && _objectsEntries.Count != 0))
            {
                switch (_objectTypeFlag)
                {
                    case WoWObjectTypeFlag.Unit:
                        var list = FindObjects<WoWUnit>();
                        list.AddRange(FindObjects<WoWGameObject>());
                        return list;
                    case WoWObjectTypeFlag.GameObject:
                        var list2 = FindObjects<WoWUnit>();
                        list2.AddRange(FindObjects<WoWGameObject>());
                        return list2;
                    case WoWObjectTypeFlag.Corpse:
                        return FindObjects<WoWCorpse>();
                    case WoWObjectTypeFlag.Item:
                        return FindObjects<WoWItem>();
                    case WoWObjectTypeFlag.Player:
                        return FindObjects<WoWPlayer>();
                }
                return FindObjects<WoWObject>();
            }
            return new List<WoWObject>();
        }
        
        /// <summary>
        /// Will start the navigation system and move within interaction range of the NPC.
        /// </summary>
        /// <returns></returns>
        public override async Task Action()
        {
            // If we find the target, then modify the destination to its location
            var objectsFound = FindObjects();
            if (objectsFound.Any())
            {
                if (_target == null || Location != objectsFound.First().Location)
                {
                    GarrisonButler.Diagnostic("[MoveToObject] Refreshing location. old: {0}, new:{1}", _target, objectsFound.First().Location);
                    _target = objectsFound.First();
                    Location.X = objectsFound.First().Location.X;
                    Location.Y = objectsFound.First().Location.Y;
                    Location.Z = objectsFound.First().Location.Z;
                }
            }
            await base.Action();
        }

        public override string Name()
        {
            if (_objectEntry != default(uint))
                return "[MoveToObject|" + _objectEntry + "]";
            if (_objectsEntries != null && _objectsEntries.Count != 0)
                return "[MoveToObject|" + string.Join(",", _objectsEntries.ToArray()) + "]";
            if(_gameObjectType != default(WoWGameObjectType))
                return "[MoveToObject|" + _gameObjectType + "]";
            return "[MoveToObject|" + Location + "]";
        }
    }
}