using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Xesin.GameplayFramework
{
    public class InputHintImage : MonoBehaviour
    {
        [SerializeField] private Transform hintContainer;
        [SerializeField] private InputActionReference inputAction;
        [SerializeField] private Image hintPrefab;
        [SerializeField] private float compositeElementSize = 100;
        [SerializeField] private float compositeElementPadding = 10;
        [SerializeField] private bool isSecondaryInput;

        private GridLayoutGroup gridLayout;
        private ContentSizeFitter sizeFitter;
        private UIWidget widgetParent;
        private LocalPlayer cachedPlayer;

        private List<GameObject> instancedHints = new List<GameObject>();

        private void Awake()
        {
            if(!hintContainer.TryGetComponent(out gridLayout))
            {
                gridLayout = hintContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            if (!hintContainer.TryGetComponent(out sizeFitter))
            {
                sizeFitter = hintContainer.gameObject.AddComponent<ContentSizeFitter>();
            }

            gridLayout.enabled = false;
            sizeFitter.enabled = false;

            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            widgetParent = GetComponentInParent<UIWidget>();
        }

        private void OnEnable()
        {
            UpdateImage();
            ConnectCallbacks();
        }

        private void OnDisable()
        {
            DisconnectCallbacks();
        }

        public void UpdateImage()
        {
            for (int i = 0; i < instancedHints.Count; i++)
            {
                DestroyImmediate(instancedHints[i]);
            }

            instancedHints.Clear();

            PlayerInput input = GetPlayerInput();
            var action = inputAction.action;

            int bindingIndex = action.GetBindingIndex(input.currentControlScheme);

            if(bindingIndex == -1)
            {
                Image newImage = CreateNewImage();
                newImage.sprite = InputImages.Instance.GetFallbackImage();
            }


            if (!action.bindings[bindingIndex].isComposite && !action.bindings[bindingIndex].isPartOfComposite)
            {
                sizeFitter.enabled = false;
                gridLayout.enabled = false;

                if(isSecondaryInput && action.bindings.Count < bindingIndex + 1 && action.bindings[bindingIndex + 1].action == action.bindings[bindingIndex].action)
                {
                    bindingIndex++;
                }

                var newImage = CreateNewImage();
                newImage.sprite = InputImages.Instance.GetInputImage(action, bindingIndex);
            }
            else
            {
                if(action.bindings[bindingIndex].isComposite)
                    bindingIndex += 1;

                int numCompositeBindings = 0;
                sizeFitter.enabled = true;
                gridLayout.enabled = true;

                List<string> addedActions = new List<string>(4);

                while (action.bindings[bindingIndex].isPartOfComposite)
                {
                    if (!isSecondaryInput && addedActions.Contains(action.bindings[bindingIndex].name))
                    {
                        bindingIndex++;
                        continue;
                    }
                    else if(isSecondaryInput && !addedActions.Contains(action.bindings[bindingIndex].name) && action.bindings.Count(b => b.name == action.bindings[bindingIndex].name) > 1)
                    {
                        addedActions.Add(action.bindings[bindingIndex].name);
                        bindingIndex++;
                        continue;
                    }
                    else
                    {
                        addedActions.Add(action.bindings[bindingIndex].name);
                    }

                    numCompositeBindings++;


                    var newImage = CreateNewImage();
                    newImage.sprite = InputImages.Instance.GetInputImage(action, bindingIndex);

                    bindingIndex++;
                }

                if(numCompositeBindings == 4)
                {
                    GameObject blankSpace = CreateBlankSpace();
                    blankSpace.transform.SetAsFirstSibling();

                    blankSpace = CreateBlankSpace();
                    blankSpace.transform.SetSiblingIndex(2);

                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.constraintCount = 3;
                }
                else
                {
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.constraintCount = 2;
                }
            }
        }

        private Image CreateNewImage()
        {
            var newImage = Instantiate(hintPrefab, hintContainer);
            instancedHints.Add(newImage.gameObject);
            return newImage;
        }

        private GameObject CreateBlankSpace()
        {
            var blankSpace = new GameObject("Blank Space");
            blankSpace.transform.SetParent(hintContainer);
            blankSpace.AddComponent<RectTransform>();
            instancedHints.Add(blankSpace);
            return blankSpace;
        }

        private PlayerInput GetPlayerInput()
        {
            if (cachedPlayer) return cachedPlayer.PlayerInput;

            PlayerInput input;
            if (widgetParent && widgetParent.Owner)
            {
                input = widgetParent.Owner.PlayerInput;
            }
            else
            {
                input = LocalPlayer.GetLocalPlayer(0).PlayerInput;
            }

            return input;
        }

        private void ConnectCallbacks()
        {
            PlayerInput input = GetPlayerInput();
            input.onControlsChanged += OnControlsChanged;
        }

        private void DisconnectCallbacks()
        {
            PlayerInput input = GetPlayerInput();
            input.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput playerInput)
        {
            UpdateImage();
        }
    }
}
