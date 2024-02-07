using System.Collections.Generic;
using System.Linq;

namespace Xesin.GameplayFramework.AI
{
    public enum BTDecoratorLogic
    {
        Invalid,
        Test,
        And,
        Or,
        Not
    }

    public enum BTChildIndex
    {
        FirstNode,
        TaskNode,
    }

    public struct BTCompositeChild
    {

        public BTCompositeNode ChildComposite;

        public BTTaskNode ChildTask;

        public List<BTDecorator> Decorators;

        public List<BTDecoratorLogic> decoratorsOps;
    };

    public class BTCompositeNode : BTNode
    {
        public const int NOT_INITIALIZED_CHILD = -1;
        public const int RETURN_TO_PARENT_CHILD = -2;

        public List<BTService> services = new List<BTService>();
        public List<BTDecorator> decorators = new List<BTDecorator>();

        public List<BTCompositeChild> children = new List<BTCompositeChild>();

        private ushort lastExecutionIndex;

        public void InitializeComposite(ushort inLastExecutionIndex)
        {
            lastExecutionIndex = inLastExecutionIndex;
        }

        public int GetChildIndex(BTNode childNode)
        {
            for (int childIndex = 0; childIndex < children.Count; childIndex++)
            {
                if (children[childIndex].ChildComposite == childNode ||
                    children[childIndex].ChildTask == childNode)
                {
                    return childIndex;
                }
            }

            return RETURN_TO_PARENT_CHILD;
        }

        public BTNode GetChildNode(int index)
        {
            return children.IsValidIndex(index) ? (
                children[index].ChildComposite ? 
                    children[index].ChildComposite : 
                    children[index].ChildTask) : null;
        }

        public ushort GetChildExecutionIndex(int index, BTChildIndex childMode = BTChildIndex.TaskNode)
        {
            BTNode childNode = GetChildNode(index);

            if(childNode)
            {
                int offset = 0;

                if(childMode == BTChildIndex.FirstNode)
                {
                    offset += children[index].Decorators.Count;

                    if (children[index].ChildTask)
                    {
                        offset += children[index].ChildTask.services.Count;
                    }
                }

                return checked((ushort) (childNode.GetExecutionIndex() - offset));
            }

            return (ushort)(lastExecutionIndex + 1);
        }
    }
}
