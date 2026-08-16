using System;
using Microsoft.AspNetCore.Identity;

namespace ProFighter.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool MustChangePassword { get; set; } = true;

    public bool MustCompleteAccount => MustChangePassword || string.IsNullOrWhiteSpace(Email);

    public void CompleteAccount()
    {
        MustChangePassword = false;
    }
}
