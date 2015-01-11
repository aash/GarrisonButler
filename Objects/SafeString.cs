using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GarrisonButler.Objects
{
    public class SafeString: INotifyPropertyChanged
    {
        // props
        private string _value;

        [XmlText()]
        public string Value
        {
            get { return _value; }
            set
            {
                this._value = value;
                OnPropertyChanged();
            }
        }

        public SafeString(string value)
        {
            Value = value;
        }

        public SafeString()
        {
            Value = "";
        }

        //public static implicit operator string(SafeString d)
        //{
        //    if (d == null)
        //        d = new SafeString(); 
        //    return d.Value;
        //}

        //public static implicit operator SafeString(string d)
        //{
        //    return new SafeString(d);
        //}

        public static string ToStringSafe()
        {
            return "****************";
        }

        public override string ToString()
        {
            return Value;
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
