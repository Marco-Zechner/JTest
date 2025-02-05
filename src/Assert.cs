using System.Collections;
using MarcoZechner.PrettyReflector;

namespace MarcoZechner.JTest; 

public static class Assert
{
    public static void Same(object actual, object expected, string failMessage = "")
    {
        if (!Equals(actual, expected))
        {
            if (string.IsNullOrEmpty(failMessage))
            {
                throw new AssertException($"Assertion Failed: Expected {expected} but got {actual}");
            }
            else
            {
                throw new AssertException($"Assertion Failed: {failMessage}");
            }
        }
    }

    public static void ContainsSame(IEnumerable<object> actual, IEnumerable<object> expected, string failMessage = ""){
        if (actual.Count() != expected.Count())
        {
            if (string.IsNullOrEmpty(failMessage))
            {
                throw new AssertException($"Assertion Failed: Expected {expected.Count()} elements but got {actual.Count()}");
            }

            throw new AssertException("Assertion Failed: " + failMessage);
        }

        for (int i = 0; i < actual.Count(); i++)
        {
            if (!Equals(actual.ElementAt(i), expected.ElementAt(i)))
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new AssertException($"Assertion Failed: Expected {expected.ElementAt(i)} at index {i} but got {actual.ElementAt(i)}");
                }

                throw new AssertException("Assertion Failed: " + failMessage);
            }
        }
    }

    public static void ContainsSame(IDictionary actual, IDictionary expected, string failMessage = "") {
        if (actual.Count != expected.Count)
        {
            if (string.IsNullOrEmpty(failMessage))
            {
                throw new AssertException($"Assertion Failed: Expected {expected.Count} elements but got {actual.Count}");
            }

            throw new AssertException("Assertion Failed: " + failMessage);
        }

        foreach (var key in expected.Keys)
        {
            if (!actual.Contains(key) || !Equals(actual[key], expected[key]))
            {
                if (string.IsNullOrEmpty(failMessage))
                {
                    throw new AssertException($"Assertion Failed: Expected {expected[key]} at key {key} but got {actual[key]}");
                }

                throw new AssertException("Assertion Failed: " + failMessage);
            }
        }
    }

    public static void Throws(Action action, Exception expectedException, string failMessage = "")
    {
        Exception? ex = null;
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
                throw new AssertException("Assertion Failed: Expected exception " + expectedException + " but no exception was thrown");
            }

            throw new AssertException("Assertion Failed: " + failMessage);
        }

        if (ex.GetType != expectedException.GetType) {
            if (string.IsNullOrEmpty(failMessage))
            {
                throw new AssertException("Assertion Failed: Expected exception type " + expectedException.GetType().PrettyType() + " but got " + ex.GetType().PrettyType());
            }

            throw new AssertException("Assertion Failed: " + failMessage);
        }

        if (ex.Message != expectedException.Message)
        {
            if (string.IsNullOrEmpty(failMessage))
            {
                throw new AssertException("Assertion Failed: Expected exception message " + expectedException.Message + " but got " + ex.Message);
            }

            throw new AssertException("Assertion Failed: " + failMessage);
        }
    }
}