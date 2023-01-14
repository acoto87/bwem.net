//////////////////////////////////////////////////////////////////////////
//
// This file is part of the BWEM Library.
// BWEM is free software, licensed under the MIT/X11 License.
// A copy of the license is provided with the library in the LICENSE file.
// Copyright (c) 2015, 2017, Igor Dimitrijevic
//
//////////////////////////////////////////////////////////////////////////

using System;

namespace BWEM.NET
{
    public readonly struct GroupId : IEquatable<GroupId>, IComparable<GroupId>
    {
        public readonly short Value;

        public GroupId(short value)
        {
            Value = value;
        }

        public int CompareTo(GroupId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(GroupId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GroupId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(GroupId aid1, GroupId aid2)
        {
            return aid1.Equals(aid2);
        }

        public static bool operator !=(GroupId aid1, GroupId aid2)
        {
            return !(aid1 == aid2);
        }

        public static bool operator <(GroupId left, GroupId right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(GroupId left, GroupId right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(GroupId left, GroupId right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(GroupId left, GroupId right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static implicit operator GroupId(short value)
        {
            return new GroupId(value);
        }

        public static implicit operator GroupId(int value)
        {
            return new GroupId((short)value);
        }
    }
}
