using System.Text;

namespace TestSupport.HttpFile;

/// <summary>
/// Represents a parsed HTTP header, including its name, value, and any associated parameters.
/// </summary>
public class ParsedHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsedHeader"/> class.
    /// </summary>
    /// <param name="name">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    /// <param name="parameters">A dictionary of additional parameters associated with the header.</param>
    public ParsedHeader(string name, string value, Dictionary<string, string> parameters)
    {
        Name = name;
        Value = value;
        Parameters = parameters ?? [];
    }

    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the header.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the dictionary of parameters associated with the header.
    /// </summary>
    public Dictionary<string, string> Parameters { get; }

    /// <summary>
    /// Returns a string representation of the parsed header, including its name, value, and parameters.
    /// </summary>
    /// <returns>A formatted string representation of the header.</returns>
    public override string ToString()
    {
        // Converts the header and its parameters into a string format.
        // Example output: "Content-Type: text/html; charset=UTF-8"
        return Parameters.Aggregate(
            new StringBuilder($"{Name}: {Value}"),
            (builder, kvp) => builder.Append($"; {kvp.Key}={kvp.Value}")
        ).ToString();
    }
}
