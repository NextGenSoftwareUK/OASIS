namespace Infrastructure.ImplementationContract;

public sealed class RwaTokenService(
    DataContext dbContext,
    ILogger<RwaTokenService> logger,
    IHttpContextAccessor accessor,
    ISolanaNftManager solanaNftManager //I will use the pattern factory
) : IRwaTokenService
{
    public async Task<Result<PagedResponse<IEnumerable<GetRwaTokensResponse>>>>
        GetAllAsync(RwaTokenFilter filter, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetAllAsync), date);

        try
        {
            IQueryable<RwaToken> query = dbContext.RwaTokens.AsNoTracking();
            if (filter.AssetType is not null)
                query = query.Where(x => x.AssetType == filter.AssetType);
            if (filter.PriceMin is not null)
                query = query.Where(x => x.Price >= filter.PriceMin);
            if (filter.PriceMax is not null)
                query = query.Where(x => x.Price <= filter.PriceMax);
            if (filter.SortBy is not null)
            {
                query = filter.SortBy switch
                {
                    SortBy.Price => query.OrderBy(x => x.Price),
                    SortBy.CreatedAt => query.OrderBy(x => x.CreatedAt),
                    _ => query
                };
            }

            if (filter.SortOrder is not null)
            {
                query = filter.SortOrder switch
                {
                    SortOrder.Asc => query.OrderBy(x => x),
                    SortOrder.Desc => query.OrderByDescending(x => x),
                    _ => query
                };
            }

            int totalCount = await query.CountAsync(token);

            PagedResponse<IEnumerable<GetRwaTokensResponse>> pagedResult =
                PagedResponse<IEnumerable<GetRwaTokensResponse>>.Create(
                    filter.PageSize,
                    filter.PageNumber,
                    totalCount,
                    query
                        .OrderBy(x => x.Id)
                        .Page(filter.PageNumber, filter.PageSize)
                        .Select(x => x.ToRead()).ToList());

            logger.OperationCompleted(nameof(GetAllAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokensResponse>>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetAllAsync), ex.Message);
            logger.OperationCompleted(nameof(GetAllAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokensResponse>>>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<GetRwaTokenDetailResponse>>
        GetDetailAsync(Guid id, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetDetailAsync), date);

        try
        {
            // This query will be optimized later. For now, it retrieves RWA tokens along with related virtual accounts, users, and networks.
            GetRwaTokenDetailResponse? rwaToken = await dbContext.RwaTokens
#nullable disable
                .AsNoTrackingWithIdentityResolution()
                .Include(x => x.VirtualAccount)
                .ThenInclude(x => x.Network)
                .Include(x => x.VirtualAccount)
                .ThenInclude(x => x!.User)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(x => x.User)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(x => x.Network)
#nullable restore
                .Where(x => x.Id == id)
                .Select(x => x.ToReadDetail()).FirstOrDefaultAsync(token);

            if (rwaToken is null)
            {
                logger.OperationCompleted(nameof(GetDetailAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<GetRwaTokenDetailResponse>.Failure(
                    ResultPatternError.NotFound(Messages.RwaTokenNotFound));
            }


            logger.OperationCompleted(nameof(GetDetailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<GetRwaTokenDetailResponse>.Success(rwaToken);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetDetailAsync), ex.Message);
            logger.OperationCompleted(nameof(GetDetailAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<GetRwaTokenDetailResponse>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<PagedResponse<IEnumerable<GetRwaTokenDetailResponse>>>>
        GetTokensOwnedByCurrentUserAsync(RwaTokenOwnerFilter filter, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetTokensOwnedByCurrentUserAsync), date);
        try
        {
            Guid userId = accessor.GetId();

            IQueryable<GetRwaTokenDetailResponse> query = dbContext.RwaTokens
                .AsNoTrackingWithIdentityResolution()
#nullable disable
                .Include(x => x.VirtualAccount)
                .ThenInclude(va => va.User)
                .Include(x => x.VirtualAccount)
                .ThenInclude(va => va.Network)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(wla => wla.User)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(wla => wla.Network)
#nullable restore
                .Where(x =>
                    (x.VirtualAccount != null && x.VirtualAccount.UserId == userId) ||
                    (x.WalletLinkedAccount != null && x.WalletLinkedAccount.UserId == userId))
                .WhereIf(filter.RwaId != null, x => x.Id == filter.RwaId)
                .Select(x => x.ToReadDetail());
            int totalCount = await query.CountAsync(token);
            PagedResponse<IEnumerable<GetRwaTokenDetailResponse>> response =
                PagedResponse<IEnumerable<GetRwaTokenDetailResponse>>.Create(filter.PageSize, filter.PageNumber,
                    totalCount, query.Page(filter.PageNumber, filter.PageSize));

            logger.OperationCompleted(nameof(GetTokensOwnedByCurrentUserAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokenDetailResponse>>>.Success(response);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetTokensOwnedByCurrentUserAsync), ex.Message);
            logger.OperationCompleted(nameof(GetTokensOwnedByCurrentUserAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokenDetailResponse>>>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<CreateRwaTokenResponse>>
        CreateAsync(CreateRwaTokenRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateAsync), date);

        try
        {
            Result<CreateRwaTokenResponse> responseValidation = request.CreateValidateNftFields();
            if (!responseValidation.IsSuccess) return responseValidation;

            Guid userId = accessor.GetId();

            bool existingUser = await dbContext.Users.AnyAsync(x
                => x.Id == userId, token);
            if (!existingUser)
                return Result<CreateRwaTokenResponse>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));

            bool existingNetwork =
                await dbContext.Networks.AnyAsync(x => x.Name == request.Network, token);
            if (!existingNetwork)
                return Result<CreateRwaTokenResponse>.Failure(ResultPatternError.NotFound(Messages.NetworkNotFound));

            Guid? vaId = await (from u in dbContext.Users
                    join va in dbContext.VirtualAccounts on u.Id equals va.UserId
                    join n in dbContext.Networks on va.NetworkId equals n.Id
                    where u.Id == userId && n.Name == request.Network
                    select va.Id
                ).FirstOrDefaultAsync(token);
            if (vaId is null)
                return Result<CreateRwaTokenResponse>.Failure(
                    ResultPatternError.NotFound(Messages.VirtualAccountNotFound));

            Common.DTOs.Nft nft = new()
            {
                Name = request.Title,
                Royality = request.Royalty.ToString(),
                Symbol = request.UniqueIdentifier,
                Url = request.ProofOfOwnershipDocument,
                Description = request.AssetDescription,
                ImageUrl = request.Image
            };
            Result<NftMintingResponse> mintingResult = await solanaNftManager.MintAsync(nft, token);
            if (!mintingResult.IsSuccess)
                return Result<CreateRwaTokenResponse>.Failure(mintingResult.Error);

            RwaToken newRwaToken = request.ToEntity(accessor, mintingResult.Value!, (Guid)vaId);
            await dbContext.RwaTokens.AddAsync(newRwaToken, token);

            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<CreateRwaTokenResponse>.Success(newRwaToken.ToCreateResponse())
                : Result<CreateRwaTokenResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.CreateRwaTokenFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateRwaTokenResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<UpdateRwaTokenResponse>> UpdateAsync(Guid id, UpdateRwaTokenRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateAsync), date);

        try
        {
            Guid userId = accessor.GetId();

            Result<UpdateRwaTokenResponse> responseValidation = request.UpdateValidateNftFields();
            if (!responseValidation.IsSuccess) return responseValidation;


            RwaToken? rwaToken = await dbContext.RwaTokens
#nullable disable
                .Include(x => x.VirtualAccount)
                .ThenInclude(x => x.Network)
                .Include(x => x.VirtualAccount)
                .ThenInclude(x => x!.User)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(x => x.User)
                .Include(x => x.WalletLinkedAccount)
                .ThenInclude(x => x.Network)
#nullable restore
                .Where(x => x.Id == id).FirstOrDefaultAsync(token);
            
            if (rwaToken is null)
                return Result<UpdateRwaTokenResponse>.Failure(ResultPatternError.NotFound(Messages.RwaTokenNotFound));

            if ((rwaToken.VirtualAccount != null && rwaToken.VirtualAccount.UserId != userId) ||
                (rwaToken.WalletLinkedAccount != null && rwaToken.WalletLinkedAccount.UserId != userId))
                return Result<UpdateRwaTokenResponse>.Failure(
                    ResultPatternError.AccessDenied(Messages.UpdateRwaTokenForbidden));

            Guid rwaOwnerId = (Guid)(rwaToken.VirtualAccountId ?? rwaToken.WalletLinkedAccountId)!;
            if (rwaToken.AreRwaTokensEqual(request))
                return Result<UpdateRwaTokenResponse>.Success();

            NftBurnRequest? nftBurnRequest = await (from va in dbContext.VirtualAccounts
                where va.Id == rwaToken.VirtualAccountId
                select new NftBurnRequest
                {
                    MintAddress = rwaToken.MintAccount,
                    OwnerPrivateKey = va.PrivateKey,
                    OwnerPublicKey = va.PublicKey,
                    OwnerSeedPhrase = va.SeedPhrase
                }).FirstOrDefaultAsync(token);
            if (nftBurnRequest is null)
                return Result<UpdateRwaTokenResponse>.Failure(
                    ResultPatternError.NotFound(Messages.VirtualAccountNotFound));

            Result<string> burnResponse = await solanaNftManager.BurnAsync(nftBurnRequest, token);
            if (!burnResponse.IsSuccess)
                return Result<UpdateRwaTokenResponse>.Failure(burnResponse.Error);

            Common.DTOs.Nft nft = new()
            {
                Name = request.Title,
                Royality = request.Royalty.ToString(),
                Symbol = rwaToken.UniqueIdentifier,
                Url = request.ProofOfOwnershipDocument,
                Description = request.AssetDescription,
                ImageUrl = rwaToken.Image
            };

            Result<NftMintingResponse> mintingResult = await solanaNftManager.MintAsync(nft, token);
            if (!mintingResult.IsSuccess)
                return Result<UpdateRwaTokenResponse>.Failure(mintingResult.Error);

            if (request.Price != rwaToken.Price)
            {
                await dbContext.RwaTokenPriceHistories.AddAsync(new()
                {
                    CreatedBy = accessor.GetId(),
                    CreatedByIp = accessor.GetRemoteIpAddress(),
                    OldPrice = rwaToken.Price,
                    NewPrice = request.Price,
                    RwaTokenId = rwaToken.Id,
                    OwnerId = rwaOwnerId,
                }, token);
            }

            rwaToken.ToEntity(request, accessor, mintingResult.Value!);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateRwaTokenResponse>.Success(rwaToken.ToUpdateResponse())
                : Result<UpdateRwaTokenResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.UpdateRwaTokenFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateRwaTokenResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }
}