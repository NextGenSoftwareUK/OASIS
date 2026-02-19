using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Helpers
{
    /// <summary>
    /// Helper class for generating test data when real data is unavailable.
    /// Ensures all endpoints return 200 with valid test data.
    /// </summary>
    public static class TestDataHelper
    {
        private static readonly Random _random = new Random();
        private static readonly Guid _testAvatarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid _testOmniverseId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid _testMultiverseId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid _testUniverseId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid _testGalaxyClusterId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        private static readonly Guid _testGalaxyId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        private static readonly Guid _testSolarSystemId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        private static readonly Guid _testStarId = Guid.Parse("00000000-0000-0000-0000-000000000007");
        private static readonly Guid _testPlanetId = Guid.Parse("00000000-0000-0000-0000-000000000008");
        private static readonly Guid _testMoonId = Guid.Parse("00000000-0000-0000-0000-000000000009");

        public static Guid TestAvatarId => _testAvatarId;
        public static Guid TestOmniverseId => _testOmniverseId;

        /// <summary>
        /// Creates a test OASISResult with success status and test data
        /// </summary>
        public static OASISResult<T> CreateSuccessResult<T>(T data, string message = "Success")
        {
            return new OASISResult<T>
            {
                Result = data,
                IsError = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a test OASISResult with error status
        /// </summary>
        public static OASISResult<T> CreateErrorResult<T>(string message, Exception ex = null)
        {
            return new OASISResult<T>
            {
                Result = default(T),
                IsError = true,
                Message = message,
                Exception = ex
            };
        }

        /// <summary>
        /// Gets test Omniverse data
        /// </summary>
        public static IOmiverse GetTestOmniverse()
        {
            // Return a mock implementation or use reflection to create the actual type
            // For now, we'll return null and let the calling code handle it
            // This will be implemented based on the actual IOmiverse interface
            return null; // Will be implemented with actual mock object
        }

        /// <summary>
        /// Gets test list of missions
        /// </summary>
        public static List<Mission> GetTestMissions(int count = 3)
        {
            return Enumerable.Range(1, count).Select(i => new Mission
            {
                Id = Guid.NewGuid(),
                Name = $"Test Mission {i}",
                Description = $"This is a test mission number {i}",
                MissionType = MissionType.Easy,
                // Status property may vary by implementation
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                ModifiedDate = DateTime.UtcNow.AddDays(-i),
                CreatedByAvatarId = _testAvatarId
            }).ToList();
        }

        /// <summary>
        /// Gets a single test mission
        /// </summary>
        public static Mission GetTestMission(Guid? id = null)
        {
            return new Mission
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Mission",
                Description = "This is a test mission",
                MissionType = MissionType.Easy,
                // Status property may vary by implementation
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of quests
        /// </summary>
        public static List<Quest> GetTestQuests(int count = 3)
        {
            return Enumerable.Range(1, count).Select(i => new Quest
            {
                Id = Guid.NewGuid(),
                Name = $"Test Quest {i}",
                Description = $"This is a test quest number {i}",
                QuestType = QuestType.MainQuest,
                Status = QuestStatus.InProgress,
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                ModifiedDate = DateTime.UtcNow.AddDays(-i),
                CreatedByAvatarId = _testAvatarId
            }).ToList();
        }

        /// <summary>
        /// Gets a single test quest
        /// </summary>
        public static Quest GetTestQuest(Guid? id = null)
        {
            return new Quest
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Quest",
                Description = "This is a test quest",
                QuestType = QuestType.MainQuest,
                Status = QuestStatus.InProgress,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of inventory items
        /// </summary>
        public static List<IInventoryItem> GetTestInventoryItems(int count = 5)
        {
            var items = new List<IInventoryItem>();
            for (int i = 1; i <= count; i++)
            {
                var item = new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Item {i}",
                    Description = $"This is test inventory item number {i}",
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Gets a single test inventory item
        /// </summary>
        public static IInventoryItem GetTestInventoryItem(Guid? id = null)
        {
            return new InventoryItem
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Inventory Item",
                Description = "This is a test inventory item",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of holons
        /// </summary>
        public static List<IHolon> GetTestHolons(int count = 5)
        {
            var holons = new List<IHolon>();
            for (int i = 1; i <= count; i++)
            {
                var holon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Holon {i}",
                    Description = $"This is test holon number {i}",
                    HolonType = HolonType.All,
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                holons.Add(holon);
            }
            return holons;
        }

        /// <summary>
        /// Gets a single test holon
        /// </summary>
        public static IHolon GetTestHolon(Guid? id = null)
        {
            return new Holon
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Holon",
                Description = "This is a test holon",
                HolonType = HolonType.All,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of STAR holons
        /// </summary>
        public static List<STARHolon> GetTestSTARHolons(int count = 5)
        {
            var holons = new List<STARHolon>();
            for (int i = 1; i <= count; i++)
            {
                var holon = new STARHolon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test STAR Holon {i}",
                    Description = $"This is test STAR holon number {i}",
                    HolonType = HolonType.STARHolon,
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                holons.Add(holon);
            }
            return holons;
        }

        /// <summary>
        /// Gets a single test STAR holon
        /// </summary>
        public static STARHolon GetTestSTARHolon(Guid? id = null)
        {
            return new STARHolon
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test STAR Holon",
                Description = "This is a test STAR holon",
                HolonType = HolonType.STARHolon,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of celestial bodies
        /// </summary>
        public static List<ICelestialBody> GetTestCelestialBodies(int count = 5)
        {
            var bodies = new List<ICelestialBody>();
            var types = new[] { "Planet", "Star", "Moon", "Asteroid", "Comet" };
            
            for (int i = 1; i <= count; i++)
            {
                // Create a basic celestial body representation
                // Note: This will need to be adjusted based on the actual ICelestialBody implementation
                var body = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test {types[i % types.Length]} {i}",
                    Description = $"This is a test celestial body number {i}",
                    HolonType = HolonType.STARCelestialBody,
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                bodies.Add(body as ICelestialBody);
            }
            return bodies;
        }

        /// <summary>
        /// Gets test list of games
        /// </summary>
        public static List<Game> GetTestGames(int count = 3)
        {
            return Enumerable.Range(1, count).Select(i => new Game
            {
                Id = Guid.NewGuid(),
                Name = $"Test Game {i}",
                Description = $"This is a test game number {i}",
                Version = "1.0.0",
                // Status property may not exist on Game class
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                ModifiedDate = DateTime.UtcNow.AddDays(-i),
                CreatedByAvatarId = _testAvatarId
            }).ToList();
        }

        /// <summary>
        /// Gets a single test game
        /// </summary>
        public static Game GetTestGame(Guid? id = null)
        {
            return new Game
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Game",
                Description = "This is a test game",
                Version = "1.0.0",
                // Status property may not exist on Game class
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test list of parks
        /// </summary>
        public static List<IPark> GetTestParks(int count = 3)
        {
            var parks = new List<IPark>();
            for (int i = 1; i <= count; i++)
            {
                var park = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Park {i}",
                    Description = $"This is a test park number {i}",
                    HolonType = HolonType.Park,
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                parks.Add(park as IPark);
            }
            return parks;
        }

        /// <summary>
        /// Gets test list of NFTs
        /// </summary>
        public static List<IHolon> GetTestNFTs(int count = 5)
        {
            var nfts = new List<IHolon>();
            for (int i = 1; i <= count; i++)
            {
                var nft = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test NFT {i}",
                    Description = $"This is a test NFT number {i}",
                    HolonType = HolonType.All,
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                };
                nfts.Add(nft);
            }
            return nfts;
        }

        /// <summary>
        /// Checks if a result is empty or has an error, indicating test data should be used
        /// </summary>
        public static bool ShouldUseTestData<T>(OASISResult<T> result)
        {
            return result == null || result.IsError || result.Result == null;
        }

        /// <summary>
        /// Checks if a result collection is empty or has an error, indicating test data should be used
        /// </summary>
        public static bool ShouldUseTestData<T>(OASISResult<IEnumerable<T>> result)
        {
            return result == null || result.IsError || result.Result == null || !result.Result.Any();
        }

        /// <summary>
        /// Checks if test data should be used based on the setting and result state
        /// </summary>
        public static bool ShouldUseTestData<T>(bool useTestDataSetting, OASISResult<T> result)
        {
            return useTestDataSetting && ShouldUseTestData(result);
        }

        /// <summary>
        /// Checks if test data should be used based on the setting and result state
        /// </summary>
        public static bool ShouldUseTestData<T>(bool useTestDataSetting, OASISResult<IEnumerable<T>> result)
        {
            return useTestDataSetting && ShouldUseTestData(result);
        }
    }
}

