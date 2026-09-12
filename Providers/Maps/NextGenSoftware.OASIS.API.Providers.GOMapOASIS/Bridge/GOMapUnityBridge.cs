using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge
{
    /// <summary>
    /// Forwards calls to a live GO Map instance without a compile-time dependency
    /// on it.
    ///
    /// GO Map is a Unity Asset Store package: its types (GOMap, GoShared.GOCoordinate,
    /// GOLocationManager, GOOrbit) only exist inside a Unity player, and referencing
    /// UnityEngine from a net10.0 library is not possible. So when this provider is
    /// hosted inside Unity, Initialize is handed the real GOMap MonoBehaviour and
    /// everything is dispatched through reflection; when it is hosted on a server it
    /// simply has no instance and the provider works from its own state and command
    /// queue instead.
    ///
    /// Member lookups are cached, so the reflection cost is paid once per member.
    /// </summary>
    public class GOMapUnityBridge
    {
        private readonly ConcurrentDictionary<string, MethodInfo> _methods =
            new ConcurrentDictionary<string, MethodInfo>();

        private readonly ConcurrentDictionary<string, MemberInfo> _members =
            new ConcurrentDictionary<string, MemberInfo>();

        public object Instance { get; private set; }

        public bool IsBound => Instance != null;

        /// <summary>The last reflection failure, kept for diagnostics rather than thrown.</summary>
        public string LastError { get; private set; }

        public void Bind(object goMapInstance)
        {
            Instance = goMapInstance;
            _methods.Clear();
            _members.Clear();
            LastError = null;
        }

        public void Unbind()
        {
            Instance = null;
            _methods.Clear();
            _members.Clear();
        }

        /// <summary>
        /// Invokes a method on the bound GO Map instance. Returns false (without
        /// throwing) when nothing is bound or the member does not exist, so callers
        /// can fall back to headless behaviour.
        /// </summary>
        public bool TryInvoke(string methodName, object[] args, out object result)
        {
            result = null;
            if (Instance == null) return false;

            try
            {
                string key = methodName + "/" + (args == null ? 0 : args.Length);
                MethodInfo method = _methods.GetOrAdd(key, _ => FindMethod(methodName, args));

                if (method == null)
                {
                    LastError = "GO Map instance has no method " + methodName + " taking "
                              + (args == null ? 0 : args.Length) + " argument(s).";
                    return false;
                }

                result = method.Invoke(Instance, args);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                LastError = "GO Map " + methodName + " threw: "
                          + (ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                return false;
            }
            catch (Exception ex)
            {
                LastError = "Could not invoke GO Map " + methodName + ": " + ex.Message;
                return false;
            }
        }

        public bool TryInvoke(string methodName, params object[] args)
        {
            return TryInvoke(methodName, args, out _);
        }

        /// <summary>
        /// Reads a member, following a dotted path such as "locationManager.currentLocation".
        /// </summary>
        public bool TryGetValue(string path, out object value)
        {
            value = null;
            if (Instance == null || string.IsNullOrWhiteSpace(path)) return false;

            object current = Instance;

            foreach (string segment in path.Split('.'))
            {
                if (current == null) return false;

                MemberInfo member = _members.GetOrAdd(
                    current.GetType().FullName + "." + segment,
                    CreateMemberLookup(current.GetType(), segment));

                if (member == null)
                {
                    LastError = "GO Map type " + current.GetType().Name + " has no member " + segment + ".";
                    return false;
                }

                try
                {
                    PropertyInfo property = member as PropertyInfo;
                    current = property != null
                        ? property.GetValue(current)
                        : ((FieldInfo)member).GetValue(current);
                }
                catch (Exception ex)
                {
                    LastError = "Could not read GO Map member " + segment + ": " + ex.Message;
                    return false;
                }
            }

            value = current;
            return true;
        }

        /// <summary>Reads a member and converts it, returning the fallback if anything is missing.</summary>
        public T GetValueOrDefault<T>(string path, T fallback = default)
        {
            object value;
            if (!TryGetValue(path, out value) || value == null) return fallback;

            try
            {
                if (value is T) return (T)value;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Builds a GoShared.GOCoordinate for the bound instance's assembly, so
        /// coordinates can be handed to GO Map in its own type. Returns null when
        /// the type cannot be located.
        /// </summary>
        public object CreateCoordinate(double latitude, double longitude, double altitude = 0.0)
        {
            if (Instance == null) return null;

            try
            {
                Type coordinateType = Instance.GetType().Assembly.GetType("GoShared.GOCoordinate");

                if (coordinateType == null)
                {
                    coordinateType = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => SafeGetType(a, "GoShared.GOCoordinate"))
                        .FirstOrDefault(t => t != null);
                }

                if (coordinateType == null)
                {
                    LastError = "GoShared.GOCoordinate could not be located in the loaded assemblies.";
                    return null;
                }

                object coordinate = Activator.CreateInstance(coordinateType);
                SetIfPresent(coordinateType, coordinate, "latitude", latitude);
                SetIfPresent(coordinateType, coordinate, "longitude", longitude);
                SetIfPresent(coordinateType, coordinate, "altitude", altitude);
                return coordinate;
            }
            catch (Exception ex)
            {
                LastError = "Could not construct a GOCoordinate: " + ex.Message;
                return null;
            }
        }

        private static Type SafeGetType(Assembly assembly, string typeName)
        {
            try
            {
                return assembly.GetType(typeName);
            }
            catch
            {
                return null;
            }
        }

        private static void SetIfPresent(Type type, object target, string name, double value)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, Convert.ChangeType(value, property.PropertyType));
                return;
            }

            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                field.SetValue(target, Convert.ChangeType(value, field.FieldType));
        }

        private MethodInfo FindMethod(string methodName, object[] args)
        {
            int arity = args == null ? 0 : args.Length;

            return Instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase)
                                     && m.GetParameters().Length == arity);
        }

        private static Func<string, MemberInfo> CreateMemberLookup(Type type, string name)
        {
            return _ => FindMember(type, name);
        }

        private static MemberInfo FindMember(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance
                                     | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase;

            MemberInfo property = type.GetProperty(name, flags);
            return property ?? type.GetField(name, flags);
        }
    }
}
