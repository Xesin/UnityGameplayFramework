using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class DefaultSubsystemPrefabAttribute : Attribute
    {
        readonly string prefabName;

        public DefaultSubsystemPrefabAttribute(string positionalString)
        {
            this.prefabName = positionalString;
        }

        public string PrefabName
        {
            get { return prefabName; }
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class AutoCreateSubsystemAttribute : Attribute
    {
        public AutoCreateSubsystemAttribute()
        {
        }
    }

    public class Subsystems : MonoSingleton<Subsystems>
    {
        private Dictionary<Type, IGameplaySubsystem> registeredSubsystems = new Dictionary<Type, IGameplaySubsystem>();

        public static T GetSubsystem<T>() where T : Subsystem<T>
        {
            var instance = Instance;
            if (instance.registeredSubsystems.TryGetValue(typeof(T), out var subsystem))
            {
                return subsystem as T;
            }
            else if(typeof(T).GetCustomAttribute<AutoCreateSubsystemAttribute>() != null)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying) return null;
#endif
                var newSystem = instance.InstantiateSystem<T>();
                RegisterSubsystem(newSystem);
                return newSystem;
            }

            return null;
        }

        public static void RegisterSubsystem<T>(T systemObject) where T : Subsystem<T>
        {
            var instance = Instance;

            if(instance.registeredSubsystems.ContainsKey(typeof(T)))
            {
                Debug.LogWarning($"System {typeof(T)} already registered, skip registration");
            }
            else
            {
                instance.registeredSubsystems.Add(typeof(T), systemObject);
                DontDestroyOnLoad(systemObject.gameObject);
                systemObject.OnRegistered();
            }
        }

        public static void UnregisterSubsystem<T>() where T : Subsystem<T>
        {
            var instance = Instance;

            if (instance.registeredSubsystems.TryGetValue(typeof(T), out var subsystem))
            {
                subsystem.OnDesregistered();
                instance.registeredSubsystems.Remove(typeof(T));
            }
        }

        private T InstantiateSystem<T>() where T : Subsystem<T>
        {
            var type = typeof(T);
            var defaultPrefabAttr = type.GetCustomAttribute<DefaultSubsystemPrefabAttribute>();
            if(defaultPrefabAttr != null)
            {
                // TODO: Get from a list of configured prefabs
                var prefabName = defaultPrefabAttr.PrefabName;

                var newGo = new GameObject(type.Name + "GameObject");
                var newSubsystem = newGo.AddComponent<T>();
                return newSubsystem;
            }
            else
            {
                var newGo = new GameObject(type.Name + "GameObject");
                var newSubsystem = newGo.AddComponent<T>();
                return newSubsystem;
            }
        }
    }
}
