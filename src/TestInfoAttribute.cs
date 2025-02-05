namespace MarcoZechner.JTest;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestInfoAttribute(string category) : Attribute
{
    public string Category { get; } = category;
}
