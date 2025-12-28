using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop
{
    /// <summary>
    /// Represents a function signature with parameter types and return type
    /// Used for generating strongly-typed proxy methods
    /// </summary>
    public interface IFunctionSignature
    {
        /// <summary>
        /// Name of the function
        /// </summary>
        string FunctionName { get; }

        /// <summary>
        /// Return type of the function (C# type name)
        /// </summary>
        string ReturnType { get; }

        /// <summary>
        /// Parameter information (name, type, optional)
        /// </summary>
        IList<IParameterInfo> Parameters { get; }

        /// <summary>
        /// Whether the function is async (returns Task/Task<T>)
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Documentation/description of the function
        /// </summary>
        string Documentation { get; }
    }

    /// <summary>
    /// Information about a function parameter
    /// </summary>
    public interface IParameterInfo
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Parameter type (C# type name)
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Whether the parameter is optional
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Default value if optional
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// Parameter documentation
        /// </summary>
        string Documentation { get; }
    }
}

