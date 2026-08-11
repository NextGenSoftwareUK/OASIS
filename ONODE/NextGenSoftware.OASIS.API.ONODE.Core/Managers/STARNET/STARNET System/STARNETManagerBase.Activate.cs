using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4>
    {
    }
}
