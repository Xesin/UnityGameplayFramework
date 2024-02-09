using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using Xesin.GameplayFramework.AI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameplayFramework.AI
{


    public class BehaviorTreeView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<BehaviorTreeView, GraphView.UxmlTraits> { }

        private BehaviorTree tree;

        public BehaviorTreeView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/BehaviorTreeEditor.uss");
            styleSheets.Add(styleSheet);
        }

        internal void PopulateView(BehaviorTree tree)
        {
            this.tree = tree;

            graphViewChanged -= OnGraphViewChanged;

            DeleteElements(graphElements);

            graphViewChanged += OnGraphViewChanged;

            var root = CreateRootView();
            tree.compositeNodes.ForEach(CreateNodeView);
            tree.compositeNodes.ForEach(CreateEdges);

            CenterGraphOnNode(root);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if(graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if(elem is BTNodeView nodeView)
                    {
                        tree.DeleteNode(nodeView.node);
                    }

                    Edge edge = elem as Edge;
                    if(edge != null)
                    {
                        BTNodeView inNode = edge.input.node as BTNodeView;
                        if (edge.output.node is BTRootNodeView)
                        {
                            tree.RemoveChild(null, tree.GetChildByID(inNode.node.nodeId));
                        }
                        else
                        {
                            BTNodeView outNode = edge.output.node as BTNodeView;
                            tree.RemoveChild(tree.GetChildByID(outNode.node.nodeId), tree.GetChildByID(inNode.node.nodeId));
                        }
                    }
                });
            }

            if(graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    BTNodeView inNode = edge.input.node as BTNodeView;
                    if (edge.output.node is BTRootNodeView)
                    {
                        tree.AddChild(null, tree.GetChildByID(inNode.node.nodeId));
                    }
                    else
                    {
                        BTNodeView outNode = edge.output.node as BTNodeView;
                        tree.AddChild(tree.GetChildByID(outNode.node.nodeId), tree.GetChildByID(inNode.node.nodeId));
                    }
                });
            }
            return graphViewChange;
        }

        private BTRootNodeView CreateRootView()
        {
            BTRootNodeView nodeView = new BTRootNodeView(tree);
            AddElement(nodeView);
            return nodeView;
        }

        private void CreateNodeView(BTCompositeChild compositeChild)
        {
            BTNode node = compositeChild.childTask ? compositeChild.childTask : compositeChild.childComposite;
            BTNodeView nodeView = null;
            switch (node)
            {
                case BTTask:
                    nodeView = new BTTaskNodeView(node);
                    break;
                case BTAuxiliaryNode:
                    nodeView = new BTNodeView(node);
                    break;
                case BTComposite:
                    nodeView = new BTCompositeNodeView(node);
                    break;
                default:
                    break;
            }
            AddElement(nodeView);
        }

        private void CreateEdges(BTCompositeChild child)
        {
            if(tree.rootNode == child.GetNode())
            {
                BTRootNodeView parentView = GetNodeByGuid("BTRoot") as BTRootNodeView;
                BTNodeView childView = GetNodeByGuid(child.GetNode().nodeId) as BTNodeView;

                Edge edge = parentView.output.ConnectTo(childView.input);
                AddElement(edge);
            }


            if(child.childComposite)
            {
                BTComposite composite = child.childComposite;
                composite.children.ForEach(cc =>
                {
                    BTNode childNode = cc.GetNode();

                    BTNodeView parentView = GetNodeByGuid(composite.nodeId) as BTNodeView;
                    BTNodeView childView = GetNodeByGuid(childNode.nodeId) as BTNodeView;

                    Edge edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge);
                });
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            DropdownMenuAction.Status status = tree ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

            {
                var types = TypeCache.GetTypesDerivedFrom<BTTask>();

                foreach (var type in types)
                {
                    evt.menu.AppendAction($"Add/Tasks/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                }
            }

            {
                var types = TypeCache.GetTypesDerivedFrom<BTService>();

                foreach (var type in types)
                {
                    evt.menu.AppendAction($"Add/Service/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                }
            }

            {
                var types = TypeCache.GetTypesDerivedFrom<BTDecorator>();

                foreach (var type in types)
                {
                    evt.menu.AppendAction($"Add/Decorator/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                }
            }

            {
                var types = TypeCache.GetTypesDerivedFrom<BTComposite>();

                foreach (var type in types)
                {
                    evt.menu.AppendAction($"Add/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var returnPorts = ports
                .Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node);
            if (startPort.node is BTRootNodeView)
            {
                returnPorts = returnPorts.Where(endPort => endPort.node is BTCompositeNodeView);
            }
            else if(startPort.node is not BTCompositeNodeView)
            {
                returnPorts = returnPorts.Where(endport => endport.node is not BTRootNodeView);
            }
            return returnPorts
                .ToList();
        }

        private void CreateNode(System.Type type)
        {
            var node = tree.CreateNode(type);
            CreateNodeView(node);
        }

        public void CenterGraphOnNode(Node node, bool selectNode = false)
        {
            schedule.Execute(() =>
            {
                if (tree == null) return;
                if (node == null) return;
                ClearSelection();
                AddToSelection(node);
                FrameSelection();
                if (selectNode) return;
                ClearSelection();
            });
        }
    }
}
