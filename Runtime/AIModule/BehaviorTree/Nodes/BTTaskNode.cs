using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTTaskNode : BTNode
    {
        public List<BTService> services = new List<BTService>();
    }
}
