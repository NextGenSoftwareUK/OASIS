// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "..\Common\Omiverse.sol";
import "..\Common\Multiverse.sol";
import "..\Common\Dimension.sol";
import "..\Enums\DimensionLevel.sol";
import "..\Common\GreatGrandSuperStar.sol";
import "..\Common\GrandSuperStar.sol";
import "..\Common\Star.sol";
import "..\Enums\SubDimensionLevel.sol";
import "..\Common\GalaxyCluster.sol";
import "..\Common\Galaxy.sol";
import "..\Common\SolarSystem.sol";
import "..\Common\Node.sol";
import "..\Common\Zoom.sol";
import "..\Common\Moon.sol";
import "..\Common\Planet.sol";
import "..\Common\SuperStar.sol";
import "..\Common\Universe.sol";
import "..\Common\Zoom.sol";

struct Holon {
    string HolonId;
    string ParentOmiverseId;
    Omiverse ParentOmiverse;
    string ParentMultiverseId;
    Multiverse ParentMultiverse;
    string ParentUniverseId;
    Universe ParentUniverse;
    string ParentDimensionId;
    Dimension ParentDimension;
    DimensionLevel DimensionLevel;
    SubDimensionLevel SubDimensionLevel;
    string ParentGalaxyClusterId;
    GalaxyCluster ParentGalaxyCluster;
    string ParentGalaxyId;
    Galaxy ParentGalaxy;
    string ParentSolarSystemId;
    SolarSystem ParentSolarSystem;
    string ParentGreatGrandSuperStarId;
    GreatGrandSuperStar ParentGreatGrandSuperStar;
    string ParentGrandSuperStarId;
    GrandSuperStar ParentGrandSuperStar;
    string ParentSuperStarId;
    SuperStar ParentSuperStar;
    string ParentStarId;
    Star ParentStar;
    string ParentPlanetId;
    Planet ParentPlanet;
    string ParentMoonId;    
    Moon ParentMoon;
    string ParentZomeId;
    Zome ParentZome;
    string ParentHolonId;
    Holon[] Children;
    Holon[] ChildrenTest;
    Node[] Nodes;
}
