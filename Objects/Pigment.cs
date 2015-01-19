using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GarrisonButler.Objects
{
    public class Pigment
    {
        private static List<Pigment> _woDPigments;
        private static List<Pigment> _allPigments;

        public Pigment(uint itemId, List<SourcePigment> milledFrom)
        {
            Id = itemId;
            MilledFrom = milledFrom;
        }

        public Pigment()
        {
            Id = 0;
            MilledFrom = new List<SourcePigment>();
        }


        [XmlArrayItem("Source", typeof(SourcePigment))]
        [XmlArray("SourceForPigments")]
        public List<SourcePigment> MilledFrom { get; set; }

        [XmlAttribute("ItemId")]
        public uint Id { get; set; }


        [XmlIgnore]
        public static List<Pigment> WoDPigments
        {
            get
            {
                return _woDPigments ?? (_woDPigments = new List<Pigment>
                {
                    new Pigment(114931, new List<SourcePigment>
                    {
                        new SourcePigment(109125, "Fireweed"), // 
                        new SourcePigment(109124, "Frostweed"), // 
                        new SourcePigment(109126, "Gorgrond Flytrap"), // 
                        new SourcePigment(109128, "Nagrand Arrowbloom"), // 
                        new SourcePigment(109127, "Starflower"), // 
                        new SourcePigment(109129, "Talador Orchid")  // 
                    })
                });
            }
            set
            {
                _woDPigments = value;
                
            }
        }

        [XmlIgnore]
        public static List<Pigment> AllPigments
        {
            get
            {
                return _allPigments ?? (_allPigments = WoDPigments);
            }
            set { _allPigments = value; }
        }
    }

    public class SourcePigment : INotifyPropertyChanged
    {
        private uint _itemId;
        private bool _activated;
        private string _name;

        public SourcePigment(uint itemId, string name, bool activated = false)
        {
            this._itemId = itemId;
            _name = name;
            this._activated = activated;
        }
        public SourcePigment()
        {
            this._itemId = 0;
            _name = "";
            this._activated = false;
        }

        [XmlAttribute("ItemId")]
        public uint ItemId
        {
            get { return _itemId; }
            set { _itemId = value; }
        }

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
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}