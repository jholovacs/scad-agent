namespace ScadAgent.Domain.ValueObjects;

public sealed record ValidationIssue(string Message, string? Line = null);
