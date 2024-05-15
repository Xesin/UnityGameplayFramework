using System;

namespace Blackbloard
{
    public struct Key
    {
        private ushort value;

        public Key(ushort value)
        {
            this.value = value;
        }

        public Key(int value)
        {
            this.value = checked((ushort)value);
        }

        public static implicit operator int(Key value)
        {
            return value.value;
        }

        public static implicit operator ushort(Key value)
        {
            return value.value;
        }

        public static implicit operator Key(ushort key)
        {
            return new Key(key);
        }

        public override bool Equals(object obj)
        {
            if(obj is Key otherKey)
                return value == otherKey.value; 
            else return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static Key invalid = new Key(unchecked((ushort)-1));
    }


}
