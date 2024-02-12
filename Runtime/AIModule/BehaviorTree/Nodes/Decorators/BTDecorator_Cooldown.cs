using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public class BTDecorator_Cooldown : BTDecorator
    {
        

#if UNITY_EDITOR
        protected override string GetDefaultName()
        {
            return "Cooldown";
        }
#endif
    }
}
