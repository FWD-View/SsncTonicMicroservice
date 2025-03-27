using System;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Specifies the property name that is present in the Oracle PAR file serializing and deserializing.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ParameterAttribute"/> with the specified property name.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    public ParameterAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; }
}