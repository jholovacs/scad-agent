using System.Security.Cryptography;
using System.Text;

namespace ScadAgent.Domain.ValueObjects;

public sealed class ScadSource
{
    public string Content { get; }
    public string Hash { get; }

    public ScadSource(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("SCAD content cannot be empty.", nameof(content));

        Content = content.Trim();
        Hash = ComputeHash(Content);
    }

    public static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
