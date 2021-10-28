// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./Planet.sol";
import "./Star.sol";
import "./SolarSystem.sol";
import "./GalaxyCluster.sol";
import "./Nebula.sol";
import "./Asteroid.sol";
import "./Comet.sol";
import "./Meteroid.sol";

struct Universe {
    GalaxyCluster GalaxyClusters;
    SolarSystem SolarSystems;
    Nebula Nebulas;
    Star Stars;
    Planet Planets;
    Asteroid Asteroids;
    Comet Comets;
    Meteroid Meteroids;
}