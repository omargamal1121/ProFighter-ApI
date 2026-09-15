using MediatR;
using ProFighter.Application.Auth.Commands.Login;

namespace ProFighter.Application.Auth.Commands.AdminLogin;

public record AdminLoginCommand(
    string MobileNumber,
    string Password) : IRequest<LoginResult>;
