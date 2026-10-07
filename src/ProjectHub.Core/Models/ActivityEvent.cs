namespace ProjectHub.Core.Models;

public sealed record ActivityEvent(DateTimeOffset Date, string Kind, string Description);
