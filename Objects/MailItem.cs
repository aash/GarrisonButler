#region

using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

#endregion

namespace GarrisonButler.Objects
{
    public class MailItem : INotifyPropertyChanged
    {
        private string _comment;
        private int _itemId;
        private string _recipient;

        public MailItem(int itemId, string recipient, string comment = "")
        {
            ItemId = itemId;
            Recipient = recipient;
            Comment = comment;
        }

        public MailItem()
        {
        }

        public int ItemId
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