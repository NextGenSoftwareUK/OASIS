using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private static bool SearchRecordMatches(IHolonBase record,bool avatar,ISearchParams search,IEnumerable<IHolon> holons)
        => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(record,avatar,search,holons);
}
