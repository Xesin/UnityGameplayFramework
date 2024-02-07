using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    [Serializable]
    public struct BlackboardEntry
    {
        public string entryName;

        private object internalValue;

        public object Value
        {
            get => internalValue;
            set
            {
                if ((valueType.IsPrimitive || valueType.IsValueType || valueType == typeof(string)) && value.GetType() != ValueType) return; // Do not allow primitives to change it's value

                internalValue = value;
                valueType = value.GetType();
            }
        }

        [SerializeField] private string valueTypeQualifiedName;
        private string ValueTypeQualifiedName
        {
            get => valueTypeQualifiedName;
            set
            {
                valueTypeQualifiedName = value;
                valueType = Type.GetType(value, false);
            }
        }

        [SerializeField] private Type valueType;
        public Type ValueType
        {
            get
            {
                if (valueType != null) return valueType;
                if (string.IsNullOrEmpty(ValueTypeQualifiedName)) return null;
                valueType = Type.GetType(ValueTypeQualifiedName, false);
                return valueType;
            }
            private set
            {
                valueType = value;
                if (value == null) return;
                valueTypeQualifiedName = value.AssemblyQualifiedName;
            }
        }

        public bool IsValid()
        {
            return entryName != null && ValueType != null;
        }

        public override bool Equals(object obj)
        {
            if (obj is not BlackboardEntry entry) return false;

            return entry.entryName == entryName && entry.ValueType == ValueType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(entryName, ValueType);
        }
    }

    public class BlackboardData : ScriptableObject
    {
        public BlackboardEntry[] Keys = new BlackboardEntry[0];

        public int Count => Keys.Length;

        public string GetKeyName(Blackbloard.Key key)
        {
            var blackBoardEntry = GetKey(key);
            return blackBoardEntry.IsValid() ? blackBoardEntry.entryName : null;
        }

        public Type GetKeyType(Blackbloard.Key key)
        {
            var blackboardEntry = GetKey(key);
            return blackboardEntry.IsValid() ? blackboardEntry.ValueType : null;
        }

        public Blackbloard.Key GetKeyID(string name)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Keys[i].entryName == name)
                {
                    return new Blackbloard.Key(i);
                }
            }

            return Blackbloard.Key.invalid;
        }

        public BlackboardEntry GetKey(Blackbloard.Key keyID)
        {
            if (keyID != Blackbloard.Key.invalid)
            {
                return Keys[keyID];
            }

            return default;
        }

        public bool IsValid()
        {
            return true;
        }
    }
}
