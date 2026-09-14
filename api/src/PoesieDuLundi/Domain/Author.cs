using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Domain;

/// <summary>
/// The person publishing under a given SSO subject (the <c>Remote-User</c> header —
/// <see cref="Api.Admin.ForwardAuthIdentityProvider"/>). <see cref="ProvisionFor"/> is the only
/// way an <c>Author</c> comes into existence: the first admin request from an unknown subject
/// auto-provisions a minimal author (display name = subject) rather than requiring an explicit
/// sign-up step. It takes the existing author for that subject, if any, as a fact the caller
/// already knows — it looks up nothing itself — and returns it unchanged instead of provisioning
/// a duplicate.
/// </summary>
public sealed class Author : AggregateRoot
{
    public string Subject { get; }
    public string DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }

    public Author(string subject, string displayName, string? bio = null, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        Subject = subject.Trim();
        DisplayName = displayName.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
    }

    /// <summary>
    /// Resolves the author for <paramref name="subject"/>: <paramref name="existingAuthor"/>
    /// unchanged when that subject is already known, otherwise a freshly provisioned minimal
    /// author (display name = subject, no bio/avatar) that the caller is responsible for
    /// persisting.
    /// </summary>
    public static Author ProvisionFor(string subject, Author? existingAuthor)
    {
        return existingAuthor ?? new Author(subject, subject);
    }

    public void UpdateProfile(string displayName, string? bio, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
    }
}
