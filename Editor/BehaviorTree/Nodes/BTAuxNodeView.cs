using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTAuxNodeView : BTNodeView
    {
        public BTAuxNodeView(BTNode node) : base(node, "Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeAuxNode.uxml")
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeChildNode.uss");
            styleSheets.Add(styleSheet);
            style.position = Position.Relative;

            titleContainer.style.backgroundImage = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/DecoratorGradient.png"));
        }

        protected override void CreateInputNodes()
        {

        }

        protected override void CreateOutputNodes()
        {

        }

        public override void SetPosition(Rect newPos)
        {
            
        }
    }
}
