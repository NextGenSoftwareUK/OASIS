
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
## 3.0.0 (09/09/23)

A brief summary of the massive amount of work that has gone into this release is detailed below, this does not include everything, just a few highlights, please see the full commit history further down for the full list.

- Added 2 new methods to the Avatar API: get-all-avatar-names and get-all-avatar-names-grouped-by-name that both have 2 params allowing you to specify whether to also include usernames and id's (if both are false it will only return their full name). Full name is made up of FirstName and LastName fields and both are NOT unique because people share the same first and last names, the username and email fields ARE unique however.
- Related to above, new accounts/avatars now need to specify a unique username when creating a avatar. Previous versions only needed an email, which was then used as the default username (which they could then change later). The issue with this was that the new Avatar API methods above return their username so would expose user's private email addresses! So this has now been fixed. Old accounts will have the username changed to their fullname (if there are more than one sharing this name then it will add a 1,2,3 etc to the end to make it unique), they can then login and change it to whatever they like (but it needs to be unique).
- Updated the email templates used in the Avatar API, which are in turn used for methods such as register, authenticate, verify-email etc.
- Implemented new LevelManager which dynamically calculates the avatar's level based on their karma score (previously it was hard-coded and only supported upto level 10! It now supports infinite levels).
- As part of the above, Karma is now a long rather than an int supporting much higer avatar levels. The maximum karma score is now 9,223,372,036,854,775,807, we don't even think Budaha himself could have achived this! ;-)
- Avatar has new FullNameWithTitle property.
- Renamed Password to NewPassword and ConfirmPassword to ConfirmNewPassword for reset-password Avatar API method.
- Fixed a bug in reset-password Avatar API method where the message response was being set in the Result property instead of the Message one.
- Various fixes/improvements to the Avatar API.
- Added new AzureCosmosDBOASIS OASIS Provider adding support for the Azure Cosmos DB Cloud service from Microsoft, allowing people to import/export their cloud data to the OASIS as well as store their OASIS Avatar & data to the cloud if they wish (could be used as a backup or redundancy in case their local copies get lost or corrupted etc).
- Neo4j OASIS Provider has been re-written from scratch with many improvements/fixes etc.
- HoloOSIS Provider has also been re-written from scatch with many improvements/fixes to work with the latest HoloNET (world's first .NET/Unity client for Holochain). This is a pre-release and so is still a WIP so it is not quite ready to be used yet, but will be in a future version soon...
- New NFT API - This allows mass interoperability between NFT's across all chains and even web2 as well as IPFS, Holochain and more! It introduces a new global generic NFT Standard (The OASISNFT) as well as the OASISGeoSpatialNFT standard. It also includes the new cross-chain OASIS Geo-NFT API and many other unique world first's that are being implemented in our AR Geo-location game Our World.
- New OLAND NFT API - Related to above, it also includes a new OLAND NFT API allowing people to purchase virtual land in Our World and The OASIS. This is built on top of the new NFT API.
- As part of above, added new INFTWalletTransaction interface.
- Added a new MemoText property to IWalletTransaction interface.
- All SendTraction and SendTractionAsync methods in IOASISBlockchainStorageProvider interface in OASIS.Core now return OASISResult<TransactionRespone> instead of OASISResult<bool>.
- Added HostURI, EOSAccountName, EOSChainId & EOSAccountPk properties to EOSIOOASIS Provider.
- Re-wrote and improved ActivateProviderAsync, ActivateProvider, DeActivateProviderAsync & DeActivateProvider methods in EOSIOOASIS Provider.
- Implemented new async versions for ActivateProvider and DeActivateProvider so these sometimes time intensive operations can now be done asyncally so can be done in the background and not block/slow down the OASIS Arcitecture (follows best practices).
- Various other older core OASIS Architecture methods have also been upgraded to async versions giving yet further performance and stability improvements.
- Added ActivateProviderAsync & DeActivateProviderAsync methods to BlockStackOASIS, ChainlinkOASIS, CosmosBlockchainOASIS, AzureCosmosDBOASIS, ElrondOASIS, EOSIOOASIS, EthereumOASIS, HashgraphOASIS, PLANOASIS, ScuttlebuttOASIS, SOLIDOASIS, ThreeFoldOASIS & TRONOASIS providers.
- Improved and upgraded the OASIS Interfaces that the various OASIS Providers use such as IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISNFTProvider, IOASISProvider etc. 
- Releated to above all methods in the core IOASISStorageProvider interface now have additional async versions of their methods giving yet further performance/stability improvements.
- Releated to above upgraded all of the OASIS Providers to use the new interface versions.
- Added ActivateProviderAsync & DeActivateProviderAsync methods to BlockStackOASIS, ChainlinkOASIS, CosmosBlockchainOASIS, AzureCosmosDBOASIS, ElrondOASIS, EOSIOOASIS, EthereumOASIS, HashgraphOASIS, PLANOASIS, ScuttlebuttOASIS, SOLIDOASIS, ThreeFoldOASIS & TRONOASIS providers.
- Added HostURI, ChainPrivateKey, ChainId & ContractAddress properties to EthereumOASIS Provider.
- Refactored EthereumOASIS Provider so setup/init code is now done in the ActivateProviderAsync & ActivateProvider methods rather than the constructor giving further performance/stability improvements.
- Added Search, SearchAsync, Import, ImportAsync, ExportAllDataForAvatarByIdAsync, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByUsernameAsync, ExportAllDataForAvatarByEmail, ExportAllDataForAvatarByEmailAsync, ExportAll & ExportAllAsync methods to IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, SolanaOASIS & SQLLiteDBOASIS providers.
- Added new overloads to SendTransactionById method in IOASISBlockchainStorageProvider interface that takes a token in OASIS.API.Core.
- Renamed LoadAvatarForProviderKeyAsync to LoadAvatarByProviderKeyAsync and LoadAvatarForProviderKey method to LoadAvatarByProviderKey in IOASISStorageProvider interface and OASISStorageProviderBase in OASIS.API.Core.
- Updated ActivityPubOASIS, AzureCosmosDBOASIS, BlockStackOASIS, ChainLinkOASIS, CosmosBlockchainOASIS, ElrondOASIS, HashgraphOASIS, HoloOASIS, IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, PLANOASIS, ScuttlebuttOASIS, SolanaOASIS, SOLIDOASIS, EOSOASIS, EthereumOASIS, SQLLiteDBOASIS, TelosOASIS, ThreeFoldOASIS & TRONOASOS providers to reflect the above changes as well as other improvements, better layout, etc.
- Added new OASIS Provider Template making it easier for devs to jump in and build new OASIS Providers. https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/tree/master/NextGenSoftware.OASIS.API.Providers.ProviderNameOASIS and https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/wiki/OASIS-Provider-Template.
- Related to above the Provider Development section on our WIKI has also been updated: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/wiki/Provider-Development.
- Created and released new OASIS Runtime: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/releases/tag/OASIS-Runtime-v2.3.1. This allows the OASIS Runtime to be embedded and integrated directly into any c# project (app, game, website, service etc) and so can also work offline unlike the REST API Service.
- Related to above created new [NextGenSoftware.OASIS.API.Native.EndPoint](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint) NuGet Package (users OASIS Runtime under the hood just like the REST API Service does) to be used to integrate the OASIS API direcly in your app bypassing the existing REST API. Can be used for offline/poor conncection use cases, etc. This version will also of of course be faster than the REST API. This version also contains some functionality not currently implemented in the REST API.
- Improved the OASIS Architecture generally.
- Upgraded OASIS API/OASIS Architecture and all OASIS Providers to the latest and fastest version (v7) of .NET yet! Giving yet more performance/stability improvements & making it even more secure!
- Various stability/performance improvments.
- Improved error handling/reporting.
- Various under the hood improvements/bug fixes.


Below is the changelog/commit history for commits related to the OASIS API, there has been many more than this over the past year, these have been mostly related to STAR ODK Omniverse Interoperable Metaverse Low Code Generator.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/ba670757c2487d51a7cc2719892efde3557d8e12
- Moved old version of HoloNET (Redux) into Archive folder.
- Extracted out the generic CLI Engine from the STAR CLI project into its own re-usable project.
- Changed HoloOASIS namespace from NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core to NextGenSoftware.OASIS.API.Providers.HoloOASIS.
- Renamed HoloOASISBase to HoloOASIS and is no longer abstract (Desktop/Unity versions/projects have now also been removed to to the latest version of HoloNET no longer needing them to be seperate because of the new logging strategy).
- Updated OASISBootLoader to work with the latest changes to HoloOASIS Provider.
- ExtentionMethods in STAR have now been moved into new generic re-usable NextGenSoftware.Utilities project. Need to move other helper methods etc in OASIS.API.CORE ASAP.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/3c3c5bb30d2f263116e2f957534dc20e78d7310f
- CLI.Engine- Added global MessageColour, WorkingMessageColour, Error…MessageColour &amp; SuccessMessageColour, which are used in the ShowMessage, ShowWorkingMessage, ShowSuccessMessage &amp; ShowErrorMessageColour methods. The colour can be overidden per call to each method if needed.
- Updated CLI.Engine Package and bumped to v1.1.0.
- Created new NextGenSoftware.OASIS.API.Native.EndPoint NuGet Package to be used to integrate the OASIS API direcly in your app bypassing the existing REST API. Can be used for offline/poor conncection use cases, etc. This version will also of of course be faster than the REST API. This version also contains some functionality not currently implemented in the REST API.
- Updated OASISAPIManager used in new NextGenSoftware.OASIS.API.Native.EndPoint package. Added Map, Missions, Quests, Parks, OLAND, Search, Wallets, Keys & more!
- Added Solana, SQLLite & more to OASISProviders used in new NextGenSoftware.OASIS.API.Native.EndPoint package.
- Added new README to NextGenSoftware.OASIS.API.Native.EndPoint package.
- Created new NextGenSoftware.OASIS.API.Native.EndPoint.TestHarness project.
- Removed older SearchManager in ONODE.BLL project (the version in OASIS.API.Core is more up to date).
- Added EFCore ref to OASISBootLoader, needed for SQLLiteDBOASIS Provider.
- Updated OASISAPIManager in STAR ODK to now also include Missions, Quests, Parks, OLAND & Search.
- Added Solana & SQLLiteDB to OASISProviders in STAR ODK.
- Fixed bugs in ExtentionMethods in NextGenSoftware.Utilities.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/ed04fe85608b064062183bf805c32eee8261e715
- Removed older NextGenSoftware.OASIS.Providers.HoloOASIS.Desktop & NextGenSoftware.OASIS.Providers.HoloOASIS.Unity projects.
- Created NuGet Package for NextGenSoftware.OASIS.BootLoader.
- Created README for new NuGet Package NextGenSoftware.OASIS.BootLoader.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/782ab9a08493fd93106ffb678423faf31160c71f
- Updated HoloOASIS to latest version of HoloNET (v1.1.9 Embedded).
- Created new OASIS Runtimes:
- OASIS Runtime (With Holochain Conductors Embedded) v2.3.1
- OASIS Runtime (With Holochain Conductors Not Embedded) v2.3.1

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/960a997ead93a49c1f8252bf0bf6d9131ace3d1a
- Added missing OASIS Runtimes to OASIS Solution file.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/35b76bf0e0825b5e2d02aed8c579f6cfc50cce97
- Added NextGenSoftware.OASIS.Runtime.Setup projects to enable installing the new OASIS Runtime into the GAC (Global Assembly Cache) on a target machine meaning OAPP's won't need to come bundled with the runtime and can just use the version in the GAC resulting in MUCH smaller OAPPS (20 MB- 40 MB).
- Need to finish setting it up, still a WIP... not top priority at the moment... need to finish Holochain integrations first with HoloOASIS etc...

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/788c73df76df29df89126aaa94f481ee7560a0dd
- Renamed NextGenSoftware.OASIS.API.Manager to NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.
- Renamed OASISManager to OASISAPI in NextGenSoftware.OASIS.API.Native.Integrated.EndPoint project.
- Updated NextGenSoftware.OASIS.API.Native.Integrated.EndPoint README.
- Renamed NextGenSoftware.OASIS.API.Manager.TestHarness to NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.TestHarness.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/87a8895c1063451ff2edaa612ea6d94b866e3444
- Added new MapProviderType enum.
- Moved IBuilding & IQuest interfaces from OASIS.ONODE.BLL to OASIS.API.Core.
- Added new IOASISMapProvider interface, which map providers will implement in Our World such as MapBox, WRLD3D, etc allowing the mapping provider to be switched in real-time in the game and across the OASIS...
- Added additional null check to MapMetaData function in HolonManager making more robust.
- Added new CurrentMapProviderType & CurrentMapProvider properties to IMapManager/MapManager in OASIS.ONODE.BLL.
- Added new SetCurrentMapProvider method overloads to IMapManager/MapManager in OASIS.ONODE.BLL.
- Started upgrading HoloOASIS Provider to work with latest version of HoloNET.
- Implemented IOASISMapProvider interface in MapBoxOASIS Provider.
- Created new WRLD3DOASIS Provider that implements IOASISMapProvider interface.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/ad03baca39596cdf1f0c8cad3d2ca5efe228d9df
Azure Cosmos DB Provider

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/71add0310cfa7a8adde533861872bb299f81cdcd
- Updated NextGenSoftware.OASIS.API.Native.Integrated.EndPoint ReleasNotes.
- Bumped NextGenSoftware.OASIS.API.Native.Integrated.EndPoint to v1.0.3.
- Updated NextGenSoftware.OASIS.API.Native.Integrated.EndPoint README.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.EthereumOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.EthereumOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.HoloOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.HoloOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.IPFSOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.IPFSOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.MapBoxOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.MapBoxOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.
- Removed redundant methods from Neo4jOASIS Provider.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS.
- Fixed bugs in OrionProtocolOASIS Provider.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.PLANOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.PLANOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.ScuttleButtOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.ScuttleButtOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.TelosOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.TelosOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.TRONOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.TRONOASIS.
- Added NuGet Deployment metadata to NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.
- Added README to NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/7028a527299b7d27d6edb2f360ffae5357c57e82
- Made some corrections to the AzureCosmosDBOASIS Provider.
- IsWarning property setter in OASISResult no longer throws exceptions.
- Message property setter in OASISResult no longer throws exceptions.
- Added ProviderKey property to Entity class in  AzureCosmosDBOASIS Provider.
- ProviderKey is now set in AddAsync method in  CosmosDbRepository AzureCosmosDBOASIS Provider.
- Added new DeleteAsync overload that takes a id in  CosmosDbRepository in AzureCosmosDBOASIS Provider.
- Fixed a bug in DeActivateProvider method in AzureCosmosDBOASIS Provider where avatarDetailRepository and holonRepository were not nulled in AzureCosmosDBOASIS Provider.
- Fixed bugs in DeleteAvatar, DeleteAvatarAsync, DeleteHolon & DeleteHolonAsync methods where they were loading the full list rather than deleting directly in AzureCosmosDBOASIS Provider.
- Also fixed bugs DeleteHolonAsync methods where softDelete was not being handled in AzureCosmosDBOASIS Provider.
- LoadHolonsForParent methods were loading with id rather than parentHolonId in AzureCosmosDBOASIS Provider.
- LoadHolonsForParent methods Error handling did not set IsError to true in AzureCosmosDBOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/4790abf834400b1230ad3c8add059bb6deb6088b
- Started upgrading HoloOASIS Provider to the latest HoloNET version (v2.1.2).
- HcAvatar now extends HoloNETAuditEntryBaseClass.
- Removed id & user_id from HcAvatar & IHCAvatar interface.
- Renamed HcProfile to HcAvatar.
- Added HcAvatar property to HoloOASIS.
- HcAvatar is now initialized in the Initialize method in HoloOASIS.
- Removed older redudant HoloNET code.
- Updated LoadAvatarAsync method to work with latest HoloNET code (HcAvatar/HoloNETAuditEntryBaseClass).

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/e041bb74e383ea64b4e59e32019f74d8ef51b4a4
- Continued upgrading HoloOASIS Provider to work with the latest version of HoloNET.
- Re-wrote SaveAvatarAsync method in HoloOASIS.
- Re-wrote ConvertAvatarToHoloOASISAvatar method in HoloOASIS.
- Added provider_key & holon_type to IHcAvatar interface.
- Renamed IHcProfile interface to IHcAvatar.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/ebd3a957aa8aa5c17b8497a58f7a1248b0f038d4
- Added HcAvatar constructor that takes a HoloNETClient instance to HcAvatar.
- Added id property to HcAvatar.
- Renamed property uername to username in HcAvatar.
- Renamed property pssword to password in HcAvatar.
- Removed HcAvatar & _savingAvatars properties from HoloOASIS.
- Removed HcAvatar from HoloOASIS constructors and Initialize method.
- Added LoadAvatarForProviderKey method to HoloOASIS.
- Re-wroteLoadAvatarAsync method in HoloOASIS to work with the latest HoloNET.
- Re-wrote SaveAvatarAsync method in HoloOASIS to work with the latest HoloNET.
- Updated ConvertAvatarToHoloOASISAvatar method in HoloOASIS.
- Added id property to IHCAvatar.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/7533412721a547651e09a7de7822d46a3426f851
- Added HolochainFieldName attributes to each property in HcAvatar and made them all Pascal Case.
- Removed Karma, Level, Address & DOB from HcAvatar and IHcAvatar because these have been removed to AvatarDetail.
- Made each property Pascal Case in IHcAvatar.
- Added new useReflectionForSaving param to the constrcutors in HoloOASIS.
- Updated SaveAvatarAsync method in HoloOASIS.
- Updated ConvertAvatarToHoloOASISAvatar method in HoloOASIS.
- Updated ConvertHcAvatarToAvatar method in HoloOASIS.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/132d27c604231c497fbbbeff5a93452c84d72e4e
- Updated email templates that are sent when a user first registers to verify their email address as well as when they request to reset their password on AvatarManager.
- Added ZOME_LOAD_FUNCTION_BY_ID, ZOME_LOAD_FUNCTION_BY_USERNAME, ZOME_DELETE_FUNCTION_BY_ID, ZOME_DELETE_FUNCTION_BY_USERNAME & ZOME_DELETE_FUNCTION_BY_EMAIL constants to HoloOASIS.
- HoloOASIS constructors now default to using reflection.
- Updated/Added LoadAvatarAsync, SaveAvatarAsync, DeleteAvatarAsync, DeleteAvatar, DeleteAvatarByEmailAsync, DeleteAvatarByEmail, DeleteAvatarByUsernameAsync & DeleteAvatarByUsername method overloads in HoloOASIS.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/872b521ecb3b12d4735b98136db7f8dfb30bd202
- Tidied up and re-ordered methods in IOASISStorageProvider interface.
- Removed obsolete LoadAvatarAsync and Load method overloads that take username in IOASISStorageProvider interface.
- Added Search, ImportAsync, ExportAllDataForAvatarByIdAsync,ExportAllDataForAvatarByUsernameAsync & ExportAllDataForAvatarByEmailAsync methods to IOASISStorageProvider interface.
- Added CollectionOperationEnum to HoloOASIS Provider.
- Added OperationEnum to HoloOASIS Provdier.
- Re-wrote and made HoloOASIS much more efficient, now uses much less code so is easier to mantian in future.
- Re-wrote LoadAvatarAsync, LoadAvatar, LoadAvatarByEmailAsync, LoadAvatarByEmail, LoadAvatarByUsernameAsync, LoadAvatarByUsername, LoadAvatarDetailAsync, LoadAvatarDetail, LoadAvatarDetailByEmailAsync, LoadAvatarDetailByEmail, LoadAvatarDetailByUsernameAsync, LoadAvatarDetailByUsername, LoadAllAvatarsAsync
- Added Search, ImportAsync, Import, ExportAllDataForAvatarByIdAsync, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsernameAsync & ExportAllDataForAvatarByUsername methods to HoloOASIS Provider.
- Added new generic ExecuteOperationAsync & ExecuteOperation methods to HoloOASIS Provider that all other HoloNET operations now use making it much more efficient.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/cc3ea30417cf3021331161dfd6612dbba529b324
- Added additional properites to HcAvatar & IHcAvatar in HoloOASIS so now fully maps the full OASIS Avatar object and HolonBase it extends.
- Created HcAvatarDetail & IHcAvatarDetail in HoloOASIS so now fully maps the full OASIS AvatarDetail object and HolonBase it extends.
- Renamed _useReflectionForSaving to _useReflection in HoloOASIS.
- Updated LoadAllAvatarsAsync, LoadAllAvatars, LoadAllAvatarDetailsAsync, LoadAllAvatarDetails, SaveAvatarAsync, SaveAvatar, SaveAvatarDetailAsync, ConvertAvatarToHoloOASISAvatar, LoadAsync & Load methods in HoloOASIS Provider.
- Added ConvertAvatarDetailToHoloOASISAvatarDetail to HoloOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/6fcd57bdbb7cd7d72572d89404c7c194ed81c7fc
- Added HcObjectTypeEnum to HoloOASIS Provider.
- Added new IHcObject interface HoloOASIS Provider to allow to make the code even more generic and efficient.
- IHcAvatar and IHcAvatarDetail interfaces now extend the new IHcObject interface.
- Moved IHcAvatar, IHcAvatarDetail & IHcObject interfaces to new Interfaces folder.
- Finished implementing missing properties from IHolonBase interface to HcAvatarDetail in HoloOASIS Provider.
- Updated LoadAvatarAsync, LoadAvatar, LoadAvatarByEmailAsync & LoadAvatarByEmail methods in HoloOASIS Provider.
- Changed ConvertHcAvatarToAvatar method return type to IAvatar in HoloOASIS Provider.
- Added ConvertKeyValuePairToAvatar method to HoloOASIS Provider.
- Added ConvertHcAvatarDetailToAvatarDetail method to HoloOASIS Provider.
- Added ConvertKeyValuePairToAvatarDetail method to HoloOASIS Provider.
- Updated generic LoadAsync & Load methods in HoloOASIS Provider to make it even more efficient so all load operations now go through it.
- Added new HandleResponse method to HoloOASIS Provider.
- Started adding new generic SaveAsync method to HoloOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/0f51e452b901a64ea32722b493d679c2510e2d7a
- Added new ConvertAvatarToParamsObject, ConvertAvatarDetailToParamsObject methods to HoloOASIS Provider.
- Updated LoadAsync & Load methods in HoloOASIS Provider.
- Re-wrote and improved SaveAsync method in HoloOASIS Provider.
- Renamed HandleResponse to HandleLoadResponse in HoloOASIS Provider.
- Added HandleSaveResponse method to HoloOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/2bdefb4db118ccd5f5faf05b37fa20b51b8e104f
- Renamed OperationEnum to HcOperationEnum in HoloOASIS Provider.
- Updated SaveAsync method in HoloOASIS Provider.
- Added new Save method to HoloOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/8b48403e818d96161f6c3f86318fcb25ec44edd8
- Renamed CreateCollection to AddToCollection and DeleteCollection to RemoveFromCollection in CollectionOperationEnum in HoloOASIS Provider.
- Removed redundant code from HoloOASIS Provider.
- Made HoloOASIS Provider more efficient and uses a lot less code now thanks to more generic functions.
- Re-wrote LoadAvatarForProviderKeyAsync, LoadAvatarForProviderKey, SaveAvatarAsync, SaveAvatar, SaveAvatarDetailAsync, DeleteAvatarAsync, DeleteAvatar, DeleteAvatarAsync, DeleteAvatar, DeleteAvatarByEmailAsync, DeleteAvatarByUsernameAsync, DeleteAvatarByUsername & LoadHolonAsync methods in HoloOASIS Provider so uses less code and is more efficient thanks to more generic functions.
- Updated LoadAvatarByUsernameAsync, LoadAvatarByUsername, LoadAvatarDetailAsync, LoadAvatarDetail, LoadAvatarDetailByEmailAsync, LoadAvatarDetailByEmail, LoadAvatarDetailByUsernameAsync, LoadAvatarDetailByUsername, ExecuteOperationAsync & ExecuteOperation methods in HoloOASIS Provider.
- Added new LoadHolon, LoadHolonAsync, LoadHolon, LoadHolonsForParentAsync, LoadHolonsForParent, LoadHolonsForParentAsync, LoadHolonsForParent, LoadAllHolonsAsync & LoadAllHolons methods to HoloOASIS Provider.
- Added new generic LoadCollectionAsync, LoadCollection, DeleteAsync, Delete & HandleLoadCollectionResponse methods to HoloOASIS Provider.
- Added new additionalParams params to LoadAsync & Load methods in HoloOASIS Provider.
- Improved Error Logging/Handing in HoloOASIS Provider.
- Updated HoloNET nuget package used in HoloOASIS Provider to latest version (v2.1.4).

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/1329d21ea6ee16d82395f0e0e5b118c7b1615ce0
- Re-wrote HandleLoadResponse method in HoloOASIS Provider making it more efficient.
- Re-wrote HandleDeleteResponse method in HoloOASIS Provider making it more efficient.
- Re-wrote HandleSaveResponse method in HoloOASIS Provider making it more efficient.
- Adding new generic HandleResponse method to HoloOASIS Provider.
- Added new generic ConvertHCResponseToOASISResult method to HoloOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/4d53b2c76f281e3f2c6e3974396cbcab43112e49
- Changed LoadAvatarAsync to LoadAvatarByUsernameAsync in LoadAvatarForProviderAsync method in AvatarManager OASIS.API.Core.
- Removed HcProfile from HoloOASIS Provider.
- IHcAvatar & IHcAvatarDetail interfaces now extend IHoloNETAuditEntryBaseClass interface instead of IHcObject interface.
- Removed now redundant IHcObject.
- Added ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION & ZOME_LOAD_HOLON_FUNCTION_BY_ID constants to HoloOASIS Provider.
- Fixed LoadAvatarDetailAsync, LoadAvatarDetail, LoadAvatarDetailByEmailAsync, LoadAvatarDetailByEmail, LoadAvatarDetailByUsernameAsync & LoadAvatarDetailByUsername methods in HoloOASIS Provider.
- Swapped all IHcObject interfaces to IHoloNETAuditEntryBaseClass interface in LoadAsync, Load,  SaveAsync, Save, HandleLoadResponse, HandleSaveResponse, HandleDeleteResponse, HandleResponse & ConvertHCResponseToOASISResult methods.
- Temp commented out ExecuteOperation, ExecuteOperationAsync, LoadCollection & LoadCollectionAsync methods in HoloOASIS Provider. They are WIP and will be finished later...

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/4964cc675b8f02717871de7abe386fd96fd3801d
- Added new overloads to SendTransactionById method in IOASISBlockchainStorageProvider interface that takes a token in OASIS.API.Core.
- Renamed LoadAvatarForProviderKeyAsync to LoadAvatarByProviderKeyAsync and LoadAvatarForProviderKey method to LoadAvatarByProviderKey in IOASISStorageProvider interface and OASISStorageProviderBase in OASIS.API.Core.
- Updated ActivityPubOASIS, AzureCosmosDBOASIS, BlockStackOASIS, ChainLinkOASIS, CosmosBlockchainOASIS, ElrondOASIS, HashgraphOASIS, HoloOASIS, IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, PLANOASIS, ScuttlebuttOASIS, SolanaOASIS, SOLIDOASIS, EOSOASIS, EthereumOASIS, SQLLiteDBOASIS, TelosOASIS, ThreeFoldOASIS & TRONOASOS providers to reflect the above changes as well as other improvements, better layout, etc.
- Added new OASIS Provider Template project that others can copy and adapt when they wish to create a new OASIS Provider,
- Upgrading and improving AzureCosmosDBOASIS Proviider (WIP).

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/09a4a2af77c38bc3fe7805b3ea0a85673226b4d5
- Continued working on AzureCosmosDBOASIS provider.
- Removed AvatarDetaiil, Entity & Holon entities.
- AvatarDetailRepository now uses IAvatarDetail instead of AvatarDetail.
- AvatarRepository now uses IAvatar instead of Avatar.
- HolonRepository now uses IHolon instead of Holon.
- Added public to all methods in ICosmosDbClient, ICosmosDbClientFactory & IDocumentCollectionContext interfaces.
- IAvatarDetailRepository interface now uses IAvatarDetail instead of AvatarDetail.
- IAvatarRepository interface now uses IAvatar instead of Avatar.
- IHolonRepository interface now uses IHolon instead of Holon.
- Rewrote SaveAvatar, SaveAvatarAsync, SaveAvatarDetail, SaveAvatarDetailAsync, SaveHolon, SaveHolonAsync, SaveHolons & SaveHolonsAsync methods in AzureCosmosDBOASIS.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/3f74e1e61faec98ed56ea6cfef7d14e0c331c2b8
- Continued work on AzureCosmosDBOASIS Provider.
- Renamed ReadAllDocumentsAsync to ReadAllDocuments.
- Renamed GetListAsync to GetList.
- Added new DeleteAsync overload that takes a Guid to CosmosDbRepository and ICosmosDbRepository interface.
- Rewrote DeleteHolon, DeleteHolonAsync, LoadAllAvatars, LoadAllAvatarsAsync, LoadAllHolons, LoadAllHolonsAsync, LoadAvatar, LoadAvatarAsync, LoadAvatarDetail, LoadAvatarDetailAsync, LoadAvatarByProviderKey, LoadAvatarByProviderKeyAsync, LoadHolon & LoadHolonAsync methods in AzureCosmosDBOASIS making them more efficient, improved error handling/reporting etc.
- Fixed multiple bugs.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/66465c38ef290842d2057a837bfb204bc0719cbf
- Finished implementing LoadChildHolons method for Holon in OASIS.API.Core.
- Added caching to LoadAllHolons and LoadAllHolonsAsync methods in HolonManager in OASIS.API.Core.
- Added new LoadAvatarByEmail test to AzureCosmosDBOASIS Provider Test Harness.
- Added new ReadDocumentByField method to CosmosDbClient and ICosmosDbClient interface in AzureCosmosDbOASIS Provider.
- Added new GetByField method CosmosDbRepository and IRepository interface in AzureCosmosDbOASIS Provider.
- Added new ReadDocumentByField method to ICosmosDbClient interface in AzureCosmosDbOASIS Provider.
- Updated LoadAllHolonsAsync and LoadAllHolons methods in AzureCosmosDbOASIS provider.
- Added ImportAsync, Import, ExportAllDataForAvatarByIdAsync, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsernameAsync, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByEmailAsync, ExportAllDataForAvatarByEmail, ExportAllAsync, ExportAll, SearchAsync & Search methods to EthereumOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/92d3a7bbf1045357392cfb1cceb5e5a0c4eac5ab
- Added new ConvertToList method to new ListHelper in OASIS.API.Core.
- Added new ActivateProviderAsync and DeActivateProviderAsync methods to IOASISProvider interface in OASIS.API.Core.
- Added new ActivateProviderAsync and DeActivateProviderAsync methods to ProviderManager in OASIS.API.Core.
- Added new ActivateProviderAsync and DeActivateProviderAsync methods to OASISProvider in OASIS.API.Core.
- Added new AzureCosmosDBOASIS section to OASIS.DNA and updated OASISDNA.json for OASIS Web API and STAR ODK.
- Bumped WEB 4 OASIS API to v2.4.0.
- Added AzureCosmosDBOASIS to intro text to WEB4 OASIS API.
- Removed OASISDNA.json from AzureCosmosDBOASIS Test Harness.
- Added ActivateProviderAsync & DeActivateProviderAsync methods to BlockStackOASIS, ChainlinkOASIS, CosmosBlockchainOASIS, AzureCosmosDBOASIS, ElrondOASIS, EOSIOOASIS, EthereumOASIS, HashgraphOASIS, PLANOASIS, ScuttlebuttOASIS, SOLIDOASIS, ThreeFoldOASIS & TRONOASIS providers.
- Added HostURI, ChainPrivateKey, ChainId & ContractAddress properties to EthereumOASIS Provider.
- Refactored EthereumOASIS Provider so setup/init code is now done in the ActivateProviderAsync & ActivateProvider methods rather than the constructor.
- Removed LoadAvatar overloads that take a username and/or password from IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, SolanaOASIS & SQLLiteDBOASIS providers.
- Added Search, SearchAsync, Import, ImportAsync, ExportAllDataForAvatarByIdAsync, ExportAllDataForAvatarById, ExportAllDataForAvatarByUsername, ExportAllDataForAvatarByUsernameAsync, ExportAllDataForAvatarByEmail, ExportAllDataForAvatarByEmailAsync, ExportAll & ExportAllAsync methods to IPFSOASIS, LocalFileOASIS, MongoDBOASIS, Neo4jOASIS, SolanaOASIS & SQLLiteDBOASIS providers.
- Added ActivateProviderAsync & DeActivateProviderAsync methods to OASIS Provider Template.
- Added AzureCosmosDBOASIS Provider to OASISBootLoader.
- Added AzureCosmosDBOASIS Provider Init code to RegisterProvider method in OASISBootLoader.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/2928a137456813a4bd3991317906106c2d44c900
- Added HostURI, EOSAccountName, EOSChainId & EOSAccountPk properties to EOSIOOASIS Provider.
- Re-wrote and improved ActivateProviderAsync, ActivateProvider, DeActivateProviderAsync & DeActivateProvider methods in EOSIOOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/439edfeb4d420b21ca0607a38a921083896ad154
- Lots done on upgrading the OASIS NFT Architecture and API's...
- SendNFT and SendNFTAsync methods on IOASISNFTProvider now take a INFTWalletTransaction param instead of IWalletTransaction. They also return OASISResult<TransactionRespone > instead of OASISResult<bool> in OASIS.API.Core.
- Renamed ProviderActivated to IsProviderActivated in IOASISProvider interface in OASIS.API.Core.
- Created a new INFTWalletTransaction interface that extends the IWalletTransaction interface and adds the MintWalletAddress property in OASIS.API.Core.
- Added a new MemoText property to IWalletTransaction interface in OASIS.API.Core.
- Added new TransactionRespone object to OASIS.API.Core.
- All SendTraction and SendTractionAsync methods in IOASISBlockchainStorageProvider interface in OASIS.Core now return OASISResult<TransactionRespone> instead of OASISResult<bool> in OASIS.API.Core.
- Added new NFTManager to OASIS.API.ONODE.BLL. (WIP).
- Updated the constructors for OASISManager in OASIS.API.ONODE.BLL.
- Added new HandleDNA method to OASISManager in OASIS.API.ONODE.BLL.
- Upgraded OLANDManager in OASIS.API.ONODE.BLL.
- Moved PurchaseOlandRequest and PurchaseOlandResponse from OASIS.API.ONODE.WebAPI to OASIS.API.ONODE.BLL.
- Mid-way through moving the older NftService in OASIS.API.ONODE.WebAPI to OASIS.API.ONODE.BLL.
- Added AzureCosmosDBDBOASIS to AutoReplicationProviders, AutoFailOverProviders & AutoLoadBalanceProviders lists in OASIS_DNA.json in STAR ODK.
- Added OASIS.API.ONODE.WebAPI/OASIS_DNA.json to gitignore file.
- Renamed GetAndActivateDefaultProvider to GetAndActivateDefaultStorageProvider in OASISBookLoader.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/1d5df5e1efec249cbf1cb36d3407638be0d36798
- Started upgradding OASIS to .NET 7 making it even faster, more reliable, secure than ever! :).NET 7 is the fastest .NET yet!
- Added CheckForTransactionErrors method to ErrorHandling in NextGenSoftware.OASIS.Core.
- SendNFT & SendNFTAsync methods in IOASISNFTProvider interface now return OASISResult<TransactionRespone> instead of OASISResult<NFTTransactionRespone>.
- Renamed NFTTransactionRespone to TransactionRespone in OASIS.Core.
- Renamed ProviderActivated to IsProviderActivated in OASISProvider in OASIS.Core.
- CreateNftTransaction in NFTManager in OASIS.API.ONODE.Core now returns OASISResult<TransactionRespone> instead of OASISResult<NFTTransactionRespone>.
- Renamed NextGenSoftware.OASIS.API.ONODE.BLL to NextGenSoftware.OASIS.API.ONODE.Core.
- Renamed NextGenSoftware.OASIS.API.ONODE.BLL.TestHarness to NextGenSoftware.OASIS.API.ONODE.Core.TestHarness.
- Renamed all instances of GetAndActivateDefaultProvider to GetAndActivateDefaultStorageProvider across the OASIS.
- Removed CargoService and NftService from Startup in NextGenSoftware.OASIS.API.ONODE.WebAPI.
- Updated all OASIS Providers to reflect latest changes to IOASISNFTProvider and IOASISBlockchainStorageProvider interfaces.
- Improved error handling/reporting in EOSOASIS, SolanaOASIS & EthereumOASIS Providers.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/84c87ac50f32e2e90a3203ce065f3fed1bcc168a
- Resolved all build errors and finally successfully upgraded to .NET 7! :)

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/5f58cf11639b47fdb3c807b37fbc1a29edc5ea13
- Fixed bugs in SetAndActivateCurrentStorageProvider method in ProviderManager making it even more robust with improved error handling etc.
- Started adding new GetAllAvatarNames methods to AvatarController in ONODE.WebAPI.
- Fixed a bug in ResetPassword method in AvatarService where the message was being set in the Result property instead of the Message one.
- Removed Connect and Disconnect methods in the Neo4jOASIS Provider.
- Re-wrote ActivateProvider and DeActivateProvider methods in Neo4jOASIS Provider fixing a number of bugs and improved error handling/reporting etc.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/d9bbd6628ef719efa1ef71ea47e4ddce3b9bd652
- Added new FullNameWithTitle property to Avatar and IAvatar in OASIS.API.Core.
- Added new LoadAllAvatarNames/LoadAllAvatarNamesAsync &  LoadAllAvatarNamesWithIds/LoadAllAvatarNamesWithIdsAsync methods to AvatarManager in OASIS.API.Core.
- LoadAllAvatars & LoadAllAvatarsAsync methods now takes an optional orderByName param in AvatarManager in OASIS.API.Core.
- Updated KeyManager in OASIS.API.Core to work with latest changes above.
- Added new GetAllAvatarNames and GetAllAvatarNamesWithIds methods to AvatarController in OASIS.API.Core.
- Trying to fix new annoying error in AttachAccountToContext method in JwtMiddleware in WebAPI, not sure why this is an issue now because nothing has been changed in this area! Arghhh
- Renamed Password to NewPassword and ConfirmPassword to ConfirmNewPassword in ResetPasswordRequest which is used in ResetPassword method in AvatarController in WebAPI.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/a97ae238b250d182b9a26cf84a7ec9d351501c7e
- Added new optional includeUsernames param to LoadAllAvatarNames/LoadAllAvatarNamesAsync method in AvatarManager in OASIS.API.Core.
- Added new optional includeUsernames param to LoadAllAvatarNamesWithIds/LoadAllAvatarNamesWithIdsAsync method in AvatarManager in OASIS.API.Core.
- Fixed ref to NextGenSoftware.OASIS.API.Native.Integrated.EndPoint in NextGenSoftware.OASIS.API.Core.TestHarness.
- Fixed ref to NextGenSoftware.OASIS.API.Native.Integrated.EndPoint in NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.TestHarness.
- Added new includeUserNames param to GetAllAvatarNames method in AvatarController.
- GetAllAvatarNamesWithIds now returns Dictionary<string, List<string>>> instead of Dictionary<string,string>>.
- Trying to fix annoying new issue in the JwtMiddleware after upgrading to .NET 7 in the REST ONODE Web API.
- Renamed GetAndActivateDefaultProvider to GetAndActivateDefaultStorageProvider in NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness.
- Removed NextGenSoftware.OASIS.API.Providers.EOSIOOASIS-OLD from NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness.
- Fixed ref to OASISLogo in NextGenSoftware.OASIS.OASISBootLoader.
- Renamed GetAndActivateDefaultProvider to GetAndActivateDefaultStorageProvider in OASISAPIManager.
- Fixed ref to NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor.Shared in NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor.Client.
- Fixed ref to NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor.Shared and NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor.Client in NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor.Server.
- Added ref to NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura in OASISProviders in STAR ODK.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/12fa7ab108aed812642479204b5c80176c461985
- Added includeIds param and removed removeDuplicates param from LoadAllAvatarNames & LoadAllAvatarNamesAsync methods in AvatarManager in OASIS.API.Core.
- Renamed LoadAllAvatarNamesWithIds to LoadAllAvatarNamesGroupedByName and LoadAllAvatarNamesWithIdsAsync to LoadAllAvatarNamesGroupedByNameAsync in AvatarManager in OASIS.API.Core.
- Added new includeUsernames and includeIds params to LoadAllAvatarNamesGroupedByName  and LoadAllAvatarNamesGroupedByNameAsync methods in AvatarController in REST WebAPI.
- Added new generic GroupAvatarNames and ProcessAvatarNames methods to AvatarManager in OASIS.API.Core.
- Refactored and improved LoadAllAvatarNamesGroupedByName /LoadAllAvatarNamesGroupedByNameAsync to use new generic GroupAvatarNames  method so is now more efficient and uses less code.
- Refactored and improved LoadAllAvatarNames/LoadAllAvatarNamesAsync to use new generic ProcessAvatarNames method so is now more efficient and uses less code.
- Renamed PrepareToRegisterAvatar method to PrepareToRegisterAvatarAsync and made async in AvatarManager in OASIS.API.Core.
- Added new CheckIfUsernameIsAlreadyInUse method to AvatarManager in OASIS.API.Core.
- Refactored PrepareToRegisterAvatarAsync to call new CheckIfUsernameIsAlreadyInUse method to ensure username is unique in AvatarManager in OASIS.API.Core.
- Added new username param to PrepareToRegisterAvatarAsync, Register and RegisterAsync methods in AvatarManager in OASIS.API.Core.
- Removed removeDuplicates param and added new includeIds param to GetAllAvatarNames method in AvatarController in REST WebAPI.
- Renamed GetAllAvatarNamesWithIds method to GetAllAvatarNamesGroupedByName in AvatarController in REST WebAPI.
- Added new includeUsernames and includeIds params to GetAllAvatarNamesGroupedByName method in AvatarController in REST WebAPI.
- Added Username property to CreateRequest object in  REST WebAPI.
- Updated RegisterAsync method in AvatarService in REST WebAPI to pass the new username param to the RegisterAsync method on AvatarManager.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/911d48719df4b7bb695e6926537b2bccc3646cd2
- Finished implementing new LevelManager which can now dynamically calculate the level for each karma score from a dynamically generated LevelLookup. It also contains a LevelThresholdWeighting property which controls how easy/hard it is to level up.
- As part of the above work, Karma has now been changed from an int to a long in AvatarDetail, IAvatarDetail interface & KarmaAkashicRecord.
- Added new AvatarBasicView which will be used in new api calls in the avatar api to list avatars to standard users (and will be used for the OASIS Avatar Directory, avatar lookup/searches, leader boards, etc.
- Fixed bugs in NextGenSoftware.OASIS.API.Core.TestHarness.
- Added new LevelManager tests to NextGenSoftware.OASIS.API.Core.TestHarness.
- Fixed ref issue for the OASIS Logo in NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.
- Made all methods and properties static in OASISAPI helper in NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.
- Made Karma long in SolanaAvatarDetailDto in SolanaOASIS Provider.
- Made karma long in AvatarDetailModel and KarmaAkashicRecordModel in SQLLiteDBOASIS Provider.
- Made karma long in AvatarDetail in MongoDBOASIS Provider.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/4a12c391d4faa80b6814ce06387703dc563d04b0
- Upgrading NFT API on OASIS API again.
- Added IOASISGEONFT interface to OASIS.API.Core.
- Added IOASISNFT interface to OASIS.API.Core.
- Added IOLand interface to OASIS.API.Core.
- Added IOLandPurchase interface to OASIS.API.Core.
- Added OASISGEONFT to Objects in OASIS.API.Core.
- Added OASISNFT to Objects in OASIS.API.Core.
- Added OLand to Objects in OASIS.API.Core.
- Added OLandPurchase to Objects in OASIS.API.Core.
- Updated OLANDManager in OASIS.API.ONODE.Core.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/2ca66ec6bf51984903dc9792882296a9190ea668
- Added the OASIS logo into a new solution folder Images/Logos so all projects can then refrence this for their NuGet packages and so will never have issue finding the correct logo/image again.
- Fixed the NextGenSoftware.OASIS.API.ONODE.Core.TestHarness project.
- Fixed a bug in the get-all-avatar-names-grouped-by-name avatar api function.
- Changed return type from OASISResult<int> to OASISResult<long> for GetKarmaForAvatar function in KarmaController in OASIS.API.ONODE.WebApi.
- NextGenSoftware.OASIS.OASISBootLoader now references new Images/Logo/OASISLogo128.jpg.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/b7aff5c8b66aab1a94289ef1479612d19e6d4a50
- Renamed IOASISGEONFT to IOASISGeoSpatialNFT.
- Renamed OASISGEONFT to OASISGeoSpatialNFT.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/d3791cefd3f2f67bc27edc54b47a4920d944c050
- Renamed IOASISGEONFT to IOASISGeoSpatialNFT.
- Renamed IOland to IOLand.
- Updated PurchaseOland function in OLANDManger so it no longer updates the CargoSaleId. Also removed ErrorMessage because this is part of OASISResult.
- Renamed _driver to Driver and converted to a public getter; setter on Neo4jOASIS OASIS Provider.
- Temp set startApolloServer param to false in BootOASIS function in OASISAPIManager in STAR ODK.
- Set OASIS_DNA.json and CelestialBodyDNA.json CopyToOutputDirectory config setting to "PreserveNewest" in NextGenSoftware.OASIS.STAR,csproj.
- Added a new string email param to the CreateAvatar/CreateAvatarAsync functions in STAR.cs in STAR ODK because the email use to be used as the default username but now these can be different.
- Updated STAR ODK Test Harness so the OASIS Provider tests now use IsProviderActivated instead of ProviderActivated.
- Updated Neo4j OASIS Provider tests in STAR ODK Test Harness to work with latest changes to the Neo4j OASIS Provider.
- Added new GetValidUsername function to STAR ODK Test Harness, which in turn calls through to the new CheckIfUsernameIsAlreadyInUse function on the AvatarManager in the OASIS API to ensure the new username is unique.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/37cdb7f02bd3fa6a6e8fdc5e9c09f9553e44a78a
- Removed reundant code from AvatarManger.
- Corrected bug in OLandUnitController in Web API REST API.
- Changed IOland to IOLand in IOlandService in WEB API REST API.
- Changed IOland to IOLand in OlandService in WEB API REST API.
- Updated OASIS API RELEASE HISTORY.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/255070316e06f2e6a062809520124e0d17915fb8
- Continued upgrading OASIS API NFT API.
- Moved INFTWalletTransaction interface to NFT sub-folder in Interfaces folder in OASIS.API.Core
- IOASISGeoSpatialNFT interface now extends IOASISNFT interface in OASIS.API.Core.
- Created NFTWalletTransaction implementing INFTWalletTransaction.
- Created INFTManager interface in OASIS.API.ONODE.Core.
- Removed _olandManager from NFTManager.
- Moved OlandByCountPrice, OlandUnitPrice, GetOlandPrice & PurchaseOland from NFTManager to OLandManager in OASIS.API.ONODE.Core.
- Renamed CreateNftTransaction function to CreateNftTransactionAsync in NFTManger in OASIS.API.ONODE.Core.
- Added INFTManager param to OLandManager constructor (DI) in OASIS.API.ONODE.Core.
- Updated PurchaseOland function on OLandManager (WIP) making it more generic and is now using the generic CreateNftTransactionAsync function on NFTManger in OASIS.API.ONODE.Core.
- Removed CargoSaleId property from PurchaseOlandRequest in OASIS.API.ONODE.Core.
- Halfway removing nftService from OLandController.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/f9769d9898957b4daf7c712483c00190a92f8841
- Continued upgradding the NFT API on the OASIS API.
- Added ConvertFromList function to ListHelper in OASIS.API.Core.Helpers.
- Changed Guid OlandId property to List<Guid> OlandIds on IOLandPurchase in OASIS.API.Core.
- Changed Guid OlandId property to List<Guid> OlandIds on OLandPurchase in OASIS.API.Core and removed ErrorMessage.
- Updated error message in CreateNftTransactionAsync function in NFTManger in OASIS.API.ONODE.Core.
- Renamed LoadAllOlands function to LoadAllOlandsAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed LoadOlandfunction to LoadOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed DeleteOland to DeleteOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed SaveOland to SaveOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed UpdateOland to UpdateOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed PurchaseOland to PurchaseOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed GetOlandPrice to GetOlandPriceAsync on OLandManager in OASIS.API.ONODE.Core.
- Renamed PurchaseOland to PurchaseOlandAsync on OLandManager in OASIS.API.ONODE.Core.
- Updated and fixed bugs in PurchaseOlandAsync function in OLandManager in OASIS.API.ONODE.Core.
- Updated PurchaseOlandAsync function in OLandManager in OASIS.API.ONODE.Core making more generic and fixed bugs.
- Changed Guid OlandId property to List<Guid> OlandIds on PurchaseOlandRequest in OASIS.API.ONODE.
- Rewrote PurchaseOlandResponse on OASIS.API.ONODE so it now contains TransactionHash,  OlandIds and OLandPurchaseId.
- Updating NftController in OASIS.API.ONODE.WebAPI which included removing INftService because services should not be used in WebAPI, all logic needs to go into the managers so it can be re-used in other ONODE types such as gRPC, GraphQL, Native Integrated, etc. as well as in STAR ODK etc. All logic has now been moved to NftManger in OASIS.API.ONODE.Core.
- Removed OLandService for same reasons as above All logic has now been moved to OLandManger in OASIS.API.ONODE.Core.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/a22e37d641c19c8d5314c602fd7c1ce662eb0bab
- Continued work on upgrading the OASIS API NFT API.
- Created IMintAndPlaceGeoSpatialNFTRequest, IOASISGeoSpatialNFT, IPlaceGeoSpatialNFTRequest, IPlaceGeoSpatialNFTRequestBase & IMintNFTTransaction interfaces in OASIS.API.Core.
- Updated IOASISNFT interface in OASIS.API.Core by adding ThumbnailUrl, Token, Hash and MetaData properties.
- Updated IOASISNFTProvider interface in OASIS.API.Core by adding MintNFT, MintNFTAsync, LoadNFT, LoadNFTAsync, LoadAllNFTsForAvatar, LoadAllNFTsForAvatarAsync, LoadAllNFTsForMintAddress, LoadAllNFTsForMintAddressAsync, PlaceGeoNFT, PlaceGeoNFTAsync, MintAndPlaceGeoNFT & MintAndPlaceGeoNFTAsync functions.
- Removed Date from IWalletTransaction and WalletTransaction in OASIS.API.Core (this should be set by the relevant managers (NFTManger, WalletManager etc) so the date is always set to Now and so the date cannot be forged then.)
- Added Hash, ThumbnailUrl, Token & MetaData to OASISNFT in OASIS.API.Core.
- Created NFTProviderType enum in OASIS.API.ONODE.Core.
- Updated INFTManager interface in OASIS.API.ONODE.Core by adding CreateNftTransactionAsync, CreateNftTransaction, MintNftAsync, MintNft, LoadNftAsync, LoadNft, GetNFTProvider, GetNFTProviderTypeFromProviderType & GetProviderTypeFromNFTProviderType functions.
- Created ICreateNftTransactionRequest, IPurchaseOlandRequest & IPurchaseOlandResponse interfaces in OASIS.API.ONODE.Core.
- Updated NFTManager in OASIS.API.ONODE.Core by adding CreateNftTransactionAsync, CreateNftTransaction, MintNftAsync, MintNft, LoadNftAsync, LoadNft, GetNFTProvider, GetNFTProviderTypeFromProviderType & GetProviderTypeFromNFTProviderType functions.
- Added CreateNftTransactionRequest object to OASIS.API.ONODE.Core.
- Removed CargoSaleId property from PurchaseOlandRequest object in OASIS.API.ONODE.Core.
- PurchaseOlandResponse object now implements new IPurchaseOlandResponse interface in OASIS.API.ONODE.Core.
- Updated NFTController in OASIS.API.ONODE.WebAPI by removing all references to the new obsolete NFTService and pointing methods CreateNftTransaction to the new NFTManger in OASIS.API.ONODE.Core instead. Also added new MintNFT function.
- Removed the rest of the now retired NFTService in OASIS.API.ONODE.WebAPI.

https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/commit/3ad18e238ecf98f66f76a483626694e43a1a00e7
- Continued on the new NFT API and OLAND API for the OASIS API...
- Added ProviderType property to IPlaceGeoSpatialNFTRequestBase interface on OASIS.API.Core.
- Renamed Name property to Title on IMintNFTTransaction interface on OASIS.API.Core.
- Removed Token property from IMintNFTTransaction interface on OASIS.API.Core.
- Added LoadNFT and LoadNFTAsync method overloads that take a hash on IOASISNFTProvider interface to on OASIS.API.Core.
- Added LoadAllGeoNFTsForAvatar, LoadAllGeoNFTsForAvatarAsync, LoadAllGeoNFTsForMintAddress & LoadAllGeoNFTsForMintAddressAsync methods to IOASISNFTProvider interface to on OASIS.API.Core.
- Added new MintNFTTransaction object to OASIS.API.Core that implements the IMintNFTTransaction interface.
- Moved NFTWalletTransaction from Objects/Wallets to Objects/NFT on OASIS.API.Core.
- Added IPFS, Holochain and All to NFTProviderType on OASIS.API.ONODE.Core.
- GetNFTProvider methods now return OASISResult<IOASISNFTProvider> on NFTManger/INFTManger on OASIS.API.ONODE.Core. so now comforms with rest of the OASIS standards.
- Removed Token property from ICreateNftTransactionRequest interface on OASIS.API.ONODE.Core.
- Removed Token on CreateNftTransactionAsync and CreateNftTransaction methods on OASIS.API.ONODE.Core.
- Improved error handling and upgraded CreateNftTransactionAsync, CreateNftTransaction, MintNftAsync, MintNft, LoadNftAsync & LoadNft methods on NFTManager on OASIS.API.ONODE.Core so they now comform to OASIS standards  using OASISResult etc.
- Added new LoadNft and LoadNftAsync method overloads that take a hash rather than Guid id on NFTManger on OASIS.API.ONODE.Core.
- Added new LoadAllNFTsForAvatarAsync, LoadAllNFTsForAvatar, LoadAllNFTsForMintAddressAsync, LoadAllNFTsForMintAddress, LoadAllGeoNFTsForAvatarAsync, LoadAllGeoNFTsForAvatar, LoadAllGeoNFTsForMintAddressAsync, LoadAllGeoNFTsForMintAddress, PlaceGeoNFTAsync, PlaceGeoNFT, MintAndPlaceGeoNFTAsync & MintAndPlaceGeoNFT, methods on NFTManger on OASIS.API.ONODE.Core.
- Added new Instance property on OLandManager on OASIS.API.ONODE.Core so now comforms to OASIS Standards and follows other Managers.
- Removed Token property on CreateNftTransactionRequest on OASIS.API.ONODE.Core.
- Renamed CreateNftTransaction to CreateNftTransactionAsync on NftController on OASIS.API.ONODE.WebAPI.
- Added MintNftAsync, LoadNftByIdAsync, LoadNftByHashAsync, LoadAllNFTsForAvatarAsync, LoadAllNFTsForMintAddressAsync, LoadAllGeoNFTsForAvatarAsync, LoadAllGeoNFTsForMintAddressAsync,PlaceGeoNFTAsync, MintAndPlaceGeoNFTAsync, GetProviderTypeFromNFTProviderType, GetNFTProviderTypeFromProviderType, GetNFTProviderFromNftProviderType & GetNFTProviderFromNftProviderType methods to NftController on OASIS.API.ONODE.WebAPI.
- Added GetOlandPrice, LoadAllOlands, LoadOlandAsync, DeleteOlandAsyn &, SaveOlandAsync & UpdateOlandAsync methods to OASIS.API.ONODE.WebAPI.
- Renamed MintNft  to MintNftAsync on SolanaService and ISolanaService on SolanOASIS Provider.
- Updated OASIS API RELEASE HISTORY.md
- Bumped OASIS API REST API to v3.0.0.
- Updated OASIS_API.postman_collection.json to support the latest release (v3.0.0) on OASIS.API.ONODE.WebUI.HTML.
- Added new MintNFT, MintNFTAsync, LoadNFT, LoadNFTAsync, LoadAllNFTsForAvatarAsync, LoadAllNFTsForAvatar, LoadAllNFTsForMintAddressAsync, LoadAllNFTsForMintAddress, LoadAllGeoNFTsForAvatarAsync, LoadAllGeoNFTsForAvatar, LoadAllGeoNFTsForMintAddressAsync, LoadAllGeoNFTsForMintAddress, PlaceGeoNFTAsync, PlaceGeoNFT, MintAndPlaceGeoNFTAsync & MintAndPlaceGeoNFT method overloads to BlockStackOASIS, ChainLinkOASIS, CosmosBlockChainOASIS, ElrondOASIS, EOSIOOASIS, HashgraphOASIS, HoloOASIS, SolanaOASIS, TelosOASIS, ThreeFoldOASIS & TRONOASIS OASIS Providers.
- Also added the new NFT methods above to the OASIS Provider Template.
- Removed MongoDB.Bson ref from NextGenSoftware.OASIS.API.Providers.MongoDBOASIS project file which was causing a version conflict in the other projects.

----------------------------------------------------------------------------------------------------------------------------
## 3.3.3 (17/04/25)

Way too many changes to list here, there were numerous internal releases between v3.0.0 and v3.3.3, some of these changes are listed below:

- ArbitrumOASIS Provider added.
- PolygonOASIS Provider added.
- Rootstock Provider added.
- Data API upgrade/overhaul.
- GeoNFT API upgraded.
- Various performance improvements.
- NFT API upgraded.
- Upgraded to .NET 8 which comes with various improvements, performance enhacements and other fixes etc.
- Numerous bug fixes.
- Misc improvements.

Going forward we are likely to keep the changelog more brief and will summarise the changes rather than list the full changelog! ;-)

----------------------------------------------------------------------------------------------------------------------------
