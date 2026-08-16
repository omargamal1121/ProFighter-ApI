using System;
using System.Threading.Tasks;
using ProFighter.Application.Common.Models.Auth;

namespace ProFighter.Application.Common.Interfaces.Auth;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(TokenGenerationRequest request, bool isAccountCompletion = false);
}
