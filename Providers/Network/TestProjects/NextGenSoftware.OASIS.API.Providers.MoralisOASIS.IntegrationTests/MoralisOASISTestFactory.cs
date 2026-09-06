using System;
using NextGenSoftware.OASIS.API.Providers.MoralisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.IntegrationTests
{
    /// <summary>
    /// Builds the provider under test. Constructor shapes vary across providers, so
    /// this resolves one reflectively and feeds each parameter from an environment
    /// variable named MORALISOASIS_&lt;PARAM&gt;, falling back to a harmless default.
    /// Keeping it in one place means the tests do not need per-provider wiring.
    /// </summary>
    internal static class MoralisOASISTestFactory
    {
        public static MoralisOASIS Create()
        {
            var type = typeof(MoralisOASIS);

            foreach (var ctor in SortByFewestParameters(type))
            {
                var ps = ctor.GetParameters();
                var args = new object?[ps.Length];
                var ok = true;

                for (var i = 0; i < ps.Length; i++)
                {
                    var env = Environment.GetEnvironmentVariable(
                        $"MORALISOASIS_{ps[i].Name!.ToUpperInvariant()}");

                    if (env != null) { args[i] = Convert.ChangeType(env, ps[i].ParameterType); continue; }
                    if (ps[i].HasDefaultValue) { args[i] = ps[i].DefaultValue; continue; }

                    var d = Default(ps[i].ParameterType);
                    if (d == null && ps[i].ParameterType.IsValueType) { ok = false; break; }
                    args[i] = d;
                }

                if (!ok) continue;
                try { return (MoralisOASIS)ctor.Invoke(args); } catch { /* try the next overload */ }
            }

            throw new InvalidOperationException(
                $"Could not construct {typeof(MoralisOASIS).Name} - no usable constructor.");
        }

        private static System.Reflection.ConstructorInfo[] SortByFewestParameters(Type t)
        {
            var ctors = t.GetConstructors();
            Array.Sort(ctors, (a, b) => a.GetParameters().Length.CompareTo(b.GetParameters().Length));
            return ctors;
        }

        private static object? Default(Type t)
        {
            if (t == typeof(string)) return "test";
            if (t == typeof(bool)) return false;
            if (t == typeof(int) || t == typeof(long)) return 0;
            if (t == typeof(Guid)) return Guid.Empty;
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null;
        }
    }
}
