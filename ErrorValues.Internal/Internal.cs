using System;

namespace ErrorValues.Internal
{
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
}

namespace ErrorValues.Internal.Attributes
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class GenerationAttribute : Attribute
    {
        public GenerationAttribute(string enum_name) { }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class NotImplementedByConsumerAttribute : Attribute
    {
        public NotImplementedByConsumerAttribute() { }
    }
}
