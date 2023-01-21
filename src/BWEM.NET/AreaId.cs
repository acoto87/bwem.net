// Original work Copyright (c) 2015, 2017, Igor Dimitrijevic
// Modified work Copyright (c) 2017-2018 OpenBW Team

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
    public readonly struct AreaId : IEquatable<AreaId>, IComparable<AreaId>
    {
        public readonly short Value;

        public AreaId(short value)
        {
            Value = value;
        }

        public int CompareTo(AreaId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(AreaId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is AreaId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(AreaId aid1, AreaId aid2)
        {
            return aid1.Equals(aid2);
        }

        public static bool operator !=(AreaId aid1, AreaId aid2)
        {
            return !(aid1 == aid2);
        }

        public static bool operator <(AreaId left, AreaId right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(AreaId left, AreaId right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(AreaId left, AreaId right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(AreaId left, AreaId right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static implicit operator AreaId(short value)
        {
            return new AreaId(value);
        }

        public static implicit operator AreaId(int value)
        {
            return new AreaId((short)value);
        }
    }
}
