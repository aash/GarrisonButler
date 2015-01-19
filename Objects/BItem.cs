using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Styx.XmlEngine;

namespace GarrisonButler.Objects
{
    public class BItem : INotifyPropertyChanged
    {
        private bool _activated;

        public BItem(uint itemId, string name, bool activated = false)
        {
            ItemId = itemId;
            Name = name;
            _activated = activated;
        }

        public BItem()
        {
            ItemId = 0;
            Name = "";
            _activated = false;
        }

        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

        [XmlAttribute("Activated")]
        public bool Activated
        {
            get { return _activated; }
            set
            {
                _activated = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}