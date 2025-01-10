using System;
using System.Collections;

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

        public static void ContainsSame(IEnumerable<object> actual, IEnumerable<object> expected, string failMessage = ""){
            if (actual.Count() != expected.Count())
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new Exception($"Assertion Failed: Expected {expected.Count()} elements but got {actual.Count()}");
                }

                throw new Exception("Assertion Failed: " + failMessage);
            }

            for (int i = 0; i < actual.Count(); i++)
            {
                if (!Equals(actual.ElementAt(i), expected.ElementAt(i)))
                {
                    if (string.IsNullOrEmpty(failMessage))
                    {
                        throw new Exception($"Assertion Failed: Expected {expected.ElementAt(i)} at index {i} but got {actual.ElementAt(i)}");
                    }

                    throw new Exception("Assertion Failed: " + failMessage);
                }
            }
        }

        public static void ContainsSame(IDictionary actual, IDictionary expected, string failMessage = "") {
            if (actual.Count != expected.Count)
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new Exception($"Assertion Failed: Expected {expected.Count} elements but got {actual.Count}");
                }

                throw new Exception("Assertion Failed: " + failMessage);
            }

            foreach (var key in expected.Keys)
            {
                if (!actual.Contains(key) || !Equals(actual[key], expected[key]))
                {
                    if (string.IsNullOrEmpty(failMessage))
                    {
                        throw new Exception($"Assertion Failed: Expected {expected[key]} at key {key} but got {actual[key]}");
                    }

                    throw new Exception("Assertion Failed: " + failMessage);
                }
            }
        }

        public static void Throws(Action action, string expectedError, string failMessage = "")
        {
            Exception ex = null;
            try
            {
                action();
            }
            catch (Exception ex2)
            {
                ex = ex2;
                return;
            }

            if (ex == null)
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new Exception("Assertion Failed: Expected exception " + expectedError + " but no exception was thrown");
                }

                throw new Exception("Assertion Failed: " + failMessage);
            }

            if (ex.Message != expectedError)
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new Exception("Assertion Failed: Expected exception message " + expectedError + " but got " + ex.Message);
                }

                throw new Exception("Assertion Failed: " + failMessage);
            }
        }
    }
}