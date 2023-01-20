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
    public readonly struct Altitude : IEquatable<Altitude>, IComparable<Altitude>
    {
        public readonly short Value;

        public Altitude(short value)
        {
            Value = value;
        }

        public int CompareTo(Altitude other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(Altitude other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Altitude other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(Altitude aid1, Altitude aid2)
        {
            return aid1.Equals(aid2);
        }

        public static bool operator !=(Altitude aid1, Altitude aid2)
        {
            return !(aid1 == aid2);
        }

        public static bool operator <(Altitude left, Altitude right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Altitude left, Altitude right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Altitude left, Altitude right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Altitude left, Altitude right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static Altitude operator +(Altitude left, Altitude right)
        {
            return new Altitude((short)(left.Value + right.Value));
        }

        public static Altitude operator -(Altitude left, Altitude right)
        {
            return new Altitude((short)(left.Value - right.Value));
        }

        public static Altitude operator *(Altitude left, Altitude right)
        {
            return new Altitude((short)(left.Value * right.Value));
        }

        public static Altitude operator /(Altitude left, Altitude right)
        {
            return new Altitude((short)(left.Value / right.Value));
        }

        public static implicit operator Altitude(short value)
        {
            return new Altitude(value);
        }

        public static implicit operator Altitude(int value)
        {
            return new Altitude((short)value);
        }
    }
}
