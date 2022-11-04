#pragma warning disable IDE0073 // Licensed not appropriate here

// ReSharper disable once CheckNamespace - This is a shim for .NET Standard
// see https://github.com/dotnet/csharplang/issues/287#issuecomment-967195663
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

    public string ParameterName { get; }
}
