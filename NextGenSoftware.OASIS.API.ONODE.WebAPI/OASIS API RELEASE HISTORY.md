
# OASIS API RELEASE HISTORY

This needs to be updated whenever we do anywork that will effect the OASIS API (even changes to properties, etc on OASIS.API.CORE objects that are used in the API, etc...
Then when we do a release, this file will be linked to the Swagger documentation. The file is also linked to the WIKI and GitHub Documentation.

----------------------------------------------------------------------------------------------------------------------------
## 0.0.1 ALPHA (10/10/2020)

Initial release of the WEB4 OASIS API.
https://www.ourworldthegame.com/single-post/oasis-api-v0-0-1-altha-live

----------------------------------------------------------------------------------------------------------------------------
## 0.0.2 ALTHA - 0.0.16 ALTHA

Miscellaneous releases made before this change log was created (need to go back through old GitHub commits to fill this out...)
TODO: Also need to go back through to find the dates of all releases below and update the dates...

----------------------------------------------------------------------------------------------------------------------------
## 0.17 ALPHA

- Changed controller methods to return OASISResult (more efficient error handling system used throughout The OASIS with minimal overhead)
- Added ability to call avatar methods or specify avatar parameters by their username or email as well as the existing id way.
- Avatar methods now return id.
- Added new Solana API (and SolanaOASIS Provider)
- Added new Cargo API (and CargoOASIS Provider)
- Added new NFT API (generic API for both SolanaOASIS and CargoOASIS Providers, with more coming soon...)
- Split out all but essential SSO properties (and Karma, Level & Image2D) from Avatar into new AvatarDetail object. AvatarDetail is now what the old Avatar was and Avatar is now a lightweight version of Avatar for SSO, etc.
- Added new AvatarDetail methods to Avatar API.
- Split out all but essential properties from Holon. HolonBase is now a lightweight version of Holon. Holon extends HolonBase.
- Added UmaJson field to AvatarDetail.
- Added PreviousVersionId & PreviousVersionProviderKey to HolonBase object for blockchain/ipfs providers to point to the previous version of a record when updates are being made.
- Added LastBeamedIn, LastBeamedOut and IsBeamedIn properties to Avatar and IAvatar.

----------------------------------------------------------------------------------------------------------------------------
## 0.17.1 ALPHA

- Fixed bug preventing people logging in for avatar/authenticate API method (Password was missing in mappings in MongoDBOASIS).

----------------------------------------------------------------------------------------------------------------------------
## 0.17.2 ALPHA

- Avatar and AvatarDetail now reutn id (workaorund for bug in Web API).
- Fixed bug in SQLLiteOASIS Provider (null checkes for collections).

----------------------------------------------------------------------------------------------------------------------------
## 0.18 ALPHA (09/10/21)

- All routes in all controllers returns OASISResult
- Added exception filter
- GetOLANDPrice method to cargo and nft controllers
- PurchaseOLAND route added to nft controller
- Added MoralisOASIS Provider
- Added routes for getting UmaJSON data
- Added route for getting avatar data with JWT token
- Added Release History link to this doc on main Swagger text at the top of the OASIS API.

----------------------------------------------------------------------------------------------------------------------------
## 0.19 ALPHA (25/10/21)

- ErrorHandling on all routes
- Solana Provider Integrated
- Added Admin controller for managing coupons and olands
- Fixed bug in getting Avatar by JWT token
- Cargo Provider Documented
- Solana Provider Documented
- Added GetCurrentLiveVersion Route
- Added GetCurrentStagingVersion Route
- IFPS Provider Integrated

----------------------------------------------------------------------------------------------------------------------------
## 0.19.1 ALPHA (04/11/21)

- Fixed Authorization Error
- Fixed Email Validation

----------------------------------------------------------------------------------------------------------------------------
## 2.0.0 PREVIEW (14/04/22)

LOTS of improvements in all areas including performance, security, features, improved error handling/reporting as well as being upgraded to the latest .NET (v6), which itself comes with many performance/security improvements & new features.
This is built on top of a whole new massively improved OASIS Architecture with improved error handling/reporting, auto-fail over, auto-replication, auto-load balancing and so much more! ;-) 
Everything the OASIS API was intended to do is now implemented... 
This is the real deal and is the commerical production ready OASIS, and why is no longer a ALTHA prototype as the previous versions were, and is also why the version number jumps from 0 to 2. 1 included many internal builds and why the date between 0 and 2 is large.

TODO: Will attempt to list here more detail of what is in this release (a very long list! ;-) ).

----------------------------------------------------------------------------------------------------------------------------
## 2.0.1 PREVIEW (17/04/22)

Fixed a bug with the Authentication/Signup process where it previously allowed more than one avatar to be created with the same email address.

This was not really a bug but by deseign because when someone deletes a avatar it is soft deleted so it can be recovered later if needed. Then
 they can create a new avatar with the same email address, but in the end the logic and additional work was deemed not worth it at this stage,
 we may re-visit this later on. For now if an avatar is deleted it is still soft deleted, then the user has 3 options:
 
 (1) - They can create a new avatar with a new email address.
 (2) - They can contact support to get the avatar un-deleted and active again.
 (3) - They can contact support to get the avatar permaneltey deleted and then they can create a new avatar with their old email address.

 As part of this hotfix, the messages that are returned were improved to now who deleted the avatar and when. The next release will have the 3 options above added also to the message returned.

 This hotfix is what officially made the new OASIS 2.0 LIVE and usable and is no coincidence it was on EASTER SUNDAY! ;-) We did not plan to release
 on this day, but is interesting it was on Easter just like when our founder started reading Ready Player One book on Easter Sunday when he had no idea
 what the book was about (gunters hunting the golden egg, etc).

 This release also included miscellaneous fixes/improvements:

 https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/0246c4f619111f425774edcc05e2dfaf459d5f06
  - Users can now login with their username, email or one of the public keys linked to their account.
  - Improved error handling/reporting in Auth/Signup.
  - Fixed a bug in AvatarManager where error messages were not properly formatted.
  - Improved error handling/reporting in JWTMiddleware for when a JWT Token expires/is invalid.
  - Changed Email to Username in AuthenticateRequest so need to use username instead of email for auth endpoint.
  - Added Username property to UpdateRequest so a avatars username can now also be updated/changed.
  - Refactored & improved Update, UpdateByEmail & UpdateByUsername methods in AvatarService so is now more efficient and generic calling into new private Update method.

----------------------------------------------------------------------------------------------------------------------------
## 2.0.2 PREVIEW (18/04/22)

Miscellaneous fixes/improvements including:

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/21f2f98650cb4262125dd83af039495e5de51af3
 - Can now upload & retreive an avatar image using their username & email as well as their id.
 - When an avatar image is retrived it now also shows the avatars username & email address in addition to their id.
 - Updated GetAvatarUmaJsonById, GetAvatarUmaJsonByUsername & GetAvatarUmaJsonByEmail methods so more detailed error messages are now returned.
 - When you now update an avatar it also syncs and updates AvatarDetail with the new username/email if they were changed in Avatar.
 - Updated Swagger index page to now include correct link to Postman JSON file as well as adding links to the DEV, STAGING & LIVE Enviroment JSON files.
 - Fixed a bug in MongoDBOASIS Provider where the email and username were not being mapped in ConvertMongoEntityToOASISAvatarDetail & ConvertOASISAvatarDetailToMongoEntity methods.

----------------------------------------------------------------------------------------------------------------------------
## 2.1 (05/05/22)

First official version of the production ready to use OASIS API! :)

Many improvements:<br>
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/42a43cc3c788cd6333bd937a997d443da3b195ed
- Added a new StackTraces property to the OASISResult wrapper.
- Detailed messages are now added to the new StackTraces property of OASISResult rather than being appended to the InnerMessages property in HandleWarning method in ErrorHandling helper,
- Fixed a bug in Authtenticate/AuthenticateAsync methods for logging in with a public key linked to an avatar in AvataManager.
- Also improved Authtenticate/AuthenticateAsync methods by making more generic and efficient using the new ProcessAvatarLogin method in AvataManager.
- Created a new ProcessAvatarLogin method in AvataManager.
- All methods in AvatarManager upgraded to now also pass in their detailedMessages into HandleError/HandleWarning methods.
- Fixed a bug in all methods in AvatarManager where the response from load/save methods on an OASIS Provider were not being handled properly.
- Added AutoMapper to OASIS.API.Core to be used in UpdateAvatarDetailAsync method.
- Added Automapper config to UpdateAvatarDetailAsync method.
- Upgraded AutoMapper to v11 in OASIS.API.ONODE.WebAPI project so matches the version added to OASIS.API.Core.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/cf1cf1b0ab282acabf7af64d2d50df530962c492
- Removed where clause restriction on OASISResultCollectionToCollection helper so can be used for mapping any objects and not just holons.
- Improved Security: Removed LoadAvatar/LoadAvatarAsync method overloads that take a username and password as params (only the overloads that take username are currently used because the password is decrypted in AvatarManager and checked there so is more secure) from IOASISStorageProvider interface.
- Related to above removed LoadAvatar/LoadAvatarAsync method overloads that take a username and password as params from AvatarManager.
- Releated to above removed LoadAvatar/LoadAvatarAsync method overloads that take a username and password as params from OASISStorageProviderBase.
- Releated to above removed LoadAvatar/LoadAvatarAsync method overloads that take a username and password as params from ActivityPubOASIS, BlockStackOASIS, ChainLinkOASIS, CosmosBlockChainOASIS, ElrondOASISOASIS, EOSIOOASIS, EthereumOASIS, HashgraphOASIS, HoloOASISBase, IPFSOASIS, MongoDBOASIS, Neo4jOASIS, PLANOASIS, ScuttlebuttOASIS, SolanaOASIS, SOLIDOASIS, TelosOASIS, ThreeFoldOASIS & TRONOASIS providers.
- UpdateAvatarDetail method in AvatarController now only ta
- Finished implementing UpdateAvatarDetailAsync methods in AvatarManager. It curretly only allows users to update the base properties, not the nested child objects (this functionality will be added soon...). It also syncs and updates the releated Avatar object if the username or email is changed because these are shared between the objects.
- Related to above finished implementing UpdateAvatarDetailByUsernameAsync & UpdateAvatarDetailByEmailAsync methods in AvatarManager.
- Upgraded DeleteAvatarAsync/DeleteAvatarByUsernameAsync/DeleteAvatarByUsername/DeleteAvatarByEmailAsync/DeleteAvatarByEmail methods in AvatarManager so that it now checks if the avatar has already been deleted and if it has it will inform the user that it has already been deleted and the date and by which avatar id.
- Implemented DeleteAvatar method in AvatarManager.
- Fixed bugs in LoadAvatarForProvider & LoadAvatarForProviderAsync methods in AvatarManager where the result was not being set correctly.
- Added UpdateAvatarDetailByEmail method to AvatarController.
- Added UpdateAvatarDetailByEmail & UpdateAvatarDetailByUsername methods to AvatarController.
- Updated the rest of the AvatarRepository methods to now return OASISResult as per the standard OASIS standards so better reporting/error handling info can be passed back to AvatarManager, etc. Now just need to do the same for HolonRepository and then MongoDBOASIS provider is finally finished and will be an example and best practice of how to implement a OASIS Provider so this is important work to finish before more devs get involved and build more...
- Also upgraded the Delete/DeleteAsync method overloads in AvatarRepository in MongoDBOASIS so they now use a transaction for deleting the avatar and then also delete the corresponding avatarDetail, if either fail it will roll back the transaction so they are done as a ATOMIC operation, so they will always match each other, one cannot exist without the other one. Also improved error handling, etc.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/52f37e959440d17e8bf25e8b4e74258db77f23ff
- Renamed Image2D to Portrait on AvatarDetail/IAvatarDetail.
- Changed Image2D to Portrait in UpdateAvatarDetailsAsync method in AvatarManager.
- Renamed GetAvatarImageById, GetAvatarImageByUsername, GetAvatarImageByEmail & Upload2DAvatarImage to GetAvatarPortraitById, GetAvatarPortraitByUsername, GetAvatarPortraitByEmail & UploadAvatarPortrait in AvatarService/IAvatarService.
- Renamed AvatarImage to AvatarPortrait.
-  LOTS done in AvatarController adding additional commenting/documentation to endpoints, add additional method overloads allowing the providerType to be set for each call, etc.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/f87be6cb820ae9c59fcafa723b2ddee0fd3336f8
- Added Import, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByEmail & ExportAll methods to IOASISStorageProvider interface to allow data to be imported and exported from the OASIS and between OASIS Providers.
- Added SearchAllProviders & SearchAvatarsOnly properties to ISearchParams interface & SearchParams.
- Fixed a bug in LoadAvatarForProviderAsync method in AvatarManager around the error handling mechanism.
- Started implementing SendHolon<T> method in HolonManager allowing a provider to send a holon (data packet) from one provider to another.
- Moved SearchAsync from OASISManager to SearchManager.
- Created new SearchManager to allow searching across any OASIS Provider or the full OASIS.
- LOTS done in AvatarController, added additional documentation, changed all REST endpoints to follow REST best practice naming conventions, fixed multiple bugs, renamed methods, added new methods, too many to list here (91 changes!)
- Renamed GetAvatarByJwt to GetLoggedInAvatar in IAvatarService/AvatarService.
- Re-factored Search methodin AvatarService.
- Bumped OASIS API version to "WEB 4 OASIS API v2.1". First version that is no longer a altha or preview! ;-)
- Updated Postman JSON file (lots of changes in here to reflect all the work done above, etc).
- Added init code for SearchRepository in ActivateProvider method in MongoDBOASIS Provider.
- Renamed Image2D to Portrait in AvatarDetailEntity in SQLLiteOASIS Provider.
- Renamed Image2D to Portrait in AvtarDetailRepository in SQLLiteOASIS Provider.
- Added connectionString to constructor for SQLLiteOASIS provider. Need to pass this through to DBContext ASAP...
- Temp renamed SQLLiteDBOASIS to SQLLiteOASIS in OASISBootLoader.

----------------------------------------------------------------------------------------------------------------------------
## 2.2 (06/06/22)

- Many bug fixes.
- Keys API.
- EthereumOASIS Provider.
- EOS Provider.
- All Avatar API calls are now wrapped in a HttpResponseMessage so follow HTTP/REST best practices.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/4c194d3c23792a9da2a8503e638baee0198866b8
- Continued work on the new Key/Wallet API... :) LOTS done today! :)
- Added new ProviderWallets dictionary to Avatar/IAvatar.
- Moved interfaces in OASIS.API.Core into relevant folders so better organised and easier to find.
- Added new IOASISWallet interface that contains a collection of IProviderWallets and a collection of IWalletTransactions where all transactions from all the providers will be aggregated into one fully unified OASIS Wallet.
- Added new IProviderWallet interface that contains the private key, public key, wallet address, transactions, providerType & balance.
- Improved the GenerateKeyPair method in KeyManager with improved error handling/reporting, etc.
- Fixed a bug in ClearCache method in KeyManager.
- Fixed bugs in KeyManager methods where detailed error messages were not being bubbled up to the calling function...
- Fixed bugs and improved LinkProviderPrivateKeyToAvatar method in KeyManager such as improved error handling/reporting etc.
- Fixed multiple miscalensous bugs in KeyManager.
- Replaced StringCipher encryption for private keys with Rijndael Aes256 encyption in KeyManager making it even more secure. Soon will also add Quantum level encryption improving security even more... :)
- Removed GetProviderPrivateKeyForAvatarByEmail method in KeyManager so the avatar's email and private key are never sent down the wire at the same time. Again this improves security and reduced risk.
- Improved GetPublicWif, DecodePrivateWif, Base58CheckDecode & EncodeSignature methods in KeyManager so they have improved error handling/reporting as well as now returning OASISResult so follow OASIS coding standards.
- Added Rijndael256Key to OASISDNA in both STAR ODK & OASIS API Web API.
- Removed redundant methods from Avatar Controller.
- Updated LinkEOSIOAccountToAvatar in EOSIOController to work with latest KeyManager improvements.
- Updated GetHolochainAgentIdsForAvatar & GetHolochainAgentPrivateKeyForAvatar methods in HolochainController to work with latest KeyManager improvements.
- Removed GetProviderPrivateKeyForAvatarByEmail,  GetAllProviderPrivateKeysForAvatarByEmail & GetAvatarEmailForProviderPrivateKey methods from KeysController to improve security.
- Added GetPrivateWif, GetPublicWif, DecodePrivateWif, Base58CheckDecode & EncodeSignature methods to KeysController.
- Fixed bugs in ValidateParams method in KeysController.
- Updated LinkTelosAccountToAvatar method in TelosController to work with latest KeyManager improvements.
- Added WifiParams model to OASIS API Web API Models\Keys folder.
- Moved models into new folders to better organise in Web API.
- Updated GetEOSIOAccountNamesForAvatar & GetEOSIOAccountPrivateKeyForAvatar methods in EOSIOOASIS Provider to work with latest KeyManager improvements.
- Fixed a bug in EthereumOASIS Provider where hostUri, chainPrivateKey & chainId were not null checked.
- Added ProviderWallets to AvatarEntity in SQLLiteDBOASIS Provider.
- Updated GetTelosAccountNamesForAvatar & GetTelosAccountPrivateKeyForAvatar methods in TelosOASIS Provider to work with latest KeyManager improvements.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/97ae50504b852cd4f477c77e56d3009a97d5a07c
- Continued bug fixes for the new Keys/Wallets API.
- Improved message feedback for ClearCache in KeyManager.
- Added new showPublicKey & showPrivateKey params to GenerateKeyPairAndLinkProviderKeysToAvatarById, GenerateKeyPairAndLinkProviderKeysToAvatarByUsername, GenerateKeyPairAndLinkProviderKeysToAvatarByEmail & GenerateKeyPairAndLinkProviderKeysToAvatar methods in KeyManager so the caller can decide which of the generated keys (if any) they wish the method to return once it has linked them.
- Improved error handling/reporting for GetProviderUniqueStorageKeyForAvatarById method in KeyManager.
- Fixed bugs in GetProviderUniqueStorageKeyForAvatarByUsername, GetProviderUniqueStorageKeyForAvatarByEmail, GetProviderPublicKeysForAvatarById, GetProviderPublicKeysForAvatarByUsername, GetProviderPublicKeysForAvatarByEmail, GetProviderPrivateKeyForAvatarById, GetProviderPrivateKeyForAvatarByUsername, GetAvatarIdForProviderUniqueStorageKey, GetAvatarUsernameForProviderUniqueStorageKey, GetAvatarEmailForProviderUniqueStorageKey, GetAvatarForProviderUniqueStorageKey, GetAvatarIdForProviderPublicKey, GetAvatarUsernameForProviderPublicKey, GetAvatarEmailForProviderPublicKey, GetAvatarForProviderPublicKey, GetAvatarIdForProviderPrivateKey, GetAvatarUsernameForProviderPrivateKey, GetAvatarEmailForProviderPrivateKey, GetAvatarForProviderPrivateKey & GetProviderUniqueStorageKeyForAvatar methods in KeyManager where the result was not being handled or return properly.
- Replaced StringCipher encryption with Rijndael AES 256 encryption in GetAllProviderPrivateKeysForAvatarById, GetAllProviderPrivateKeysForAvatarByUsername & GetAllProviderPrivateKeysForAvatarByEmail methods in KeyManager.
- Added new ShowPublicKey & ShowPrivate key params to GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId, GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarUsername & GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarEmail methods in KeysController.
- Fixed a bug in GetProviderUniqueStorageKeyForAvatarById &  GetProviderPrivateKeyForAvatarById methods in KeysController where providerType was not being passed in.
- Finished fully implementing GetProviderUniqueStorageKeyForAvatarByUsername,GetProviderUniqueStorageKeyForAvatarByEmail, GetProviderPrivateKeyForAvatarByUsername, GetProviderPublicKeysForAvatarByUsername &  GetProviderPublicKeysForAvatarByEmail methods in KeysController.
- Added new ShowPublicKey & ShowPrivateKey params in LinkProviderKeyToAvatarParams model.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/9c2add46f50af326feb35c1c17cf68361f9e0b22
- LOTS done today on the new Key/Wallet API's! :)
- Added new generic UnWrapOASISResult helper method to automatically unwrap OASISResult' and handle any errors, etc.
- Added new UnWrapOASISResultWithDefaultErrorMessage method to call UnWrapOASISResult method with a default error message.
- Replaced ProviderPrivateKey, ProviderPublicKey & ProviderWalletAddress properties on Avatar holon/IAvatar interface with new ProviderWallets property.
- IProviderWallet interface now inherits from IHolonBase and also added SecretRecoveryPhrase property.
- Updated Authenticate/AuthenticateAsync methods in AvatarManager to work with new ProviderWallets collection rather than ProviderPublicKey (used for allowing people to login in with their public key).
- Upgraed KeyManager to work with ProviderWallets.
- Upgraded LinkProviderPublicKeyToAvatarById, LinkProviderPublicKeyToAvatarByUsername, LinkProviderPublicKeyToAvatarByEmail, LinkProviderPublicKeyToAvatar, GenerateKeyPairAndLinkProviderKeysToAvatar, LinkProviderPrivateKeyToAvatarById, LinkProviderPrivateKeyToAvatarByUsername, LinkProviderPrivateKeyToAvatarByEmail, LinkProviderPrivateKeyToAvatar methods in KeyManager to work with ProviderWallets so they return a Guid rather than a bool and also have an additional walletId param,
- GetProviderPrivateKeysForAvatarById & GetProviderPrivateKeysForAvatarByUsername methods now return a collection of keys rather than just one key.
- Ugraded GetAvatarForProviderPublicKey, GetAllProviderPublicKeysForAvatarById, GetAllProviderPublicKeysForAvatarByUsername, GetAllProviderPublicKeysForAvatarByEmail, GetAllProviderPrivateKeysForAvatarById & GetAllProviderPrivateKeysForAvatarByUsername methods to work with new ProviderWallets.
- Added new WalletManager with 2 GetWalletThatPublicKeyBelongsTo method overloads, lots more to come! ;-)
- Added new OASISWallet, ProviderWallet & WalletTransaction objects.
- Upgraded EOSIOController, TelosController & HolochainController to work with latest KeyManager changes.
- Added new WalletId property to LinkProviderKeyToAvatarParams model.
- Upgraded EOSIOOASIS Provider to work with the latest KeyManager changes.
- Replaced ProviderPrivateKey, ProviderPublicKey & ProviderWalletAddress properties on Avatar entity, ConvertMongoEntityToOASISAvatar & ConvertOASISAvatarToMongoEntity methods with new ProviderWallets property.
- Fixed a bug in ConvertMongoEntitysToOASISAvatars, ConvertMongoEntitysToOASISAvatarDetails, ConvertMongoEntityToOASISAvatarDetail & ConvertMongoEntityToOASISHolon methods in MongoDBOASIS Provider & improved error handling.
- Upgraded TelosOASIS Provider to work with the latest KeyManager changes.

- See Commit History for more details...

----------------------------------------------------------------------------------------------------------------------------
## 2.2.1 (13/06/22)

- Many bug fixes.
- All API responses now also include a new OASISVersion property so you always know the API version you are using. It is a version number followed by the current enviroment, e.g. v2.2.1 LIVE.
- Added new LocalFileOASIS Provider to work with the new Key/Wallets API to store private keys encrypted locally only.
- Improved OASIS Arcitecture generally.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/b2172f455b3869e2dc70b854b8aa68cde83bec04 <br>
Fixed a bug in GetAllProviderPrivateKeysForAvatarById method in KeyManager where the keys were not null checked.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/8c93ea5398160aa4a273b2da1f10865c5581552f
- Improved OASIS Error Handling/Reporting.
- Added new HandleError and HandleWarning overloads to ErrorHandling helper that take an optional innerResult param so its InnerMessages, WarningCount etc can be added to the main OASISResult.
- HolonBase constructors no longer automatically create a new id (it needs to be created manually by the caller).
- Added WalletId property to IProviderWallet because the base Id property on HolonBase was not being serialaized.
- Fixed a bug in VerifyEmail method in AvataManager as a side effect from the latest work done in AvatarManager (new optional loadPrivateKeys param being added).
- Removed all private key caches to make it even more secure so private keys are never stored in memory and are only loaded from local storage and decrypted and sent to client per request to remove attack surface area.
- LinkProviderPublicKeyToAvatarById, LinkProviderPublicKeyToAvatarByUsername & LinkProviderPublicKeyToAvatarByEmail methods in KeyManager now load private keys when loading the avatar so when it saves the wallet back to local storage the private keys are not blanked out.
- LinkProviderPublicKeyToAvatar method in KeyManager now sets WalletId, AvatarId, CreatedByAvatarId & CreatedDate properties on new wallets and sets ModifiedByAvatarId and ModifiedDate on existing wallets.
- Fixed a bug in LinkProviderPublicKeyToAvatar method in KeyManager where the avatar was being saved before the local storage wallets (so the private keys were blanked out before they could be saved). It now saves to local storage first.
- LinkProviderPrivateKeyToAvatar method in KeyManager has multiple bugs fixed including checking if a key has already been linked, saving the local storage wallets before saving the avatar, improved error handling, setting WalletId, AvatarId, CreatedByAvatarId & CreatedDate for new wallets and setting ModifiedByAvatarId & ModifiedDate for exisitng wallets.
- Completley re-wrote GetProviderPrivateKeysForAvatarById & GetProviderPrivateKeysForAvatarByUsername methods in KeyManager to work with the new local storage wallets.
- Removed GetAvatarIdForProviderPrivateKey, GetAvatarUsernameForProviderPrivateKey, GetAvatarEmailForProviderPrivateKey & GetAvatarForProviderPrivateKey methods in KeyManager since would be extra work, potential security issue and not sure if there would be a good use case for them?
- Fixed a bug in GetAllProviderPrivateKeysForAvatarById & GetAllProviderPrivateKeysForAvatarByUsername methods in KeyManager.
- Fixed bugs in LoadProviderWalletsForAvatarByUsernameAsync, LoadProviderWalletsForAvatarByUsername, LoadProviderWalletsForAvatarByEmailAsync, LoadProviderWalletsForAvatarByEmail, LoadProviderWalletsForAvatarByUsername, LoadProviderWalletsForAvatarByUsernameAsync,  LoadProviderWalletsForAvatarByEmail, LoadProviderWalletsForAvatarByEmailAsync, SaveProviderWalletsForAvatarByUsername, SaveProviderWalletsForAvatarByUsernameAsync, SaveProviderWalletsForAvatarByEmail, SaveProviderWalletsForAvatarByEmailAsync, SaveProviderWalletsForAvatarByUsername, SaveProviderWalletsForAvatarByUsernameAsync, SaveProviderWalletsForAvatarByEmail, SaveProviderWalletsForAvatarByEmailAsync & GetWalletThatPublicKeyBelongsTo methods where the detailed error message was not being bubbled up correctly.
- Rewrote & improved LoadProviderWalletsForAvatarById, LoadProviderWalletsForAvatarByIdAsync, SaveProviderWalletsForAvatarById & SaveProviderWalletsForAvatarByIdAsync methods in WalletManager.
- Added WalletId to ProviderWallet object.
- Removed GetAvatarIdForProviderPrivateKey, GetAvatarUsernameForProviderPrivateKey &  GetAvatarForProviderPrivateKey methods from KeysController.
- Fixed a bug in Update, UpdateByEmail & UpdateByUsername methods in AvatarService from side-effect of improvements to AvatarManager (new loadPrivateKeys param).
- Fixed a bug in  LoadProviderWalletsForAvatarById & LoadProviderWalletsForAvatarByIdAsync methods in LocalFileOASIS Provider where it was not checking if the wallet file exists.
- Fixed a bug in  SaveProviderWalletsForAvatarById & SaveProviderWalletsForAvatarByIdAsync methods where it was not serializing the wallets correctly (missing WalletId).
 master

 https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/e4a271115c61fafc9a83d819da84e9edcfbdc7e2
- LOTS of work done over past few days on new key/wallet API's as well as improvements to the OASIS Architecture.
- Added new None, StorageLocal, StorageLocalAndNetwork enum types to ProviderCategory (to be used in the new key/wallet api for storing private keys to local storage only.
- Added new LocalFileOASIS enum type to ProviderType and moved None, All & Default to the top.
- Save & SaveAsync methods on Avatar/IAvatar now take a optional providerType param.
- Added SendTrasaction & SendTrasactionAsync methods to IOASISBlockchainStorageProvider interface that blockchain OASIS Providers need to implmenet.
- Added new IOASISLocalStorageProvider interface that contains LoadProviderWallets, LoadProviderWalletsAsync, SaveProviderWallets & SaveProviderWalletsAsync methods that a Local Storage OASIS Provider need to implement such as HoloOASIS, SQLLiteDBOASIS & LocalFileOASIS. There will be more to come...
- Added SendNFT & SendNFTAsync methods to IOASISNFTProvider interface.
- Added Import, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByEmail & ExportAll methods to IOASISStorageProvider interface.
- Added SendTransaction, SendTransactionAsync, SendNFT & SendNFTAsync methods to IOASISWallet & IProviderWallet interfaces & OASISWallet & ProviderWallet objects.
- Added new Instance properties to all OASISManagers such as AvatarManager, HolonManager, KeyManager, WalletManager & SearchManager allowing static instances to be used instead of having to create new instances (singleton pattern best practice).
- Moved OASISDNA & OASISDNAPath from OASISStorageProviderBase to OASISProvider abstract class because these are relevant to all providers, not just storage providers.
- Added Import, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByEmail & ExportAll methods to OASISStorageProviderBase.
- Added new LocalFileOASIS setting to OASISDNA.
- Added new Import, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByEmail & ExportAll methods to all OASIS Providers including ActivityPubOASIS, BlockstackOASIS, ChainLinkOASIS, EOSIOOASIS, EthereumOASIS, HashgraphOASIS, HoloOASIS, IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, PLANOASIS, ScuttlebuttOASIS, SolanaOASIS, SOLIDOASIS, SQLLiteDBOASIS, TelosOASIS, ThreeFoldOASIS& TRONOASIS.
- Added new LocalFileOASIS Provider which will serialize the avatar & holon data objects to a JSON file. Currently is serializes the Provider Wallets containing the private keys (encrypted) but will add additional encryption soon including quantum level encryption.
- Removed ProviderPrivateKey, ProviderPublicKey, ProviderUsername & ProviderWalletAddress properties from the AvatarDetail entity in MongoDBOASIS Provider.
- Added new LocalFileOASIS Provider to OASISBootLoader and fixed some other minor bugs.


 https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/408026b7be87d2b00d37abb57d9db07ab0113183
- Fixed a bug in LoadAvatarForProviderAsync & LoadAvatarByEmailForProviderAsync methods where it now checks if the provider is a StorageLocalAndNetwork category as well as a StorageLocal category before loading the provider wallets.
- Added LocalFileOASIS provider to AutoReplicationProviders, AutoFailOverProviders & AutoLoadBalanceProviders lists in OASISDNA.json in both OASIS API REST Web API and STAR ODK.

- See Commit History for more details...

----------------------------------------------------------------------------------------------------------------------------
## 2.2.2 (21/06/22)

- Many bug fixes.
- Improved new Keys/Wallets API.
- Improved OASIS Arcitecture generally.
- Many other enhancements to the OASIS Engine/OASIS API REST Web API such as the ability to now set whether to autoReplicate, autoFailOver, autoReplicate, providers to auto replication to, providers to auto load balance to and providers to auto fail over to per API call per avatar. This means each API call per avatar can be fully customised the avatar/user is in FULL control of their data, where it is stored and how and when and allows full control over the behaviour of how to handle auto-failover, load balancing and auto replication. This makes the OASIS API many more times powerful than it already was! :)
It also now also shows the current status of the OASIS Engine in every response you get from every API call (such as whether auto-failvover, auto-loadbalance and auto-replication are enabled or not, the current OASIS version, standard HTTP best practice info and more and even allows additional detailed stats to be enabled/disabled such as which providers are auto-replicating, are in auto-fail over and auto-load balance lists.
So as you can see this was a massive upgrade to the OASIS Engine/Architecture as well as the OASIS API.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/77c10ce87221f45a21fad6cdae242953e942ac31
- Added new NotSet enum to AutoFailOverMode, AutoBalanceMode & AutoReplicationMode.
- Added new ConvertToEnumValueList & ConvertFromEnumValueList methods in EnumHelper.
- Improved and re-wrote BuilderInnerMessagesError method in OASISResultHelper.
- Added new autoBalanceMode to Save & SaveAsync methods in Avatar holon & IAvatar interface.
- Fixed a bug in Save & SaveAsync methods in AvatarDetail where it was calling Save methods directly on the current provider rather than going through AvatarManager.
- Fixed a bug in AutoReplication code for Avatar where the provider list was not being passed to the background thread so if and when the provider list was changed (with the new OASIS Engine improvements above) mid-way through a API call, it was causing issues with the provider lists changing.
- Fixed a bug in LoadAllAvatarDetailsAsync method in AvatarManager where the wrong success message was returned.
- Re-wrote the send email code in AvatarManager.
- Fixed a bug in UpdateAvatarDetailAsync method in AvatarManager where LoadAvatarAsync was being called with wrong params.
- Added autoLoadBalanceMode param to SaveAvatarAsync, SaveAvatar, SaveAvatarDetail & SaveAvatarDetailAsync methods in AvatarManager.
- Added new autoReplicationMode param to AuthenticateAsync method and fixed other bugs.
- Moved ForgotPassword method from AvatarService to AvatarManager.
- _providerAutoFailOverList, _providerAutoLoadBalanceList & _providersThatAreAutoReplicating lists are no longer read-only.
- Added new SetAutoReplicationForProviders, SetAndReplaceAutoReplicationListForProviders, SetAutoFailOverForProviders, SetAndReplaceAutoFailOverListForProviders, GetProvidersFromList, GetProvidersFromListAsEnumList, SetAutoLoadBalanceForProviders, SetAndReplaceAutoLoadBalanceListForProviders & GetProviderListAsString methods to ProviderManager.
- Added new OASISVersion property to OASISDNA.
- Added new OASISWebSiteURL property to EmailSettings section in OASISDNA.
- All methods in AvatarController now return the OASISHttpResponseMessage wrapper and use the new HttpResponseHelper.FormatResponse method.
- Re-wrote and massivey upgraded the Authenticate methods in AvatarController to work with the latest OASIS Engine upgrades listed at the top. This is the 1st prototype method to test out the latest enhancements and will be rolled out to every other method soon... :)
- Moved and upgraded FormatResponse method to new HttpResponseHelper.
- Added new OASISHttpResponseMessage that extends the built in HttpResponseMessage so all OASIS API calls now conform to the standard best practice HTTP response. OASISHttpResponseMessage  also contains many additional properties such OASISVersion, AutoLoadBalanceEnabled, AutoFailOverEnabled, AutoReplicationEnabled, AutoLoadBalanceProviders, AutoFailOverProviders, AutoReplicationProviders & CurrentOASISProvider.  ALL OASIS API calls now return this info.
- AuthenicateRequest now extends the new generic OASISRequest object.
- Added new OASISRequest object that allows the OASIS Engine to be fully customised per API call per Avatar and allows the following properties/settings to be changed: ProviderType, SetGlobally, AutoFailOverEnabled, AutoReplicationEnabled, AutoLoadBalanceEnabled, AutoFailOverProviders, AutoReplicationProviders, AutoLoadBalanceProviders, WaitForAutoReplicationResult & ShowDetailedSettings.
- Fixed a bug in ResetPassord method in AvatarService.
- Fixed a bug in GetRefreshTokenmethod in AvatarService.
- Moved RandomTokenString method to AvatarManager from AvatarService.
- Bumped OASIS API Version in Startup to WEB 4 OASIS API v2.2.2.
- Added autoLoadBalanceModel to Save & SaveAsync methods AvatarEntity in SQLDBLiteOASIS Provider.
- Added OASISVersion and bumped CurrentStagingVersion & CurrentLiveVersion properties to v2.2.2 in OASIS_DNA.json in STAR.
- Remove AutoMapper from STAR.
- Updated CreateAvatar & CreateAvatarAsync methods in STAR to work with latest changes in Regiser methods in AvatarManager.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/e99399158255e51659814d7b23116f0cb05d6d23
- Added OASISVersion to OASIS_DNA.json in WebAPI and bumpted CurrentStagingVersion and CurrentLiveVersion to v2.2.2.
- Fixed bugs in OASIS.API.Core Test Harness,
- Set OASISSQLLiteDB.db to copy always in WebAPI.
- Set AvatarManager, KeyManager & WalletManager to all use EOSIOOASIS Provider in EOSIOOASIS Provider.
- Set KeyManager & WalletManager to all use EthereumOASIS Provider in EthereumOASIS Provider.
- Fixed a bug in DataContext in SQLLIteDBOASIS where connectionstring was not being passed in again!).
- Switched EOSIOOASIS-OLD to EOSIOOASIS in OASISBootLoader.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/d67da8b946078bb9476dc8dbcd646125e70a9b16
- Just about finished the new Key/Wallet API for this phase, lots more to do in the next phase with STAR (such as integrating the client side components of the wallet api into the STAR CLI etc) and after STAR...
- Renamed LoadProviderWallets & LoadProviderWalletsAsync methods to LoadProviderWalletsForAvatarById & LoadProviderWalletsForAvatarByIdAsync methods in IOASISLocalStorageProvider interface  and now takes the avatar id as a param so more than 1 avatar can be used on any given device.
- Renamed SaveProviderWallets & SaveProviderWalletsAsync methods to SaveProviderWalletsForAvatarById& SaveProviderWalletsForAvatarByIdAsync methods in IOASISLocalStorageProvider interface and now takes the avatar id as a param so more than 1 avatar can be used on any given device.
- All LoadAvatar methods in AvatarManager now take an optional loadPrivateKeys param.
- Rewrote and improved LoadProviderWalletsAsync & LoadProviderWallets methods in AvatarManager and now calls into the new WalletManager methods.
- Added new LoadProviderWalletsForAllAvatars & LoadProviderWalletsForAllAvatarsAsync methods in AvatarManager.
- KeyManager no longer takes a AvatarManager instance as a param in the constructor (DI), it uses the static instance instead.
- Upgraded KeyManager to work with the new WalletManager and latest changes in AvatarManager.
- Private keys are now encrypted in LinkProviderPrivateKeyToAvatar method in KeyManager.
- LinkProviderPrivateKeyToAvatar method now calls the new SaveProviderWalletsForAvatarById method in WalletManager.
- Added new GetProviderCategory method in ProviderManager.
- Fixed a bug in SetAndActivateCurrentStorageProvider method in ProviderManager where it was not also checking if a provider was LocalStorage or LocalStorageAndNetwork.
- Added new LoadProviderWalletsForAvatarByIdAsync, LoadProviderWalletsForAvatarById, LoadProviderWalletsForAvatarByUsernameAsync, LoadProviderWalletsForAvatarByUsername, LoadProviderWalletsForAvatarByEmailAsync, LoadProviderWalletsForAvatarByEmail, SaveProviderWalletsForAvatarByIdAsync, SaveProviderWalletsForAvatarByUsername, SaveProviderWalletsForAvatarByEmail & SaveProviderWalletsForAvatarByEmailAsync method overloads (2 versions of each method above, one takes a providerType, the other uses the AutoFailOverList to find local storage providers).
- Also added CopyProviderWallets method to WalletManager.
- Updated AvatarControll,er EOSIOController, HolochainController, KeysController & TelosConttroller to work with the latest upgrades to KeyManager/WalletManager.
- Updated EOSIOOASIS, HoloOASIS, LocalFileOASIS, SEEDSOASIS, SQLLiteDBOASIS & TelosOASIS providers to work with the latest upgrades to KeyManager/WalletManager.

- See Commit History for more details...

----------------------------------------------------------------------------------------------------------------------------
## 2.3.0 (22/07/22)

- Improvements & upgrades to the Data API.
- Finished upgrading the OASISEngine so now EVERY API call can fully customise the OASIS Engine for that call such as whether auto-load balance, auto-fail over, auto-replication is enabled, which providers to use for auto-replication, auto-load balance & auto-failover, whether to show the auto-replication, auto-load balance & auto-failover lists in the response returned & whether to wait for auto-replication result. This allows for greating power and flexability so the OASIS Engine behaviour can be customised by api call per avatar.
- Improved OASIS Arcitecture generally.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/b54b342907a2ce64c3691ccd1f28abc9088a54ca
- Fixed a bug in HoloNET where Config.FullPathToHolochainAppDNA was not null checked.
- Fixed another bug in HoloNET in CallZomeFunctionAsync method where it did not check if the WebSocket was connected, if it is not it attempts to connect first.
- Updated message sent in SendAlreadyRegisteredEmail method in AvatarManager so a custom dynamic message can be passed in.
- Updated PrepareToRegisterAvatar method in AvatarManager to work with updated CheckIfEmailIsAlreadyInUse method.
- Re-wrote and improved CheckIfEmailIsAlreadyInUse method in AvatarManager. It now emails the avatar using custom dynamic messages.
- Fixed bugs in OASISProvider constructors where it only loads the OASIS DNA if it has not already been loaded/set.
- Updated MapManager, MissionManager, OLANDManager, ParkManager, QuestManager, SampleManager, SearchManager & SeedsManager constructors to now have OASISDNA & OASISStorageProvider passed in as params passing them through to the base OASISManager.
- Added  OASISStorageProvider & OASISDNA params to the constructors in OASISManager.
- Made Web3Client public in EthereumOASIS Provider so STAR can call it directly.
- Removed IPFSEngine from IPFSOASIS Provider.
- Added additional constructors to the MongoDBOASIS Provider allowing custom OASISDNA to be passed in. Needs applying to the rest of the Providers ASAP.
- Added WalletManager to OASISAPIManager in STAR ODK.
- Changed LoggedInAvatar & LoggedinAvatarDetail from Avatar & AvatarDetail to IAvatar & IAvatarDetail interfaces so follows best practices.
- Updated STAR CLI Test Harness to work with latest changes/improvements in OASIS API.
- Adding additional error handling & mesasges to STAR Test Harness.
- Re-wrote the IPFS Tests in STAR Test Harness.
- All Provider tests (EOSIO/Ethereum/IPFS/Neo4j/Seeds/MongoDB) in STAR Test Harness now all check if the provider is activated and if it is not then it will activate it.
- Fixed bugs in ShowHolons method in STAR Test Harness.
- Updated GetValidEmail method in STAR TestHarness to work with the new and improved CheckIfEmailIsAlreadyInUse method in AvatarManager.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/a59d219ff58c9740d119a136b67e8f7f7a521210
- LOTS done past few days, fixed some bugs in STAR ODK & lots done on the OASIS API Data API, which about to make a new demo video for...
- Added new Convert methods to Mapper helper for converting between lists of Holons and IHolons.
- Fixed a bug in BuildInnerMessageError method in OASISResultHelper.
- Added a new CopyResult method in OASISResultHelper.
- Fixed bugs in HolonManager where the result was not null checked before setting a copy for the change tracking code.
- Updated Authenticate method in AvatarController to use the new ConfigureOASISSettings and ResetOASISSettings methods in OASISControllerBase.
- Lots done in DataController to complete the Data API.
- All methods in DataController now return OASISHttpResponseMessage.
- LoadHolon methods use the new ConfigureOASISSettings and ResetOASISSettings methods in OASISControllerBase.
- Added multiple versions of the LoadHolon, LoadAllHolons, LoadHolonsForParent, SaveHolon & DeleteHolon methods that take both a form/body and querystring including simple and advanced verions allow all sorts of settings to be configured such as loadChildren, recursive, maxChildDepth, continueOnError, version, providerOverride, setGlobally, AutoFailOverEnabled, AutoReplicationEnabled, AutoLoadBalanceEnabled, AutoReplicationProviderList, AutoFailOverProviderList, AutoLoadBalanceProviderList, WaitForAutoReplicationResult & ShowDetailedSettings.
- Added new ConfigureOASISSettings & ResetOASISSettings methods to OASISControllerBase allowing the OASIS Engine to be customised per API call i n the OASIS API.
- Updated FormatResponse method in HttpResponseHelper.
- Removed AutoFailOverMode, AutoReplicationMode & AutoLoadBalanceMode from OASISHttpResponseMessage.
- Added new BaseLoadHolonRequest, LoadAllHolonsRequest, LoadHolonRequest, LoadHolonsForParentRequest & SaveHolonRequest to Models.Data.
- Added OASISConfigResult to Models in WebAPI project.
- ShowDetailedSettings now defaults to true on OASISRequest (for all API calls to OASIS API).
- Renamed OlandManager to OLANDManager.
- Changed Nodes collection from ObservableCollection to IEnumerable in Holon entity on MongoDBOASIS Provider.
- Fixed a bug in GetAllHolonsAsync method in HolonRepository in HolonManager.
- Fixed a bug in SoftDelete/SoftDeleteAsync methods where holon was not being null checked.
- Fixefd a bug in ConvertMongoEntityToOASISAvatarDetail, ConvertOASISAvatarDetailToMongoEntity, ConvertMongoEntityToOASISHolon & ConvertOASISHolonToMongoEntity methods in MongoDBOASIS provider.
- Added a new zome to the test CelestialBodyDNA (about to update templating engine in STAR ODK to suupport multiple zomes).
- Changed cSharpGeneisFolder & rustGenesisFolder paths to point at Debug instead of Release in STAR Test Harness.

<a href="https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/1cd844395fec2311e108e15046f00b2cfd94f021">https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/1cd844395fec2311e108e15046f00b2cfd94f021</a>
- Finished upgrding the new evolved Data API (almost ready to deploy and demo). :)
- Added a new ResultsCount to the OASISResult helper.
- Tidied up code in AvatarController.
- Lots done in DataController upgrading it to use the new ConfigureOASISEngine method in OASISControllerBase where many settings such as AutoReplication, AutoLoadBalance, AutoFailOver, Default Providers, etc can be configured per API call per avatar.
- Added detailed documentation to Data API in DataController.
- Upgraded LoadHolon, LoadAllHolons, LoadHolonsForParent, SaveHolon & DeleteHolon methods to work with latest improvements to the OASIS Engine and intrgrated to new ConfigureOASISEngine method.
- Upgraded and renamed ConfigureOASISSettings to ConfigureOASISEngine method in OASISControllerBase.
- Upgraded FormatResponse method in HttpResponseHelper and added new overload that takes a OASISHttpResponseMessage.
- Made _showSettings a public property and renamed to ShowDetailedSettings.
- Added new BaseHolonRequest to Models in Web API.
- BaseLoadHolonRequest now inherits from BaseHolonRequest.
- Added new DeleteHolonRequest to Models in Web API.
- SaveHolonRequest now inherits from BaseHolonRequest.
- Renamed AutoFailOverEnabled to AutoFailOverMode, AutoReplicationEnabled to AutoReplicationMode & AutoLoadBalanceEnabled to AutoLoadBalanceMode in OASISRequest.
- Bumped OASIS version to v2.3.0 in Startup and OASISDNA in WebAPI & STAR ODK.

- See Commit History for more details...

----------------------------------------------------------------------------------------------------------------------------
## 2.3.1 (25/07/22)

- Corrected previous version (from v2.2.3 to v2.3.0) in this release history (above).
- Updated ref in Swagger documentation to point to this new md file rather than the older txt file.
- Corrected typeo in Data API swagger documentation.

----------------------------------------------------------------------------------------------------------------------------