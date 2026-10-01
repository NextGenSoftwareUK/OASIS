using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL
{
    /// <summary>
    /// Strips method-derived fields from auto-inferred OASIS types.
    /// <para>
    /// Most OASIS model interfaces derive from IHolon, which declares persistence methods such as
    /// <c>AddHolon(IHolon holon, ...)</c> and <c>RemoveHolon(IHolon holon, ...)</c>. HotChocolate infers a
    /// GraphQL field (with arguments) for every public method, so those IHolon parameters became an
    /// <c>IHolon (Input)</c> type reference that cannot be inferred from an interface - and the whole schema
    /// then failed to build with "Unable to infer or resolve a schema type from the type reference
    /// 'IHolon (Input)'".
    /// </para>
    /// <para>
    /// GraphQL should expose OASIS model DATA (properties), not the ORM/persistence surface, so every
    /// method-derived field on a type in the OASIS assemblies is removed. The Query/Mutation roots are
    /// exempt (their fields ARE methods by design), as are explicitly configured types such as AvatarType
    /// and HolonType, whose fields come from descriptors rather than reflected members.
    /// </para>
    /// </summary>
    public class OASISMethodFieldTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is not ObjectTypeDefinition objectTypeDefinition)
                return;

            var runtimeType = objectTypeDefinition.RuntimeType;

            if (runtimeType == null || runtimeType == typeof(Query) || runtimeType == typeof(Mutation))
                return;

            if (!IsOASISAssembly(runtimeType.Assembly))
                return;

            List<ObjectFieldDefinition> methodFields = objectTypeDefinition.Fields
                .Where(field => field.Member is MethodInfo)
                .ToList();

            foreach (var methodField in methodFields)
                objectTypeDefinition.Fields.Remove(methodField);
        }

        private static bool IsOASISAssembly(Assembly assembly)
        {
            var name = assembly?.GetName().Name;
            return name != null && name.StartsWith("NextGenSoftware.OASIS", System.StringComparison.Ordinal);
        }
    }
}
