using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GarrisonButler.Objects
{
    public class SafeString : INotifyPropertyChanged, IComparable
    {
        // props
        private string _value;

        [XmlText]
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public int Length
        {
            get { return Value.Length; }
        }

        public SafeString(string value)
        {
            Value = value;
        }

        public SafeString()
        {
            Value = "None";
        }


        public static string ToStringSafe()
        {
            return "****************";
        }

        public override string ToString()
        {
            return Value;
        }

        public int CompareTo(Object to)
        {
            var safeString = to as SafeString;
            return safeString != null 
                ? String.Compare(Value, safeString.Value, StringComparison.Ordinal) 
                : Value.CompareTo(to);
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