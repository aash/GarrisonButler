#region

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Serialization;

// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable All

#endregion

namespace GarrisonButler.Libraries
{
    /// <summary>
    ///     Serializable version of the System.Version class.
    /// </summary>
    [Serializable]
    public class ModuleVersion : ICloneable, IComparable
    {
        private int _build;
        private int _major;
        private int _minor;
        private int _revision;

        /// <summary>
        ///     Creates a new <see cref="ModuleVersion" /> instance.
        /// </summary>
        public ModuleVersion()
        {
            _build = -1;
            _revision = -1;
            _major = 0;
            _minor = 0;
        }

        /// <summary>
        ///     Creates a new <see cref="ModuleVersion" /> instance.
        /// </summary>
        /// <param name="version">Version.</param>
        public ModuleVersion(string version)
        {
            _build = -1;
            _revision = -1;
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            var chArray1 = new char[] {'.'};
            var textArray1 = version.Split(chArray1);
            var num1 = textArray1.Length;
            if ((num1 < 2) || (num1 > 4))
            {
                throw new ArgumentException("Arg_VersionString");
            }
            _major = int.Parse(textArray1[0], CultureInfo.InvariantCulture);
            if (_major < 0)
            {
                throw new ArgumentOutOfRangeException("version", @"ArgumentOutOfRange_Version");
            }
            _minor = int.Parse(textArray1[1], CultureInfo.InvariantCulture);
            if (_minor < 0)
            {
                throw new ArgumentOutOfRangeException("version", @"ArgumentOutOfRange_Version");
            }
            num1 -= 2;
            if (num1 <= 0) return;
            _build = int.Parse(textArray1[2], CultureInfo.InvariantCulture);
            if (_build < 0)
            {
                throw new ArgumentOutOfRangeException("version", @"ArgumentOutOfRange_Version");
            }
            num1--;
            if (num1 <= 0) return;
            _revision = int.Parse(textArray1[3], CultureInfo.InvariantCulture);
            if (_revision < 0)
            {
                throw new ArgumentOutOfRangeException("version", @"ArgumentOutOfRange_Version");
            }
        }

        /// <summary>
        ///     Creates a new <see cref="ModuleVersion" /> instance.
        /// </summary>
        /// <param name="major">Major.</param>
        /// <param name="minor">Minor.</param>
        public ModuleVersion(int major, int minor)
        {
            _build = -1;
            _revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException("major", @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException("minor", @"ArgumentOutOfRange_Version");
            }
            _major = major;
            _minor = minor;
            _major = major;
        }

        /// <summary>
        ///     Creates a new <see cref="ModuleVersion" /> instance.
        /// </summary>
        /// <param name="major">Major.</param>
        /// <param name="minor">Minor.</param>
        /// <param name="build">Build.</param>
        public ModuleVersion(int major, int minor, int build)
        {
            _build = -1;
            _revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException("major", @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException("minor", @"ArgumentOutOfRange_Version");
            }
            if (build < 0)
            {
                throw new ArgumentOutOfRangeException("build", @"ArgumentOutOfRange_Version");
            }
            _major = major;
            _minor = minor;
            _build = build;
        }

        /// <summary>
        ///     Creates a new <see cref="ModuleVersion" /> instance.
        /// </summary>
        /// <param name="major">Major.</param>
        /// <param name="minor">Minor.</param>
        /// <param name="build">Build.</param>
        /// <param name="revision">Revision.</param>
        public ModuleVersion(int major, int minor, int build, int revision)
        {
            _build = -1;
            _revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException("major", @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException("minor", @"ArgumentOutOfRange_Version");
            }
            if (build < 0)
            {
                throw new ArgumentOutOfRangeException("build", @"ArgumentOutOfRange_Version");
            }
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException("revision", @"ArgumentOutOfRange_Version");
            }
            _major = major;
            _minor = minor;
            _build = build;
            _revision = revision;
        }

        #region ICloneable Members

        /// <summary>
        ///     Clones this instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var version1 = new ModuleVersion();
            version1._major = _major;
            version1._minor = _minor;
            version1._build = _build;
            version1._revision = _revision;
            return version1;
        }

        #endregion

        #region IComparable Members

        /// <summary>
        ///     Compares to.
        /// </summary>
        /// <param name="version">Obj.</param>
        /// <returns></returns>
        public int CompareTo(object version)
        {
            if (version == null)
            {
                return 1;
            }
            if (!(version is ModuleVersion))
            {
                throw new ArgumentException("Arg_MustBeVersion");
            }
            var version1 = (ModuleVersion) version;
            if (_major != version1.Major)
            {
                if (_major > version1.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (_minor != version1.Minor)
            {
                if (_minor > version1.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (_build != version1.Build)
            {
                if (_build > version1.Build)
                {
                    return 1;
                }
                return -1;
            }
            if (_revision == version1.Revision)
            {
                return 0;
            }
            if (_revision > version1.Revision)
            {
                return 1;
            }
            return -1;
        }

        #endregion

        /// <summary>
        ///     Gets the major.
        /// </summary>
        /// <value></value>
        [XmlAttribute("Major")]
        public int Major
        {
            get { return _major; }
            set { _major = value; }
        }

        /// <summary>
        ///     Gets the minor.
        /// </summary>
        /// <value></value>
        [XmlAttribute("Minor")]
        public int Minor
        {
            get { return _minor; }
            set { _minor = value; }
        }

        /// <summary>
        ///     Gets the build.
        /// </summary>
        /// <value></value>
        [XmlAttribute("Build")]
        public int Build
        {
            get { return _build; }
            set { _build = value; }
        }

        /// <summary>
        ///     Gets the revision.
        /// </summary>
        /// <value></value>
        [XmlAttribute("Revision")]
        public int Revision
        {
            get { return _revision; }
            set { _revision = value; }
        }

        /// <summary>
        ///     Equalss the specified obj.
        /// </summary>
        /// <param name="obj">Obj.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if ((this as object) == null)
                return false;

            if (obj == null) 
                return false;

            if (!(obj is ModuleVersion))
                return false;

            var version1 = (ModuleVersion) obj;
            if (((_major == version1.Major) && (_minor == version1.Minor)) && (_build == version1.Build) &&
                (_revision == version1.Revision))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets the hash code.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var num1 = 0;
            num1 |= ((_major & 15) << 0x1c);
            num1 |= ((_minor & 0xff) << 20);
            num1 |= ((_build & 0xff) << 12);
            return (num1 | _revision & 0xfff);
        }

        /// <summary>
        ///     Operator ==s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator ==(ModuleVersion v1, ModuleVersion v2)
        {
            if ((v1 as object) == null)
                return v2 == null;

            if ((v2 as object) == null)
                return false;

            return v1.Equals(v2);
        }

        /// <summary>
        ///     Operator &gt;s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator >(ModuleVersion v1, ModuleVersion v2)
        {
            return (v2 < v1);
        }

        /// <summary>
        ///     Operator &gt;=s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator >=(ModuleVersion v1, ModuleVersion v2)
        {
            return (v2 <= v1);
        }

        /// <summary>
        ///     Operator !=s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator !=(ModuleVersion v1, ModuleVersion v2)
        {
            if ((v1 as object) == null)
                return (v2 as object) != null;
            if ((v2 as object) == null)
                return true;
            return (!v1.Equals(v2));
        }

        /// <summary>
        ///     Operator &lt;s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator <(ModuleVersion v1, ModuleVersion v2)
        {
            if ((v1 as object) == null)
            {
                throw new ArgumentNullException("v1");
            }
            return (v1.CompareTo(v2) < 0);
        }

        /// <summary>
        ///     Operator &lt;=s the specified v1.
        /// </summary>
        /// <param name="v1">V1.</param>
        /// <param name="v2">V2.</param>
        /// <returns></returns>
        public static bool operator <=(ModuleVersion v1, ModuleVersion v2)
        {
            if ((v1 as object) == null)
                return !((v2 as object) == null);
            return (v1.CompareTo(v2) <= 0);
        }

        /// <summary>
        ///     Toes the string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_build == -1)
            {
                return ToString(2);
            }
            if (_revision == -1)
            {
                return ToString(3);
            }
            return ToString(4);
        }

        /// <summary>
        ///     Toes the string.
        /// </summary>
        /// <param name="fieldCount">Field count.</param>
        /// <returns></returns>
        public string ToString(int fieldCount)
        {
            object[] objArray1;
            switch (fieldCount)
            {
                case 0:
                {
                    return string.Empty;
                }
                case 1:
                {
                    return (_major.ToString(CultureInfo.CurrentCulture));
                }
                case 2:
                {
                    return (_major + "." + _minor);
                }
            }
            if (_build == -1)
            {
                throw new ArgumentException(string.Format("ArgumentOutOfRange_Bounds_Lower_Upper {0},{1}", "0", "2"),
                    "fieldCount");
            }
            if (fieldCount == 3)
            {
                objArray1 = new object[] {_major, ".", _minor, ".", _build};
                return string.Concat(objArray1);
            }
            if (_revision == -1)
            {
                throw new ArgumentException(string.Format("ArgumentOutOfRange_Bounds_Lower_Upper {0},{1}", "0", "3"),
                    "fieldCount");
            }
            if (fieldCount == 4)
            {
                objArray1 = new object[] {_major, ".", _minor, ".", _build, ".", _revision};
                return string.Concat(objArray1);
            }
            throw new ArgumentException(string.Format("ArgumentOutOfRange_Bounds_Lower_Upper {0},{1}", "0", "4"),
                "fieldCount");
        }
    }
}