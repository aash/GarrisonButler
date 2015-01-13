#region

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.Config.OldSettings
{
    public class MailItem : INotifyPropertyChanged
    {
        private
            string _comment;

        private
            uint _itemId;

        private
            string _recipient;

        private
            MailCondition _condition;

        public
            MailItem(uint
                itemId,
                string recipient, MailCondition
                    mailCondition,
                int checkValue,
                string comment = "")
        {
            ItemId = itemId;
            Recipient = recipient;
            _condition = new MailCondition(mailCondition.Condition, checkValue);
            Comment = comment;
        }

        public
            MailItem()
        {
            Condition = new MailCondition();
        }

        public
            uint ItemId
        {
            get { return _itemId; }
            set
            {
                if (value == _itemId) return;
                _itemId = value;
                OnPropertyChanged();
            }
        }

        public
            string Recipient
        {
            get { return _recipient; }
            set
            {
                if (value == _recipient) return;
                _recipient = value;
                OnPropertyChanged();
            }
        }

        public
            MailCondition
            Condition
        {
            get { return _condition; }
            set
            {
                if (value == _condition) return;
                _condition = value;
                OnPropertyChanged();
            }
        }

        public
            int CheckValue
        {
            get { return _condition != null ? _condition.CheckValue : 0; }
            set
            {
                if (value == _condition.CheckValue) return;
                _condition.CheckValue = value;
                OnPropertyChanged();
            }
        }

        public
            bool CanMail
            ()
        {
            return _condition.GetCondition(ItemId);
        }

        public
            async
            Task<IEnumerable<WoWItem>> GetItemsToSend
            ()
        {
            return await _condition.GetItemsOrNull(ItemId);
        }

        public
            string Comment
        {
            get { return _comment; }
            set
            {
                if (value == _comment) return;
                _comment = value;
                OnPropertyChanged();
            }
        }

        public event
            PropertyChangedEventHandler PropertyChanged;

        [
            NotifyPropertyChangedInvocator]
        protected virtual
        void OnPropertyChanged
            ([
                CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        public
            void SetCondition
            (string
                name)
        {
            _condition.Name = name;
            OnPropertyChanged("Condition");
        }

        public
            Objects.MailItem FromOld
            ()
        {
            var mailItem = new Objects.MailItem(ItemId, Recipient, Condition.FromOld(),
                CheckValue,
                Comment);
            return mailItem;
        }
    }
}