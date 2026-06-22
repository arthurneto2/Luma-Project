using System.Text.Json.Serialization;

namespace Luma.Client.Models;

public record StartRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email
);

public record StartResponse(
    [property: JsonPropertyName("accessToken")] string? AccessToken,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string? Message
);

public record Probe(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("encoding")] string Encoding,
    [property: JsonPropertyName("timeDilationFactor")] double? TimeDilationFactor
);

public record ProbesResponse(
    [property: JsonPropertyName("probes")] Probe[]? Probes,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string? Message
);

public record SyncResponse(
    [property: JsonPropertyName("t1")] string T1,
    [property: JsonPropertyName("t2")] string T2,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string? Message
);

public record Job(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("probeName")] string ProbeName
);

public record JobTakeResponse(
    [property: JsonPropertyName("job")] Job? Job,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string? Message
);

public record JobCheckRequest(
    [property: JsonPropertyName("probeNow")] string ProbeNow,
    [property: JsonPropertyName("roundTrip")] long RoundTrip
);

public record JobCheckResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string? Message
);
