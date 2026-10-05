// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract OASISStorage {
    // Avatar struct - separate from AvatarDetail
    struct Avatar {
        uint256 id;
        string username;
        string email;
        string firstName;
        string lastName;
        string title;
        string password;
        uint8 avatarType;
        bool acceptTerms;
        bool isVerified;
        string jwtToken;
        uint256 passwordReset;
        string refreshToken;
        string resetToken;
        uint256 resetTokenExpires;
        string verificationToken;
        uint256 verified;
        uint256 lastBeamedIn;
        uint256 lastBeamedOut;
        bool isBeamedIn;
        uint256 createdDate;
        uint256 modifiedDate;
        string description;
        bool isActive;
    }
    
    // AvatarDetail struct - completely separate from Avatar
    struct AvatarDetail {
        uint256 id; // Separate ID from Avatar
        string username;
        string email;
        uint256 karma;
        uint256 level;
        uint256 xp;
        string model3D;
        string umaJson;
        string portrait;
        uint256 dob;
        string address;
        string town;
        string county;
        string country;
        string postcode;
        string landline;
        string mobile;
        uint256 favouriteColour;
        uint256 starcliColour;
        uint256 createdDate;
        uint256 modifiedDate;
        string description;
        bool isActive;
    }
    
    // Holon struct
    struct Holon {
        uint256 id;
        string name;
        string description;
        uint256 parentHolonId;
        string providerUniqueStorageKey;
        uint256 versionId;
        bool isNewHolon;
        bool isActive;
        uint256 deletedByAvatarId;
        uint256 deletedDate;
        uint256 createdByAvatarId;
        uint256 modifiedByAvatarId;
        uint256 createdDate;
        uint256 modifiedDate;
        uint8 holonType;
        uint8 dimensionLevel;
        uint8 subDimensionLevel;
        uint256 parentOmniverseId;
        uint256 parentMultiverseId;
        uint256 parentUniverseId;
        uint256 parentDimensionId;
        uint256 parentGalaxyClusterId;
        uint256 parentGalaxyId;
        uint256 parentSolarSystemId;
        uint256 parentPlanetId;
        uint256 parentMoonId;
        uint256 parentStarId;
        uint256 parentZomeId;
        uint256 parentHolonId;
        string metaData;
    }
    
    // Separate mappings for Avatar and AvatarDetail
    mapping(uint256 => Avatar) public avatars;
    mapping(string => uint256) public usernameToAvatarId;
    mapping(string => uint256) public emailToAvatarId;
    
    mapping(uint256 => AvatarDetail) public avatarDetails;
    mapping(string => uint256) public usernameToAvatarDetailId;
    mapping(string => uint256) public emailToAvatarDetailId;
    
    mapping(uint256 => Holon) public holons;
    mapping(string => uint256) public providerKeyToHolonId;
    
    uint256 public avatarCount;
    uint256 public avatarDetailCount;
    uint256 public holonCount;
    
    event AvatarCreated(uint256 indexed id, string username, string email);
    event AvatarDetailCreated(uint256 indexed id, string address);
    event HolonCreated(uint256 indexed id, string name);
    
    // Avatar functions
    function createAvatar(
        string memory _username,
        string memory _email,
        string memory _firstName,
        string memory _lastName,
        string memory _title,
        string memory _password,
        uint8 _avatarType,
        bool _acceptTerms,
        string memory _description
    ) public returns (uint256) {
        require(usernameToAvatarId[_username] == 0, "Username already exists");
        require(emailToAvatarId[_email] == 0, "Email already exists");
        
        avatarCount++;
        uint256 avatarId = avatarCount;
        
        avatars[avatarId] = Avatar({
            id: avatarId,
            username: _username,
            email: _email,
            firstName: _firstName,
            lastName: _lastName,
            title: _title,
            password: _password,
            avatarType: _avatarType,
            acceptTerms: _acceptTerms,
            isVerified: false,
            jwtToken: "",
            passwordReset: 0,
            refreshToken: "",
            resetToken: "",
            resetTokenExpires: 0,
            verificationToken: "",
            verified: 0,
            lastBeamedIn: 0,
            lastBeamedOut: 0,
            isBeamedIn: false,
            createdDate: block.timestamp,
            modifiedDate: block.timestamp,
            description: _description,
            isActive: true
        });
        
        usernameToAvatarId[_username] = avatarId;
        emailToAvatarId[_email] = avatarId;
        
        emit AvatarCreated(avatarId, _username, _email);
        return avatarId;
    }
    
    // AvatarDetail functions - completely separate from Avatar
    function createAvatarDetail(
        string memory _username,
        string memory _email,
        uint256 _karma,
        uint256 _level,
        uint256 _xp,
        string memory _model3D,
        string memory _umaJson,
        string memory _portrait,
        uint256 _dob,
        string memory _address,
        string memory _town,
        string memory _county,
        string memory _country,
        string memory _postcode,
        string memory _landline,
        string memory _mobile,
        uint256 _favouriteColour,
        uint256 _starcliColour,
        string memory _description
    ) public returns (uint256) {
        avatarDetailCount++;
        uint256 avatarDetailId = avatarDetailCount;
        
        avatarDetails[avatarDetailId] = AvatarDetail({
            id: avatarDetailId,
            username: _username,
            email: _email,
            karma: _karma,
            level: _level,
            xp: _xp,
            model3D: _model3D,
            umaJson: _umaJson,
            portrait: _portrait,
            dob: _dob,
            address: _address,
            town: _town,
            county: _county,
            country: _country,
            postcode: _postcode,
            landline: _landline,
            mobile: _mobile,
            favouriteColour: _favouriteColour,
            starcliColour: _starcliColour,
            createdDate: block.timestamp,
            modifiedDate: block.timestamp,
            description: _description,
            isActive: true
        });
        
        usernameToAvatarDetailId[_username] = avatarDetailId;
        
        emit AvatarDetailCreated(avatarDetailId, _address);
        return avatarDetailId;
    }
    
    function createHolon(
        string memory _name,
        string memory _description,
        uint256 _parentHolonId,
        string memory _providerUniqueStorageKey,
        uint256 _createdByAvatarId,
        uint8 _holonType,
        uint8 _dimensionLevel,
        uint8 _subDimensionLevel,
        uint256 _parentOmniverseId,
        uint256 _parentMultiverseId,
        uint256 _parentUniverseId,
        uint256 _parentDimensionId,
        uint256 _parentGalaxyClusterId,
        uint256 _parentGalaxyId,
        uint256 _parentSolarSystemId,
        uint256 _parentPlanetId,
        uint256 _parentMoonId,
        uint256 _parentStarId,
        uint256 _parentZomeId,
        string memory _metaData
    ) public returns (uint256) {
        holonCount++;
        uint256 holonId = holonCount;
        
        holons[holonId] = Holon({
            id: holonId,
            name: _name,
            description: _description,
            parentHolonId: _parentHolonId,
            providerUniqueStorageKey: _providerUniqueStorageKey,
            versionId: 1,
            isNewHolon: true,
            isActive: true,
            deletedByAvatarId: 0,
            deletedDate: 0,
            createdByAvatarId: _createdByAvatarId,
            modifiedByAvatarId: _createdByAvatarId,
            createdDate: block.timestamp,
            modifiedDate: block.timestamp,
            holonType: _holonType,
            dimensionLevel: _dimensionLevel,
            subDimensionLevel: _subDimensionLevel,
            parentOmniverseId: _parentOmniverseId,
            parentMultiverseId: _parentMultiverseId,
            parentUniverseId: _parentUniverseId,
            parentDimensionId: _parentDimensionId,
            parentGalaxyClusterId: _parentGalaxyClusterId,
            parentGalaxyId: _parentGalaxyId,
            parentSolarSystemId: _parentSolarSystemId,
            parentPlanetId: _parentPlanetId,
            parentMoonId: _parentMoonId,
            parentStarId: _parentStarId,
            parentZomeId: _parentZomeId,
            parentHolonId: _parentHolonId,
            metaData: _metaData
        });
        
        providerKeyToHolonId[_providerUniqueStorageKey] = holonId;
        
        emit HolonCreated(holonId, _name);
        return holonId;
    }
    
    // Separate getters for Avatar and AvatarDetail
    function getAvatar(uint256 _id) public view returns (Avatar memory) {
        return avatars[_id];
    }
    
    function getAvatarByUsername(string memory _username) public view returns (Avatar memory) {
        uint256 avatarId = usernameToAvatarId[_username];
        return avatars[avatarId];
    }
    
    function getAvatarByEmail(string memory _email) public view returns (Avatar memory) {
        uint256 avatarId = emailToAvatarId[_email];
        return avatars[avatarId];
    }
    
    function getAvatarDetail(uint256 _id) public view returns (AvatarDetail memory) {
        return avatarDetails[_id];
    }
    
    function getAvatarDetailByUsername(string memory _username) public view returns (AvatarDetail memory) {
        uint256 avatarDetailId = usernameToAvatarDetailId[_username];
        return avatarDetails[avatarDetailId];
    }
    
    function getHolon(uint256 _id) public view returns (Holon memory) {
        return holons[_id];
    }
    
    function getHolonByProviderKey(string memory _providerKey) public view returns (Holon memory) {
        uint256 holonId = providerKeyToHolonId[_providerKey];
        return holons[holonId];
    }
    
    // Separate update functions for Avatar and AvatarDetail
    function updateAvatar(
        uint256 _id,
        string memory _firstName,
        string memory _lastName,
        string memory _description
    ) public {
        require(avatars[_id].id != 0, "Avatar does not exist");
        
        avatars[_id].firstName = _firstName;
        avatars[_id].lastName = _lastName;
        avatars[_id].description = _description;
        avatars[_id].modifiedDate = block.timestamp;
    }
    
    function updateAvatarDetail(
        uint256 _id,
        string memory _address,
        string memory _country,
        uint256 _level,
        uint256 _xp,
        uint256 _hp,
        uint256 _mana,
        uint256 _stamina
    ) public {
        require(avatarDetails[_id].id != 0, "AvatarDetail does not exist");
        
        avatarDetails[_id].address = _address;
        avatarDetails[_id].country = _country;
        avatarDetails[_id].level = _level;
        avatarDetails[_id].xp = _xp;
        avatarDetails[_id].hp = _hp;
        avatarDetails[_id].mana = _mana;
        avatarDetails[_id].stamina = _stamina;
    }
    
    function softDeleteAvatar(uint256 _id, uint256 _deletedByAvatarId) public {
        require(avatars[_id].id != 0, "Avatar does not exist");
        
        avatars[_id].isActive = false;
        avatars[_id].deletedByAvatarId = _deletedByAvatarId;
        avatars[_id].deletedDate = block.timestamp;
        avatars[_id].modifiedDate = block.timestamp;
    }
    
    function softDeleteAvatarDetail(uint256 _id, uint256 _deletedByAvatarId) public {
        require(avatarDetails[_id].id != 0, "AvatarDetail does not exist");
        
        avatarDetails[_id].isActive = false;
        avatarDetails[_id].deletedByAvatarId = _deletedByAvatarId;
        avatarDetails[_id].deletedDate = block.timestamp;
    }
    
    function softDeleteHolon(uint256 _id, uint256 _deletedByAvatarId) public {
        require(holons[_id].id != 0, "Holon does not exist");
        
        holons[_id].isActive = false;
        holons[_id].deletedByAvatarId = _deletedByAvatarId;
        holons[_id].deletedDate = block.timestamp;
        holons[_id].modifiedDate = block.timestamp;
    }
}
