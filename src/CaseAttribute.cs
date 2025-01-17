namespace MarcoZechner.JTest; 

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CaseAttribute(params object[] parameters) : Attribute
{
    public string Name { get; set;} = string.Empty;
    public object[] Parameters { get; } = parameters;
}