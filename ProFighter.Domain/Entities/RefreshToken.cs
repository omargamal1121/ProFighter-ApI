using System;
using ProFighter.Domain.Common;

namespace ProFighter.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    /// <summary>Raw token value — used only for returning the existing token on reuse. Not indexed.</summary>
    public string Token { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public string SecurityStamp { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}
