// SPDX-License-Identifier: UNLICENSED
// compiler version must be greater than or equal to 0.8.3 and less than 0.9.0
pragma solidity ^0.8.9;

import "./SuperStar.sol";
import "./SolarSystem.sol";
import "./Star.sol";
import "./Planet.sol";
import "./Asteroid.sol";
import "./Comet.sol";
import "./Nebula.sol";
import "./Meteroid.sol";

struct Galaxy {
    SuperStar SuperStar;
    SolarSystem SolarSystems;
    Nebula Nebulas;
    Star Stars;
    Planet Planets;
    Asteroid Asteroids;
    Comet Comets;
    Meteroid Meteroids;    
}