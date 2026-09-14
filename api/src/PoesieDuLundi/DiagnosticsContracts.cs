namespace PoesieDuLundi;

/// <summary>Response shapes for the root-level diagnostics endpoints.</summary>
public sealed record HealthDto(string Status, string Database);

public sealed record HelloDto(string Message);

/// <summary>Build info for the running instance — the image is tagged with the commit SHA in CI,
/// but nothing bakes that into the binary yet; <see cref="Version"/> is the assembly version until
/// a later issue wires that through.</summary>
public sealed record StatusDto(string Version, string Environment);
