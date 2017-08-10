using System;

namespace RedGate.Ipc.Proxy
{
    /// <summary>
    /// Attributes based on ProxyShouldImplementAttribute can be used on methods in interface declarations.
    /// The dynamic proxy generator will re-implement the attribute on types created for the interface.
    /// Only attributes with default constructors need apply.
    /// </summary>
    public abstract class ProxyShouldImplementAttribute : Attribute
    {

    }
}