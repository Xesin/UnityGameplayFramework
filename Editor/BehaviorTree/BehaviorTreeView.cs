using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{


    public class BehaviorTreeView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<BehaviorTreeView, GraphView.UxmlTraits> { }

        private BehaviorTree tree;

        public Vector2 CachedMousePosition { get; private set; }

        public BehaviorTreeView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeEditor.uss");
            styleSheets.Add(styleSheet);
        }

        void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            CachedMousePosition = evt.mousePosition;
        }

        internal void PopulateView(BehaviorTree tree)
        {
            this.tree = tree;

            graphViewChanged -= OnGraphViewChanged;

            DeleteElements(graphElements);

            graphViewChanged += OnGraphViewChanged;

            var root = CreateRootView();


            tree.compositeNodes.ForEach(n =>
            {
                var parent = CreateNodeView(n.GetNode(), false);
            });

            for (int i = 0; i < tree.compositeNodes.Count; i++)
            {
                var child = tree.compositeNodes[i];
                if(child.childComposite)
                {
                    var children = child.childComposite.children;
                    for (int j = 0; j < children.Count; j++)
                    {
                        var match = children.FirstOrDefault(c => c.GetNode() == tree.compositeNodes[i].GetNode());
                        if(match != null)
                        {
                            int index = tree.compositeNodes.IndexOf(match);
                            children[index] = tree.compositeNodes[i];
                        }
                    }
                }
            }

            tree.compositeNodes.ForEach(n =>
            {
                var parent = GetNodeByGuid(n.GetNode().nodeId);
                n.Decorators.ForEach(decorator =>
                    CreateNodeView(decorator, false, parent as BTNodeView)
                );
            });
            tree.compositeNodes.ForEach(CreateEdges);

            CenterGraphOnNode(root);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if (elem is BTNodeView nodeView)
                    {
                       
                        tree.DeleteNode(nodeView.node);
                        nodes.ForEach(n =>
                        {
                            if (n is BTNodeView nodeView)
                            {
                                nodeView.UpdateView();
                            }
                        });
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
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
                            if (outNode is BTCompositeNodeView bTCompositeNodeViewOut)
                            {
                                (bTCompositeNodeViewOut.node as BTComposite).SortChildren();
                            }
                            outNode.UpdateView();
                        }
                        if (inNode is BTCompositeNodeView bTCompositeNodeView)
                        {
                            inNode.node.SetParent(null);
                            (bTCompositeNodeView.node as BTComposite).SortChildren();
                        }
                        inNode.UpdateView();
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
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
                        if (outNode is BTCompositeNodeView bTCompositeNodeViewOut)
                        {
                            (bTCompositeNodeViewOut.node as BTComposite).SortChildren();
                        }
                        outNode.UpdateView();
                    }
                    if(inNode is BTCompositeNodeView bTCompositeNodeView)
                    {
                        (bTCompositeNodeView.node as BTComposite).SortChildren();
                    }
                    inNode.UpdateView();
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

        private BTNodeView CreateNodeView(BTNode node, bool setPosition, BTNodeView parent = null)
        {
            BTNodeView nodeView = null;
            switch (node)
            {
                case BTTask:
                    nodeView = new BTTaskNodeView(node);
                    break;
                case BTDecorator:
                    {
                        var auxNodeView = new BTAuxNodeView(node);
                        if (parent != null)
                        {
                            parent.AddDecorator(auxNodeView);
                            parent.UpdateView(true);
                        }
                        break;
                    }
                case BTService:
                    {
                        var auxNodeView = new BTAuxNodeView(node);
                        if (parent != null)
                        {
                            parent.AddService(auxNodeView);
                            parent.UpdateView(true);
                        }
                        break;
                    }
                case BTComposite:
                    nodeView = new BTCompositeNodeView(node);
                    break;
                default:
                    break;
            }

            if (nodeView == null) return null;
            if (setPosition)
            {
                var graphMousePosition = contentViewContainer.WorldToLocal(CachedMousePosition);
                var rect = nodeView.contentRect;
                rect.x = graphMousePosition.x;
                rect.y = graphMousePosition.y;
                nodeView.SetPosition(rect);
            }

            if (node is not BTAuxiliaryNode)
                AddElement(nodeView);

            if (node is BTTask task)
            {
                task.services.ForEach(n => CreateNodeView(n, false));
            }

            if (node is BTComposite compNode)
            {
                compNode.services.ForEach(n => CreateNodeView(n, false));
            }

            return nodeView;
        }

        private void CreateEdges(BTCompositeChild child)
        {
            if (tree.rootNode == child.GetNode())
            {
                BTRootNodeView parentView = GetNodeByGuid("BTRoot") as BTRootNodeView;
                BTNodeView childView = GetNodeByGuid(child.GetNode().nodeId) as BTNodeView;

                Edge edge = parentView.output.ConnectTo(childView.input);
                AddElement(edge);
            }


            if (child.childComposite)
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
                var types = TypeCache.GetTypesDerivedFrom<BTComposite>();

                foreach (var type in types)
                {
                    evt.menu.AppendAction($"Add/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                }
            }

            bool active = selection.Count == 1 && selection[0] is not BTRootNodeView && ((selection[0] as BTNodeView).node is not BTAuxiliaryNode);
            if (active)
            {
                {
                    var types = TypeCache.GetTypesDerivedFrom<BTDecorator>();

                    foreach (var type in types)
                    {
                        evt.menu.AppendAction($"Add/Decorator/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                    }
                }

                {
                    var types = TypeCache.GetTypesDerivedFrom<BTService>();

                    foreach (var type in types)
                    {
                        evt.menu.AppendAction($"Add/Service/{type.Name.Replace(type.BaseType.Name + "_", "")}", (a) => { CreateNode(type); }, status);
                    }
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
            else if (startPort.node is not BTCompositeNodeView)
            {
                returnPorts = returnPorts.Where(endport => endport.node is not BTRootNodeView);
            }
            return returnPorts
                .ToList();
        }

        private void CreateNode(System.Type type)
        {
            var node = tree.CreateNode(type, out _);
            node.InitializeFromAsset(tree);
            if (selection.Count == 1 && selection[0] is BTNodeView comp && node is BTAuxiliaryNode auxNode)
            {
                for (int i = 0; i < tree.compositeNodes.Count; i++)
                {
                    if (tree.compositeNodes[i].GetNode() == comp.node)
                    {
                        tree.AddAuxNode(tree.compositeNodes[i], auxNode);
                        break;
                    }
                }


                CreateNodeView(node, true, comp);
            }
            CreateNodeView(node, true);
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
