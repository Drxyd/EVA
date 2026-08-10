using System;

namespace ErrorValues.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public class ErrorValuesAttribute : Attribute 
{
    public ErrorValuesAttribute(string name) { }
}

[AttributeUsage(AttributeTargets.Field)]
public class PayloadAttribute<T> : Attribute
{
    public PayloadAttribute(bool happy = false)
    {
    }
}

[AttributeUsage(AttributeTargets.Struct)]
public class GenerationAttribute : Attribute 
{ 
    public GenerationAttribute(string enum_name) { }
} // Should be limited to generated code