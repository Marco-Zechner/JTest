using System.Reflection;
using MarcoZechner.PrettyReflector;

namespace MarcoZechner.JTest; 

public class ReflectionHandler{
    public static List<TestCase> DiscoverTests(){
        List<TestCase> testCases = [];

        var testAssembly = Assembly.GetEntryAssembly() 
        ?? throw new InvalidOperationException("Unable to load the entry assembly.");

        foreach (var declaringType in testAssembly.GetTypes()) {
            var testInfoClassAttribute = declaringType.GetCustomAttribute<TestInfoAttribute>();
            foreach (var method in declaringType.GetMethods()) {
                var testAttribute = method.GetCustomAttribute<TestAttribute>();
                if (testAttribute == null)
                    continue;

                string baseCategory = testInfoClassAttribute?.Category ?? string.Empty;
                string methodCategory = method.GetCustomAttribute<TestInfoAttribute>()?.Category ?? string.Empty;
                string fullCategory = string.IsNullOrEmpty(baseCategory)
                    ? methodCategory
                    : string.IsNullOrEmpty(methodCategory)
                        ? baseCategory
                        : $"{baseCategory}/{methodCategory}";

                var cases = method.GetCustomAttributes<CaseAttribute>().ToList();
                var methodParameters = method.GetParameters();

                if (cases.Count == 0) {
                    testCases.Add(new TestCase {
                        IsDefault = true,
                        CaseName = null,
                        TestName = testAttribute.Name ?? method.Name,
                        NamespaceName = declaringType.Namespace ?? string.Empty,
                        ClassName = declaringType.Name,
                        MethodName = method.Name,
                        FullCategoryPath = fullCategory
                    });
                    continue;
                }

                foreach (var caseAttribute in cases) {
                    testCases.Add(new TestCase  {
                        CaseName = caseAttribute.Name ?? "Case" + testCases.Count,
                        TestName = testAttribute.Name ?? method.Name,
                        NamespaceName = declaringType.Namespace ?? string.Empty,
                        ClassName = declaringType.Name,
                        MethodName = method.Name,
                        FullCategoryPath = fullCategory,
                        Parameters = [.. methodParameters.Select((parameterInfo, index) => new ParameterData
                        {
                            ParameterType = parameterInfo.ParameterType,
                            ParameterName = parameterInfo.Name ?? $"Param{index + 1}",
                            ParameterValue = ResolveParameterValue(caseAttribute.Parameters[index], parameterInfo, declaringType)
                        })]
                    });
                }
            }
        }

        return testCases;
    }

    private static object? ResolveParameterValue(object? providedValue, ParameterInfo parameter, Type declaringType)
    {
        if (parameter.ParameterType.IsAssignableFrom(providedValue?.GetType()))
            return providedValue;

        var error = new InvalidOperationException(
                $"Cannot resolve value for parameter '{parameter.Name}' of type '{parameter.ParameterType.PrettyType()}' " +
                $"from provided value '{providedValue}'");

        // If the provided value is a string and the types don't match, try resolving as a static variable
        if (providedValue is not string variableName)
            throw error;


        var field = declaringType.GetField(variableName, BindingFlags.Public | BindingFlags.Static);
        if (field != null && parameter.ParameterType.IsAssignableFrom(field.FieldType))
            return field.GetValue(null); // Static field value

        var property = declaringType.GetProperty(variableName, BindingFlags.Public | BindingFlags.Static);
        if (property != null && parameter.ParameterType.IsAssignableFrom(property.PropertyType))
            return property.GetValue(null); // Static property value

        throw error;
    }
}