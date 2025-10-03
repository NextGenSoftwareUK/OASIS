using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Search endpoints for querying and searching across all OASIS data.
    /// Provides comprehensive search capabilities across holons, avatars, and other OASIS entities.
    /// </summary>
    //  [Route("api/[search]")]
    [Route("api/[controller]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SearchController : OASISControllerBase
    {
        private SearchManager _SearchManager;

        public SearchController()
        {

        }

        private SearchManager SearchManager
        {
            get
            {
                if (_SearchManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _SearchManager = new SearchManager(result.Result);
                }

                return _SearchManager;
            }
        }

        /// <summary>
        /// Performs a search across all OASIS data using the specified search parameters.
        /// </summary>
        /// <param name="searchParams">The search parameters including query, filters, and options.</param>
        /// <returns>OASIS result containing the search results.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("{searchParams}")]
        [ProducesResponseType(typeof(OASISResult<ISearchResults>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<ISearchResults>> Get(SearchParams searchParams)
        {
            return await SearchManager.SearchAsync(searchParams);
        }

        [HttpGet("{searchParams}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<ISearchResults>> Get(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Get(searchParams);
        }
    }
}
