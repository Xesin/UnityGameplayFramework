using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Xesin.GameplayFramework.Domain;

namespace Xesin.GameplayFramework.Editor
{
    public static class DomainReloadHandler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeLoad()
        {
            if (!EditorSettings.enterPlayModeOptionsEnabled)
                return;

            int clearedValues = 0;

            foreach (MemberInfo member in GetMembers<ClearOnReloadAttribute>(true))
            {
                //Fields
                {
                    var field = member as FieldInfo;

                    if (field != null && !field.FieldType.IsGenericParameter && field.IsStatic)
                    {
                        Type fieldType = field.FieldType;
                        ClearOnReloadAttribute reloadAttribute = field.GetCustomAttribute<ClearOnReloadAttribute>();
                        object valueOnReload = reloadAttribute?.ValueOnReload;
                        bool createNewInstance = reloadAttribute != null && reloadAttribute.CreateNewInstance;
                        dynamic value = valueOnReload != null ? Convert.ChangeType(valueOnReload, fieldType) : null;
                        if (createNewInstance) value = Activator.CreateInstance(fieldType);

                        try
                        {
                            field.SetValue(null, value);
                        }
                        catch
                        {
                            // ignored
                        }

                        clearedValues++;
                    }
                }

                //Properties
                {

                    var property = member as PropertyInfo;

                    if (property != null && !property.PropertyType.IsGenericParameter && property.GetAccessors(true).Any(x => x.IsStatic))
                    {
                        Type fieldType = property.PropertyType;
                        ClearOnReloadAttribute reloadAttribute = property.GetCustomAttribute<ClearOnReloadAttribute>();
                        object valueOnReload = reloadAttribute?.ValueOnReload;
                        bool createNewInstance = reloadAttribute != null && reloadAttribute.CreateNewInstance;
                        dynamic value = valueOnReload != null ? Convert.ChangeType(valueOnReload, fieldType) : null;
                        if (createNewInstance) value = Activator.CreateInstance(fieldType);

                        try
                        {
                            property.SetValue(null, value);
                        }
                        catch
                        {
                            // ignored
                        }

                        clearedValues++;
                    }
                }
            }

            // Debug.Log($"Cleared {clearedValues} members, executed {executedMethods} methods");
        }

        private static IEnumerable<MemberInfo> GetMethodMembers<TAttribute>(bool inherit) where TAttribute : System.Attribute
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var members = new List<MemberInfo>();

            try
            {
                var allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < allAssemblies.Length; i++)
                {
                    var assembly = allAssemblies[i];
                    if (!assembly.FullName.Contains("Xaloc")) continue;

                    members.AddRange(from t in assembly.GetTypes()
                                     where t.IsClass
                                     where !t.IsGenericParameter
                                     from m in t.GetMethods(flags)
                                     where !m.ContainsGenericParameters
                                     where m.IsDefined(typeof(TAttribute), inherit)
                                     select m);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                //ignored
            }

            return members;
        }

        private static IEnumerable<MemberInfo> GetMembers<TAttribute>(bool inherit) where TAttribute : System.Attribute
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var members = new List<MemberInfo>();

            try
            {
                var allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < allAssemblies.Length; i++)
                {
                    var assembly = allAssemblies[i];
                    var types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        if (!type.IsClass) continue;

                        //Fields
                        members.AddRange(type.GetFields(flags).Cast<MemberInfo>().Where(member => member.IsDefined(typeof(TAttribute), inherit)));

                        //Properties
                        var properties = type.GetProperties(flags);
                        var memberInfo = properties.Cast<MemberInfo>().ToList();
                        members.AddRange(memberInfo.Where(member => member.IsDefined(typeof(TAttribute), inherit)));

                        //Events
                        members.AddRange((from eventInfo in type.GetEvents(flags) where eventInfo.IsDefined(typeof(TAttribute), inherit) select GetEventField(type, eventInfo.Name)).Cast<MemberInfo>());
                    }
                }



            }
            catch (ReflectionTypeLoadException)
            {
                //ignored
            }

            return members;
        }

        private static FieldInfo GetEventField(Type type, string eventName)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = null;

            while (type != null)
            {

                //Events defined as field
                field = type.GetField(eventName, flags);
                if (field != null && (field.FieldType == typeof(MulticastDelegate) || field.FieldType.IsSubclassOf(typeof(MulticastDelegate))))
                    break;

                //Events defined as property { add; remove; }
                field = type.GetField(EventName(eventName), flags);
                if (field != null)
                    break;
                type = type.BaseType;
            }
            return field;
        }

        private static string EventName(string eventName)
            => $"EVENT_{eventName.ToUpper()}";
    }
}