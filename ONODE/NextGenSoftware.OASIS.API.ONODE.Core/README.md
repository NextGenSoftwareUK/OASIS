# NextGen Software WEB4 OASIS API ONODE Core (Business Logic Layer)

This sit's at a higher level of abstraction over OASIS API Core, and acts as the main Core (Business Logic Layer) to the OASIS API, whereas OASIS API Core is the core architecture and hosts the central managers such as ProviderManager, AvatarManager, HolonManager, etc, This package contains more higher level Managers (such as OLANDManager, MissionManager, ParksManager, MapManager, QuestManager, SEEDSManager, etc) and use cases built on top of OASIS API Core. This module also uses the OASISBootLoader to manage loading and using the OASISDNA/config for the ONODE. OASIS API Core does not use the OASISBootLoader because it is at a lower level in the stack and so interface implementions (OASIS Providers) need to be manually injected into each Manager's constructor (DI) whereas this module and the OASISBootLoader manages all of this for you via the OASISDNA.json config file. Both [OASIS.API.Core.Native.Integrated.EndPoint](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint) &amp; the REST API both use this module/package and provide a wrapper around it and are the recommeneded ways to use the OASIS API.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version):
https://api.oasisweb4.one

Full documentation and source code can be found here:
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!