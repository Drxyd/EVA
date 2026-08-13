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

public interface IEVA<T> 
{
    public bool Happy { get; }
}

public static class TypeExtensions
{
    public static string GetCleanName(this Type type)
    {
        string name = type.Name;
        int index = name.IndexOf('`');
        return index > 0 ? name.Substring(0, index) : name;
    }

    public static string GetCleanFullName(this Type type)
    {
        string name = type.FullName;
        int index = name.IndexOf('`');
        return index > 0 ? name.Substring(0, index) : name;
    }
}