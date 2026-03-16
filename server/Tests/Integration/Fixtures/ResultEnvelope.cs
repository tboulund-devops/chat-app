namespace Integration.Fixtures;

public sealed record ResultEnvelope<T>(T? Dto, string? Message, int Status);