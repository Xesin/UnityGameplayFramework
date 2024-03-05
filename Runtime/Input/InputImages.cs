using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework
{
    [Serializable]
    public struct DeviceCollection
    {
        public string DeviceName;
        public AssetReferenceT<InputImageCollection> imageCollection;
    }

    [CreateAssetMenu(menuName = "Gameplay/Input/Input Images", fileName = "InputImages.asset")]
    public class InputImages : ScriptableSingleton<InputImages>
    {
        [SerializeField] private DeviceCollection[] inputImageCollections;
        [SerializeField] private Sprite fallbackImage;

        private Dictionary<string, InputImageCollection> loadedCollections;

        protected override void OnEnable()
        {
            base.OnEnable();
            loadedCollections ??= new Dictionary<string, InputImageCollection>();
            loadedCollections.Clear();
        }

        public Sprite GetInputImage(InputAction action, string controlScheme)
        {
            int bindingIndex = action.GetBindingIndex(controlScheme);

            if (action.bindings[bindingIndex].isComposite)
            {
                bindingIndex += 1;
            }

            if (bindingIndex != -1)
            {
                _ = action.GetBindingDisplayString(bindingIndex, out var deviceLayoutName, out var controlPath);

                var collection = GetCollectionFromDevice(deviceLayoutName);

                if (collection != null)
                {
                    return collection.GetInputImage(controlPath);
                }
            }

            return fallbackImage;
        }

        public Sprite GetInputImage(InputAction action, int bindingIndex)
        {
            if (bindingIndex != -1)
            {
                _ = action.GetBindingDisplayString(bindingIndex, out var deviceLayoutName, out var controlPath);

                var collection = GetCollectionFromDevice(deviceLayoutName);

                if (collection != null)
                {
                    return collection.GetInputImage(controlPath);
                }
            }

            return fallbackImage;
        }

        private InputImageCollection GetCollectionFromDevice(string layoutName)
        {
            InputImageCollection loadedCollection;
            if (!loadedCollections.TryGetValue(layoutName, out loadedCollection))
            {
                for (int i = 0; i < inputImageCollections.Length; i++)
                {
                    var collection = inputImageCollections[i];
                    if (InputSystem.IsFirstLayoutBasedOnSecond(layoutName, collection.DeviceName))
                    {
                        loadedCollection = collection.imageCollection.LoadAssetAsync().WaitForCompletion();
                        loadedCollections.Add(layoutName, loadedCollection);
                        break;
                    }
                }
            }

            return loadedCollection;
        }

        public Sprite GetFallbackImage()
        {
            return fallbackImage;
        }
    }
}
