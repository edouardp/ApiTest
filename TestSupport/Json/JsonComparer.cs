using System.Text.Json;
using System.Text.RegularExpressions;

namespace TestSupport;

/// <summary>
/// Provides functionality to compare JSON strings with support for tokenized placeholders.
/// This class includes methods to perform exact and subset comparisons between JSON structures,
/// allowing specific values to be extracted dynamically via tokens.
///
/// Tokens are placeholders in the expected JSON, enclosed in double square brackets (e.g., "[[JOBID]]").
/// When a token is encountered, the corresponding value in the actual JSON is extracted and stored.
///
/// Features:
/// - Exact match comparison: Ensures the expected and actual JSON structures are identical, 
///   except for tokenized values.
/// - Subset match comparison: Verifies that all elements in the expected JSON exist within 
///   the actual JSON, without requiring a full match.
/// - Token extraction: Captures values corresponding to tokens in the expected JSON for further processing.
/// - Detailed mismatch reporting: Provides structured information on any differences found.
///
/// Example Usage:
/// <code>
///   string expectedJson = """{ "id": "[[JOBID]]", "status": "complete" }""";
///   string actualJson = """{ "id": "12345", "status": "complete" }""";
///
///   bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
///
///   Console.WriteLine(isMatch); // True
///   Console.WriteLine(extractedValues["JOBID"]); // 12345
/// </code>
///
/// </summary>
public static partial class JsonComparer
{
    // Regex to match boxed tokens in expected JSON (e.g. "[[JOBID]]")
    [GeneratedRegex(@"^\[\[(\w+)\]\]$", RegexOptions.Compiled)]
    private static partial Regex TokenRegexGenerator();
    private static readonly Regex TokenRegex = TokenRegexGenerator();  

    // Regex to find tokens that are not already enclosed in quotes.
    // This regex looks for the pattern [[VARIABLE]] that is not immediately preceded or followed by a double quote.
    [GeneratedRegex("(?<!\\\")\\[\\[(\\w+)\\]\\](?!\\\")", RegexOptions.Compiled)]
    private static partial Regex UnquotedTokenRegexGenerator();
    private static readonly Regex UnquotedTokenRegex = UnquotedTokenRegexGenerator();

    /// <summary>
    /// Compares the two JSON strings for an exact match.
    /// Returns true if they match exactly (except for tokens), false otherwise.
    /// Also extracts any token values (e.g. JOBID) into extractedValues and records mismatch details.
    /// </summary>
    public static bool ExactMatch(string expectedJson, string actualJson,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: false, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Compares the two JSON strings for a subset match (i.e. expected is a subset of actual).
    /// Returns true if all elements in expected (except tokens) are found in actual.
    /// Also extracts any token values into extractedValues and records mismatch details.
    /// </summary>
    public static bool SubsetMatch(string expectedJson, string actualJson,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        return Compare(expectedJson, actualJson, subsetMode: true, out extractedValues, out mismatches);
    }

    /// <summary>
    /// Parses the JSON strings and calls the recursive CompareElements function.
    /// It pre-processes the expected JSON string by wrapping unquoted tokens with quotes.
    /// </summary>
    private static bool Compare(string expectedJson, string actualJson, bool subsetMode,
        out Dictionary<string, JsonElement> extractedValues, out List<string> mismatches)
    {
        extractedValues = [];
        mismatches = [];

        // Pre-process the expected JSON to ensure any token of the form {{VARIABLE}}
        // is wrapped in double quotes if not already.
        expectedJson = UnquotedTokenRegex.Replace(expectedJson, "\"[[$1]]\"");

        using JsonDocument expectedDoc = JsonDocument.Parse(expectedJson);
        using JsonDocument actualDoc = JsonDocument.Parse(actualJson);

        CompareElements(expectedDoc.RootElement, actualDoc.RootElement, "$", subsetMode, extractedValues, mismatches);

        return mismatches.Count == 0;
    }

    /// <summary>
    /// Recursively compares expected and actual JsonElements.
    /// When a moustache token is encountered in expected (e.g. "{{JOBID}}"),
    /// the actual value is extracted into extractedValues and no further comparison is done at that node.
    /// </summary>
    private static void CompareElements(JsonElement expected, JsonElement actual, string jsonPath, bool subsetMode,
        Dictionary<string, JsonElement> extractedValues, List<string> mismatches)
    {
        // Check for token match when expected is a string
        if (expected.ValueKind == JsonValueKind.String)
        {
            string? expectedStr = expected.GetString();
            if (!string.IsNullOrEmpty(expectedStr))
            {
                var match = TokenRegex.Match(expectedStr);
                if (match.Success)
                {
                    // It's a token, extract the actual value (of any type) and return.
                    string tokenName = match.Groups[1].Value;
                    extractedValues[tokenName] = actual.Clone();
                    return;
                }
            }
        }

        // Check that both nodes are of the same JSON type.
        if (expected.ValueKind != actual.ValueKind)
        {
            if ((expected.ValueKind == JsonValueKind.True && actual.ValueKind == JsonValueKind.False) ||
                (expected.ValueKind == JsonValueKind.True && actual.ValueKind == JsonValueKind.False))
            {
                mismatches.Add($"{jsonPath}: Boolean mismatch. Expected {expected.GetBoolean()}, got {actual.GetBoolean()}.");
            }
            else
            {
                mismatches.Add($"{jsonPath}: Type mismatch. Expected {expected.ValueKind}, got {actual.ValueKind}.");
            }
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                // For objects, each expected property must exist in actual.
                foreach (JsonProperty prop in expected.EnumerateObject())
                {
                    if (!actual.TryGetProperty(prop.Name, out JsonElement actualProp))
                    {
                        mismatches.Add($"{jsonPath}: Missing property '{prop.Name}'.");
                    }
                    else
                    {
                        CompareElements(prop.Value, actualProp, $"{jsonPath}.{prop.Name}", subsetMode, extractedValues, mismatches);
                    }
                }
                // For an exact match, check that actual does not have extra properties.
                if (!subsetMode)
                {
                    foreach (JsonProperty prop in actual.EnumerateObject())
                    {
                        if (!expected.TryGetProperty(prop.Name, out _))
                        {
                            mismatches.Add($"{jsonPath}: Extra property '{prop.Name}' found in actual JSON.");
                        }
                    }
                }
                break;

            case JsonValueKind.Array:
                // For arrays, the expected array must be a prefix of the actual array in subset mode
                // or exactly equal in length for an exact match.
                
                //JsonElement.ArrayEnumerator expectedEnum = expected.EnumerateArray();
                //JsonElement.ArrayEnumerator actualEnum = actual.EnumerateArray();

                List<JsonElement> expectedList = new(expected.EnumerateArray());
                List<JsonElement> actualList = new(actual.EnumerateArray());

                if (!subsetMode && expectedList.Count != actualList.Count)
                {
                    mismatches.Add($"{jsonPath}: Array length mismatch. Expected {expectedList.Count}, got {actualList.Count}.");
                    return;
                }
                if (subsetMode && expectedList.Count > actualList.Count)
                {
                    mismatches.Add($"{jsonPath}: Array length mismatch in subset mode. Expected array with at most {actualList.Count} elements, but expected has {expectedList.Count} elements.");
                    return;
                }
                // Compare each element in expected.
                for (int i = 0; i < expectedList.Count; i++)
                {
                    CompareElements(expectedList[i], actualList[i], $"{jsonPath}[{i}]", subsetMode, extractedValues, mismatches);
                }
                break;

            case JsonValueKind.String:
                // Already handled token case above.
                string? expectedValue = expected.GetString();
                string? actualValue = actual.GetString();
                if (expectedValue != actualValue)
                {
                    mismatches.Add($"{jsonPath}: String mismatch. Expected \"{expectedValue}\", got \"{actualValue}\".");
                }
                break;

            case JsonValueKind.Number:
                // Compare using raw text for numbers
                if (expected.GetRawText() != actual.GetRawText())
                {
                    mismatches.Add($"{jsonPath}: Number mismatch. Expected {expected.GetRawText()}, got {actual.GetRawText()}.");
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                // Both values are the same (either both True or both False)
                break;

            case JsonValueKind.Null:
                // Both are null, so they match.
                break;

            default:
                mismatches.Add($"{jsonPath}: Unsupported JSON value kind: {expected.ValueKind}.");
                break;
        }
    }
}

