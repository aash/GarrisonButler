#region

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.Objects
{
    public class MailItem : INotifyPropertyChanged
    {
        private uint _itemId;
        private SafeString _recipient;
        private MailCondition _condition;
        private string _comment;

        public MailItem(uint itemId, string recipient, MailCondition mailCondition, int checkValue, string comment = "")
        {
            ItemId = itemId;
            _recipient = new SafeString(recipient);
            _condition = new MailCondition(mailCondition.Condition, checkValue);
            Comment = comment;
        }

        public MailItem()
        {
            Condition = new MailCondition();
        }

        [XmlAttribute("ItemID")]
        public uint ItemId
        {
            get { return _itemId; }
            set
            {
                if (value == _itemId) return;
                _itemId = value;
                OnPropertyChanged();
            }
        }

        //[XmlText()]
        public SafeString Recipient
        {
            get { return _recipient; }
            set
            {
                if (value == _recipient) return;
                _recipient = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("Condition")]
        public MailCondition Condition
        {
            get { return _condition; }
            set
            {
                if (value == _condition) return;
                _condition = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int CheckValue
        {
            get { return _condition != null ? _condition.CheckValue : 0; }
            set
            {
                if (value == _condition.CheckValue) return;
                _condition.CheckValue = value;
                OnPropertyChanged();
            }
        }

        public bool CanMail()
        {
            return _condition.GetCondition(ItemId);
        }

        public async Task<IEnumerable<WoWItem>> GetItemsToSend()
        {
            return await _condition.GetItemsOrNull(ItemId);
        }

        //[XmlText()]
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value == _comment) return;
                _comment = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        public void SetCondition(string name)
        {
            _condition.Name = name;
            OnPropertyChanged("Condition");
        }
    }
}