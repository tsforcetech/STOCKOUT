using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Application.Commands;

public class IdentityHandlers
{
    private readonly IIdentityRepository _repository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public IdentityHandlers(IIdentityRepository repository, ITokenGenerator tokenGenerator, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<RegisterResponse>> HandleRegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var result = await _repository.RegisterUserAsync(request.Email, request.Mobile, passwordHash, _passwordHasher.AlgorithmName, ct);
        // Normally emit outbox event here as part of unit of work or inside stored proc.
        if (result is Result<Emcore.IdentityAccess.Domain.Entities.UserAccount> accountResult) {
             return new Result<RegisterResponse>(); // Placeholder
        }
        return new Result<RegisterResponse>(); // Need to properly map results
    }
}
