using System;

namespace MarcoZechner.JTest {
    public static class Assert
    {
        public static void Same(object actual, object expected, string failMessage = "")
        {
            if (!Equals(actual, expected))
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new Exception($"Assertion Failed: Expected {expected} but got {actual}");
                }
                else
                {
                    throw new Exception($"Assertion Failed: {failMessage}");
                }
            }
        }
    }
}