using System;
using ErrorValues.Internal.Attributes;

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

[NotImplementedByConsumer]
public interface IEVA<T> 
{
    public bool Happy { get; }
}