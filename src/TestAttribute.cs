namespace MarcoZechner.JTest; 

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestAttribute() : Attribute
{
    public string? Name { get; set; }
}