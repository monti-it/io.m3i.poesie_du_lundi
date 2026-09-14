using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Domain;

public sealed record PoemPublished(Guid PoemId, DateOnly PublicationDate) : IDomainEvent;
