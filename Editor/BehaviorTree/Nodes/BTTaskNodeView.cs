using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTTaskNodeView : BTNodeView
    {
        public BTTaskNodeView(BTNode node) : base(node)
        {
            titleContainer.style.backgroundImage = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/TaskGradient.png"));
        }

        protected override void CreateInputNodes()
        {
            HideOutputNode();

            input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = string.Empty;
            inputContainer.Add(input);
        }
    }
}
