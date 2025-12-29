using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop
{
    /// <summary>
    /// Implementation of function signature
    /// </summary>
    public class FunctionSignature : IFunctionSignature
    {
        public string FunctionName { get; set; }
        public string ReturnType { get; set; } = "object";
        public IList<IParameterInfo> Parameters { get; set; } = new List<IParameterInfo>();
        public bool IsAsync { get; set; } = false;
        public string Documentation { get; set; } = string.Empty;

        public FunctionSignature()
        {
        }

        public FunctionSignature(string functionName, string returnType = "object", params IParameterInfo[] parameters)
        {
            FunctionName = functionName;
            ReturnType = returnType;
            Parameters = parameters?.ToList() ?? new List<IParameterInfo>();
        }

        /// <summary>
        /// Generates a C# method signature string
        /// </summary>
        public string ToCSharpSignature()
        {
            var paramStr = string.Join(", ", Parameters.Select(p => $"{p.Type} {p.Name}" + (p.IsOptional ? $" = {GetDefaultValueString(p.DefaultValue)}" : "")));
            var returnTypeStr = IsAsync ? $"Task<{ReturnType}>" : ReturnType;
            return $"{returnTypeStr} {FunctionName}({paramStr})";
        }

        private string GetDefaultValueString(object defaultValue)
        {
            if (defaultValue == null)
                return "null";

            if (defaultValue is string str)
                return $"\"{str}\"";

            if (defaultValue is bool b)
                return b ? "true" : "false";

            return defaultValue.ToString();
        }
    }

    /// <summary>
    /// Implementation of parameter info
    /// </summary>
    public class ParameterInfo : IParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; } = "object";
        public bool IsOptional { get; set; } = false;
        public object DefaultValue { get; set; }
        public string Documentation { get; set; } = string.Empty;

        public ParameterInfo()
        {
        }

        public ParameterInfo(string name, string type, bool isOptional = false, object defaultValue = null)
        {
            Name = name;
            Type = type;
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
    }
}

