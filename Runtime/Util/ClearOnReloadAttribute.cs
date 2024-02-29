using System;

namespace Xesin.GameplayFramework.Domain
{
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property)]
    public class ClearOnReloadAttribute : Attribute
    {
        public readonly object ValueOnReload;
        public readonly bool CreateNewInstance;

        public ClearOnReloadAttribute()
        {
            ValueOnReload = null;
            CreateNewInstance = false;
        }

        public ClearOnReloadAttribute(object resetValue)
        {
            ValueOnReload = resetValue;
            CreateNewInstance = false;
        }

        public ClearOnReloadAttribute(bool newInstance)
        {
            ValueOnReload = null;
            CreateNewInstance = newInstance;
        }
    }
}
