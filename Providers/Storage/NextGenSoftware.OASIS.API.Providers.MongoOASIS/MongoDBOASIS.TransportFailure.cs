using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IProviderTransportFailureClassifier
{
    public bool IsProviderTransportFailure(Exception failure)
        => failure is MongoAccountReadTimeoutException;
}
