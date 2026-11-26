using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge
{
    /// <summary>
    /// Applies shared map commands to a live GO Map component through
    /// <see cref="GOMapUnityBridge"/>. Each command maps to one GO Map method (see the
    /// README for the full list). When the component lacks a method, or the call
    /// throws, <see cref="Apply"/> returns false and the provider reports the failure
    /// with the bridge's reason, instead of recording a change the scene never got.
    /// </summary>
    public sealed class GOMapCommandSink : IMapCommandSink
    {
        private double? _appliedBearing;

        public GOMapCommandSink(GOMapUnityBridge bridge)
        {
            Bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public GOMapUnityBridge Bridge { get; }

        public bool Apply(MapCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            switch (command.CommandType)
            {
                case MapCommandType.Draw3DObject:
                    return Bridge.TryInvoke("placeObject", command.Target, command.Parameters["x"], command.Parameters["y"]);
                case MapCommandType.Draw2DSpriteOnMap:
                    return Bridge.TryInvoke("placeSprite", command.Target, command.Parameters["x"], command.Parameters["y"]);
                case MapCommandType.Draw2DSpriteOnHUD:
                    return Bridge.TryInvoke("drawOnHUD", command.Target, command.Parameters["x"], command.Parameters["y"]);
                case MapCommandType.DrawRoute:
                    return DrawRoute((IEnumerable<Geolocation>)command.Parameters["waypoints"]);
                case MapCommandType.SetCamera:
                    return SetCamera(command.Location, (double)command.Parameters["zoom"], (double)command.Parameters["bearing"]);
                case MapCommandType.DropPin:
                    return WithCoordinate(command.Location, c => Bridge.TryInvoke("dropPin", c, command.Target));
                case MapCommandType.RemovePin:
                    return Bridge.TryInvoke("removePin", command.Target);
                case MapCommandType.PlaceEntity:
                    return WithCoordinate(command.Location,
                        c => Bridge.TryInvoke("place" + (MapPlacementKind)command.Parameters["kind"], command.Target, c));
                case MapCommandType.RemovePlacement:
                    return Bridge.TryInvoke("removePlacement", command.Target);
                case MapCommandType.SelectEntity:
                    return Bridge.TryInvoke("selectObject", command.Target);
                case MapCommandType.HighlightBuilding:
                    return Bridge.TryInvoke("highlightBuilding", command.Target);
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command.CommandType, "Unknown map command.");
            }
        }

        private bool DrawRoute(IEnumerable<Geolocation> waypoints)
        {
            object[] coordinates = waypoints.Select(w => Bridge.CreateCoordinate(w.Latitude, w.Longitude)).ToArray();
            if (coordinates.Any(c => c == null)) return false;

            Array typed = Array.CreateInstance(coordinates[0].GetType(), coordinates.Length);
            Array.Copy(coordinates, typed, coordinates.Length);
            return Bridge.TryInvoke("drawRoute", new object[] { typed }, out _);
        }

        private bool SetCamera(Geolocation centre, double zoom, double bearing)
        {
            if (!WithCoordinate(centre, c => Bridge.TryInvoke("centerMap", c))) return false;
            if (!Bridge.TryInvoke("setZoom", zoom)) return false;

            // GO Map's orbit is only driven when the bearing actually changes, so a
            // component without an orbit still supports pan and zoom.
            if (_appliedBearing == bearing || (_appliedBearing == null && bearing == 0.0)) return true;
            if (!Bridge.TryInvoke("UpdateValue", bearing)) return false;
            _appliedBearing = bearing;
            return true;
        }

        private bool WithCoordinate(Geolocation location, Func<object, bool> apply)
        {
            object coordinate = Bridge.CreateCoordinate(location.Latitude, location.Longitude);
            return coordinate != null && apply(coordinate);
        }
    }
}
