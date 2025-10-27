// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Galaxy.sol";
import "./SolarSystem.sol";
import "./Star.sol";
import "./Planet.sol";
import "./Asteroid.sol";
import "./Comet.sol";
import "./Meteroid.sol";

struct GalaxyCluster {
    Galaxy Galaxies;
    SolarSystem SoloarSystems;
    Star Stars;
    Planet Planets;
    Asteroid Asteroids;
    Comet Comets;
    Meteroid Meteroids;
    bool IsSuperCluster;
}