using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Application.Services;

public class StepUpProofValidator : IStepUpProofValidator
{
    private readonly IIdentityRepository _repository;
    private readonly ITokenGenerator _tokenGenerator;

    public StepUpProofValidator(IIdentityRepository repository, ITokenGenerator tokenGenerator)
    {
        _repository = repository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result> ValidateAndConsumeStepUpProofAsync(string userId, string? sessionId, string targetAction, string proofToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(proofToken) || string.IsNullOrWhiteSpace(targetAction))
        {
            return new Result { IsSuccess = false };
        }

        string proofHash = _tokenGenerator.HashToken(proofToken);
        var consumedProofId = await _repository.ConsumeStepUpProofAsync(proofHash, userId, sessionId, targetAction, cancellationToken);

        if (consumedProofId == null || !consumedProofId.IsSuccess || consumedProofId.Value == null)
        {
            return new Result { IsSuccess = false };
        }

        return new Result();
    }
}
