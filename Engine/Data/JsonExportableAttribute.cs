// Создаем новый файл JsonAttributes.cs в namespace Engine
namespace Engine.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonExportableAttribute : Attribute
    {
        public string Category { get; }
        public Type DataType { get; }

        public JsonExportableAttribute(string category, Type dataType = null)
        {
            Category = category;
            DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class JsonFieldAttribute : Attribute
    {
        public string Description { get; }
        public bool Required { get; }
        public object DefaultValue { get; }

        public JsonFieldAttribute(string description = "", bool required = true, object defaultValue = null)
        {
            Description = description;
            Required = required;
            DefaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class JsonIgnoreAttribute : Attribute
    {
    }
}