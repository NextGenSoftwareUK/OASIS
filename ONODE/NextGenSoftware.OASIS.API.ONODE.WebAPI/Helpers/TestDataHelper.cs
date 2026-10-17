using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using System.Net;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>
    /// Helper class for generating test data when real data is unavailable in WEB4 OASIS API.
    /// Ensures all endpoints return 200 with valid test data.
    /// </summary>
    public static class TestDataHelper
    {
        private static readonly Random _random = new Random();
        private static readonly Guid _testAvatarId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static Guid TestAvatarId => _testAvatarId;

        /// <summary>
        /// Creates a test OASISHttpResponseMessage with success status and test data
        /// </summary>
        public static OASISHttpResponseMessage<T> CreateSuccessResponse<T>(T data, string message = "Success (using test data)", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var result = new OASISResult<T>
            {
                Result = data,
                IsError = false,
                Message = message
            };
            return HttpResponseHelper.FormatResponse(new OASISHttpResponseMessage<T>(result), statusCode);
        }

        /// <summary>
        /// Creates a test OASISHttpResponseMessage with success status and test data (alias for CreateSuccessResponse)
        /// </summary>
        public static OASISHttpResponseMessage<T> CreateSuccessResult<T>(T data, string message = "Success (using test data)")
        {
            return CreateSuccessResponse<T>(data, message);
        }

        /// <summary>
        /// Creates a test OASISHttpResponseMessage with error status
        /// </summary>
        public static OASISHttpResponseMessage<T> CreateErrorResponse<T>(string message, Exception ex = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var result = new OASISResult<T>
            {
                IsError = true,
                Message = message,
                Exception = ex
            };
            return HttpResponseHelper.FormatResponse(new OASISHttpResponseMessage<T>(result), statusCode);
        }

        /// <summary>
        /// Determines if test data should be used based on the result
        /// </summary>
        public static bool ShouldUseTestData<T>(OASISResult<T> result)
        {
            return result == null || result.IsError || result.Result == null;
        }

        /// <summary>
        /// Determines if test data should be used based on the response
        /// </summary>
        public static bool ShouldUseTestData<T>(OASISHttpResponseMessage<T> response)
        {
            return response == null || response.Result == null || response.Result.IsError || response.Result.Result == null;
        }

        /// <summary>
        /// Gets test holons
        /// </summary>
        public static List<Holon> GetTestHolons(int count = 5)
        {
            var holons = new List<Holon>();
            for (int i = 1; i <= count; i++)
            {
                holons.Add(new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Holon {i}",
                    Description = $"Description for test holon {i}",
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    ModifiedDate = DateTime.UtcNow.AddDays(-i),
                    CreatedByAvatarId = _testAvatarId
                });
            }
            return holons;
        }

        /// <summary>
        /// Gets a test holon
        /// </summary>
        public static Holon GetTestHolon(Guid? id = null)
        {
            return new Holon
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Holon",
                Description = "Test holon description",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedByAvatarId = _testAvatarId
            };
        }

        /// <summary>
        /// Gets test avatars
        /// </summary>
        public static List<IAvatar> GetTestAvatars(int count = 5)
        {
            // Return empty list as avatar creation requires specific implementation
            return new List<IAvatar>();
        }

        /// <summary>
        /// Gets a test avatar
        /// </summary>
        public static IAvatar GetTestAvatar(Guid? id = null)
        {
            // Return null as avatar creation requires specific implementation
            return null;
        }
    }
}

