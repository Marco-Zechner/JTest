using System;

namespace MarcoZechner.JTest {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CaseAttribute(params object[] parameters) : Attribute
    {
        public string? Name { get; set;} = null;
        public int Timeout { get; set;} = 0;
        public object[] Parameters { get; } = parameters;
    }
}