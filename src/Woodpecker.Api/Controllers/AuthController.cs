using MediatR;
using Microsoft.AspNetCore.Mvc;
using Woodpecker.Api.Contracts;
using Woodpecker.Application.Auth;

namespace Woodpecker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new RegisterUserCommand(request.Email, request.Password), cancellationToken);
        return Ok(new RegisterResponse(id));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(new LoginResponse(token));
    }
}
