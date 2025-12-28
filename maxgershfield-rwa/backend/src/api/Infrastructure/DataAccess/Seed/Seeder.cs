using NetworkType = Domain.Enums.NetworkType;
using Role = Domain.Entities.Role;

namespace Infrastructure.DataAccess.Seed;

/// <summary>
/// Handles initial data seeding for the application.
///
/// This class populates the database with essential entities such as:
/// - Roles (e.g., Admin, User)
/// - Users and their Role mappings
/// - Supported Blockchain Networks and Tokens
///
/// The seeding is idempotent: it checks for existing records before inserting,
/// ensuring safe and repeatable execution.
///
/// Useful for system bootstrapping, testing, and CI/CD automation.
///
/// Note: Uses predefined seed data from <see cref="SeedData"/> and <see cref="BridgeSeedData"/>.
/// </summary>

public class Seeder(DataContext dbContext)
{
    public async Task InitialAsync()
    {
        await InitUserAsync();
        await InitRoleAsync();
        await InitUserRoleAsync();

        await InitNetworkAsync();
        await InitNetworkTokenAsync();
    }

    private async Task InitRoleAsync()
    {
        foreach (Role role in SeedData.ListRoles)
        {
            if (!await dbContext.Roles.AnyAsync(x => x.Id == role.Id))
                await dbContext.Roles.AddAsync(role);

            await dbContext.SaveChangesAsync();
        }
    }


    private async Task InitUserAsync()
    {
        foreach (User user in SeedData.ListUsers)
        {
            if (!await dbContext.Users.AnyAsync(x => x.Id == user.Id))
                await dbContext.Users.AddAsync(user);

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task InitUserRoleAsync()
    {
        foreach (UserRole user in SeedData.ListUserRoles)
        {
            if (!await dbContext.UserRoles.AnyAsync(x => x.Id == user.Id))
                await dbContext.UserRoles.AddAsync(user);

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task InitNetworkAsync()
    {
        foreach (Network network in BridgeSeedData.Networks)
        {
            if (!await dbContext.Networks.AnyAsync(x => x.Id == network.Id))
                await dbContext.Networks.AddAsync(network);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task InitNetworkTokenAsync()
    {
        foreach (NetworkToken networkToken in BridgeSeedData.NetworkTokens)
        {
            if (!await dbContext.NetworkTokens.AnyAsync(x => x.Id == networkToken.Id))
                await dbContext.NetworkTokens.AddAsync(networkToken);
            await dbContext.SaveChangesAsync();
        }
    }
}

file static class SeedData
{
    public static readonly Guid SystemId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId = new("33333333-3333-3333-3333-333333333333");

    private static readonly Guid AdminRoleId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserRoleId = new("22222222-2222-2222-2222-222222222222");

    private static readonly Guid UserRoleId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserRoleId2 = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserRoleId3 = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserRoleId4 = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid UserRoleId5 = new("55555555-5555-5555-5555-555555555555");


    public static readonly List<Role> ListRoles =
    [
        new()
        {
            Id = AdminRoleId,
            Name = Roles.Admin,
            RoleKey = Roles.Admin,
            CreatedBy = SystemId
        },
        new()
        {
            Id = UserRoleId,
            Name = Roles.User,
            RoleKey = Roles.User,
            CreatedBy = SystemId
        },
    ];

    public static readonly List<User> ListUsers =
    [
        new()
        {
            Id = SystemId,
            Email = "system@gmail.com",
            PhoneNumber = "+99200000000",
            UserName = "System",
            PasswordHash = HashingUtility.ComputeSha256Hash("11111111")
        },
        new()
        {
            Id = AdminId,
            Email = "admin@gmail.com",
            PhoneNumber = "+992001001001",
            UserName = "Admin",
            CreatedBy = SystemId,
            PasswordHash = HashingUtility.ComputeSha256Hash("12345678")
        },
        new()
        {
            Id = UserId,
            Email = "user@gmail.com",
            PhoneNumber = "+992002002002",
            UserName = "User",
            CreatedBy = SystemId,
            PasswordHash = HashingUtility.ComputeSha256Hash("43211234")
        },
    ];

    public static readonly List<UserRole> ListUserRoles =
    [
        new()
        {
            Id = UserRoleId1,
            UserId = AdminId,
            RoleId = AdminRoleId,
            CreatedBy = SystemId
        },
        new()
        {
            Id = UserRoleId2,
            UserId = AdminId,
            RoleId = UserRoleId,
            CreatedBy = SystemId
        },
        new()
        {
            Id = UserRoleId3,
            UserId = UserId,
            RoleId = UserRoleId,
            CreatedBy = SystemId
        },
        new()
        {
            Id = UserRoleId4,
            UserId = SystemId,
            RoleId = AdminRoleId,
            CreatedBy = SystemId
        },
        new()
        {
            Id = UserRoleId5,
            UserId = SystemId,
            RoleId = UserRoleId,
            CreatedBy = SystemId
        },
    ];
}

file static class BridgeSeedData
{
    private static readonly Guid SolanaNetworkId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RadixNetworkId = new("22222222-2222-2222-2222-222222222222");

    private static readonly Guid SolanaTokenId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RadixTokenId = new("22222222-2222-2222-2222-222222222222");

    public static readonly List<Network> Networks =
    [
        new()
        {
            Id = SolanaNetworkId,
            Name = "Solana",
            Description = "Solana network",
            NetworkType = NetworkType.Solana,
            CreatedBy = SeedData.SystemId
        },
        new()
        {
            Id = RadixNetworkId,
            Name = "Radix",
            Description = "Radix network",
            NetworkType = NetworkType.Radix,
            CreatedBy = SeedData.SystemId
        }
    ];


    public static readonly List<NetworkToken> NetworkTokens =
    [
        new()
        {
            Id = SolanaTokenId,
            Symbol = "SOL",
            Description = "Solana token",
            NetworkId = SolanaNetworkId,
            CreatedBy = SeedData.SystemId
        },
        new()
        {
            Id = RadixTokenId,
            Symbol = "XRD",
            Description = "Radix token",
            NetworkId = RadixNetworkId,
            CreatedBy = SeedData.SystemId
        }
    ];
}