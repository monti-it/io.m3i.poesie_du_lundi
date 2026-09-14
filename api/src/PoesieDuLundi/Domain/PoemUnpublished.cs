using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Domain;

public sealed record PoemUnpublished(Guid PoemId) : IDomainEvent;
