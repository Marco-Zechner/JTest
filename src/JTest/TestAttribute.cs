using System;

namespace MarcoZechner.JTest {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestAttribute() : Attribute
    {
        public string? Name { get; set; } = null;
        public int Order { get; set; } = 0;
        public int Timeout { get; set; } = 0;
    }
}