using Application.Contracts;
using Application.DTOs.Trust;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace Infrastructure.ImplementationContract;

public class TrustService : ITrustService
{
    private readonly DataContext _context;
    private readonly HttpClient _httpClient;

    public TrustService(DataContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<TrustResponse> CreateTrustAsync(CreateTrustRequest request)
    {
        var trust = new Trust
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SettlorName = request.SettlorName,
            TrusteeName = request.TrusteeName,
            TrustProtector = request.TrustProtector,
            QualifiedCustodian = request.QualifiedCustodian,
            Jurisdiction = request.Jurisdiction,
            DurationYears = request.DurationYears,
            TrustType = request.TrustType,
            MinimumPropertyValue = request.MinimumPropertyValue,
            AnnualDistributionRate = request.AnnualDistributionRate,
            ReserveFundRate = request.ReserveFundRate,
            TrusteeFeeRate = request.TrusteeFeeRate,
            Status = EntityStatus.Active
        };

        _context.Trusts.Add(trust);
        await _context.SaveChangesAsync();

        return new TrustResponse(
            trust.Id,
            trust.Name,
            trust.SettlorName,
            trust.TrusteeName,
            trust.TrustProtector,
            trust.QualifiedCustodian,
            trust.Jurisdiction,
            trust.DurationYears,
            trust.TrustType,
            trust.MinimumPropertyValue,
            trust.AnnualDistributionRate,
            trust.ReserveFundRate,
            trust.TrusteeFeeRate,
            trust.Status.ToString(),
            trust.CreatedAt.DateTime,
            trust.UpdatedAt?.DateTime
        );
    }

    public async Task<TrustResponse> GetTrustAsync(Guid id)
    {
        var trust = await _context.Trusts.FindAsync(id);
        if (trust == null)
            throw new ArgumentException("Trust not found");

        return new TrustResponse(
            trust.Id,
            trust.Name,
            trust.SettlorName,
            trust.TrusteeName,
            trust.TrustProtector,
            trust.QualifiedCustodian,
            trust.Jurisdiction,
            trust.DurationYears,
            trust.TrustType,
            trust.MinimumPropertyValue,
            trust.AnnualDistributionRate,
            trust.ReserveFundRate,
            trust.TrusteeFeeRate,
            trust.Status.ToString(),
            trust.CreatedAt.DateTime,
            trust.UpdatedAt?.DateTime
        );
    }

    public async Task<PropertyResponse> AddPropertyAsync(Guid trustId, AddPropertyRequest request)
    {
        var trust = await _context.Trusts.FindAsync(trustId);
        if (trust == null)
            throw new ArgumentException("Trust not found");

        var netIncome = request.AnnualRentalIncome - request.AnnualExpenses;
        var tokenSupply = request.TotalSquareFootage;
        var tokenPrice = request.PropertyValue / tokenSupply;

        var property = new TrustProperty
        {
            Id = Guid.NewGuid(),
            TrustId = trustId,
            PropertyAddress = request.PropertyAddress,
            PropertyType = request.PropertyType,
            PropertyValue = request.PropertyValue,
            TotalSquareFootage = request.TotalSquareFootage,
            AnnualRentalIncome = request.AnnualRentalIncome,
            AnnualExpenses = request.AnnualExpenses,
            NetIncome = netIncome,
            TokenSupply = tokenSupply,
            TokenPrice = tokenPrice,
            TokenNaming = $"{trust.Name}-{request.PropertyAddress.Replace(" ", "-").ToUpper()}-{request.PropertyValue}-{tokenSupply}",
            TitleDocumentHash = request.TitleDocumentHash,
            AppraisalDocumentHash = request.AppraisalDocumentHash,
            InsuranceDocumentHash = request.InsuranceDocumentHash,
            SurveyDocumentHash = request.SurveyDocumentHash,
            Status = EntityStatus.Active
        };

        _context.TrustProperties.Add(property);
        await _context.SaveChangesAsync();

        return new PropertyResponse(
            property.Id,
            property.TrustId,
            property.PropertyAddress,
            property.PropertyType,
            property.PropertyValue,
            property.TotalSquareFootage,
            property.AnnualRentalIncome,
            property.AnnualExpenses,
            property.NetIncome,
            property.TokenSupply,
            property.TokenPrice,
            property.TokenNaming,
            property.Status.ToString(),
            property.CreatedAt.DateTime
        );
    }

    public async Task<ContractGenerationResponse> GenerateContractAsync(Guid trustId, GenerateContractRequest request)
    {
        var trust = await _context.Trusts
            .Include(t => t.Properties)
            .FirstOrDefaultAsync(t => t.Id == trustId);

        if (trust == null)
            throw new ArgumentException("Trust not found");

        var property = trust.Properties.FirstOrDefault();
        if (property == null)
            throw new ArgumentException("No property found for trust");

        // Prepare data for Smart Contract Generator
        var contractData = new
        {
            contractType = request.ContractType,
            blockchain = request.Blockchain,
            trustData = new
            {
                trustName = trust.Name,
                settlorName = trust.SettlorName,
                trusteeName = trust.TrusteeName,
                trustProtector = trust.TrustProtector,
                qualifiedCustodian = trust.QualifiedCustodian,
                jurisdiction = trust.Jurisdiction,
                durationYears = trust.DurationYears,
                trustType = trust.TrustType,
                minimumPropertyValue = trust.MinimumPropertyValue,
                annualDistributionRate = trust.AnnualDistributionRate,
                reserveFundRate = trust.ReserveFundRate,
                trusteeFeeRate = trust.TrusteeFeeRate
            },
            propertyData = new
            {
                propertyAddress = property.PropertyAddress,
                propertyType = property.PropertyType,
                propertyValue = property.PropertyValue,
                totalSquareFootage = property.TotalSquareFootage,
                annualRentalIncome = property.AnnualRentalIncome,
                annualExpenses = property.AnnualExpenses,
                netIncome = property.NetIncome,
                tokenSupply = property.TokenSupply,
                tokenPrice = property.TokenPrice,
                tokenNaming = property.TokenNaming
            },
            tokenData = new
            {
                tokenName = request.TokenName ?? property.TokenNaming,
                tokenSymbol = request.TokenSymbol ?? "PROP",
                tokenStandard = "ERC721",
                totalSupply = request.TotalSupply ?? property.TokenSupply,
                tokenPrice = request.TokenPrice ?? property.TokenPrice,
                minimumPurchase = 1,
                requiresNotarization = request.RequiresNotarization,
                transferRestrictions = request.TransferRestrictions ?? new[] { "Wyoming notarization required" },
                blockchain = request.Blockchain
            }
        };

        // Call Smart Contract Generator API
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5257/api/v1/contracts/generate", contractData);
        var contractContent = await response.Content.ReadAsStringAsync();

        // Create contract generation record
        var contractGeneration = new ContractGeneration
        {
            Id = Guid.NewGuid(),
            TrustId = trustId,
            PropertyId = property.Id,
            ContractType = request.ContractType,
            Blockchain = request.Blockchain,
            GeneratedContract = contractContent,
            ContractHash = $"Qm{Guid.NewGuid():N}",
            DeploymentStatus = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        _context.ContractGenerations.Add(contractGeneration);
        await _context.SaveChangesAsync();

        return new ContractGenerationResponse(
            contractGeneration.Id,
            contractGeneration.TrustId,
            contractGeneration.PropertyId,
            contractGeneration.ContractType,
            contractGeneration.Blockchain,
            contractGeneration.GeneratedContract,
            contractGeneration.ContractHash,
            contractGeneration.AbiData,
            contractGeneration.BytecodeData,
            contractGeneration.DeploymentAddress,
            contractGeneration.DeploymentStatus,
            contractGeneration.ErrorMessage,
            contractGeneration.GeneratedAt,
            contractGeneration.DeployedAt
        );
    }

    public async Task<IEnumerable<ContractGenerationResponse>> GetContractsAsync(Guid trustId)
    {
        var contracts = await _context.ContractGenerations
            .Where(c => c.TrustId == trustId)
            .ToListAsync();

        return contracts.Select(c => new ContractGenerationResponse(
            c.Id,
            c.TrustId,
            c.PropertyId,
            c.ContractType,
            c.Blockchain,
            c.GeneratedContract,
            c.ContractHash,
            c.AbiData,
            c.BytecodeData,
            c.DeploymentAddress,
            c.DeploymentStatus,
            c.ErrorMessage,
            c.GeneratedAt,
            c.DeployedAt
        ));
    }
}
