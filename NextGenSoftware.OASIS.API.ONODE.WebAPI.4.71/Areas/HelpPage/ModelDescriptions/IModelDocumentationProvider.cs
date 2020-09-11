using System;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}