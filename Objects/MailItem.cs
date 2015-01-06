using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Bots.Professionbuddy.Components;
using JetBrains.Annotations;

namespace GarrisonButler.Objects
{
    public class MailItem : INotifyPropertyChanged
    {
        private int _itemId;
        private string _recipient;
        private string _comment;
        public int ItemId
        {
            get { return this._itemId; }
            set
            {
                if (value != this._itemId)
                {
                    this._itemId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipient
        {
            get { return this._recipient; }
            set
            {
                if (value != this._recipient)
                {
                    this._recipient = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Comment
        {
            get { return this._comment; }
            set
            {
                if (value != this._comment)
                {
                    this._comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public MailItem(int itemId, string recipient, string comment = "")
        {
            ItemId = itemId;
            Recipient = recipient;
            Comment = comment;
        }
        public MailItem()
        {

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
