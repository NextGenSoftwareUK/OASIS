// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

struct Holon {
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
