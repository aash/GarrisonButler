#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Bots.Professionbuddy.Components;
using JetBrains.Annotations;
using Styx;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.Objects
{
    public class MailItem : INotifyPropertyChanged
    {
        private string _comment;
        private uint _itemId;
        private string _recipient;
        private MailCondition _condition;

        public MailItem(uint itemId, string recipient, MailCondition mailCondition, int checkValue, string comment = "")
        {
            ItemId = itemId;
            Recipient = recipient;
            _condition = new MailCondition(mailCondition.Condition,checkValue);
            Comment = comment;
        }

        public MailItem()
        {
        }

        public uint ItemId
        {
            get { return _itemId; }
            set
            {
                if (value != _itemId)
                {
                    _itemId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipient
        {
            get { return _recipient; }
            set
            {
                if (value != _recipient)
                {
                    _recipient = value;
                    OnPropertyChanged();
                }
            }
        }
        public MailCondition Condition
        {
            get { return _condition; }
            set
            {
                if (value != _condition)
                {
                    _condition = value;
                    OnPropertyChanged();
                }
            }
        }
        public int CheckValue
        {
            get
            {
                if (_condition != null)
                    return _condition.CheckValue;
                return 0;
;
            }
            set
            {
                if (value != _condition.CheckValue)
                {
                    _condition.CheckValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanMail()
        {
            return _condition.GetCondition(ItemId);
        }

        public IEnumerable<WoWItem> GetItemsToSend()
        {
            return _condition.GetItemsOrNull(ItemId);
        }
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value != _comment)
                {
                    _comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}