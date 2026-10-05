"use client";

import Map from "ol/Map";
import View from "ol/View";
import TileLayer from "ol/layer/Tile";
import OSM from "ol/source/OSM";
import Feature from "ol/Feature";
import Point from "ol/geom/Point";
import { fromLonLat, toLonLat } from "ol/proj";
import { Vector as VectorLayer } from "ol/layer";
import { Vector as VectorSource } from "ol/source";
import { Icon, Style } from "ol/style";
import { useEffect, useRef, useState, useCallback } from "react";

import Modal from "@/components/Modal";
import { Button } from "@/components/ui/button";
import "ol/ol.css";
import MapBrowserEvent from "ol/MapBrowserEvent";

interface LocationPickerModalProps {
  onSelect: (coords: { latitude: number; longitude: number }) => void;
  setIsOpen: (open: boolean) => void;
}

export default function LocationPickerModal({
  onSelect,
  setIsOpen,
}: LocationPickerModalProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const vectorSource = useRef(new VectorSource());
  const mapObj = useRef<Map | null>(null);
  const [selectedCoords, setSelectedCoords] = useState<{
    latitude: number;
    longitude: number;
  } | null>(null);

  const handleMapClick = useCallback((event: MapBrowserEvent) => {
    const [lon, lat] = toLonLat(event.coordinate);
    setSelectedCoords({ latitude: lat, longitude: lon });

    vectorSource.current.clear();

    const marker = new Feature({
      geometry: new Point(fromLonLat([lon, lat])),
    });

    marker.setStyle(
      new Style({
        image: new Icon({
          anchor: [0.5, 1],
          src: "/mapIcon.png",
          width: 30,
          height: 30,
        }),
      })
    );

    vectorSource.current.addFeature(marker);
  }, []);

  useEffect(() => {
    if (!mapRef.current) return;

    const baseLayer = new TileLayer({ source: new OSM() });
    const markerLayer = new VectorLayer({ source: vectorSource.current });

    const map = new Map({
      target: mapRef.current,
      layers: [baseLayer, markerLayer],
      view: new View({
        center: fromLonLat([0, 0]),
        zoom: 2,
      }),
    });

    map.on("click", handleMapClick);
    mapObj.current = map;

    return () => {
      map.setTarget(undefined);
    };
  }, [handleMapClick]);

  const handleConfirm = () => {
    if (!selectedCoords) return;
    onSelect(selectedCoords);
    setIsOpen(false);
  };

  return (
    <Modal isNonUrlModal onCloseFunc={() => setIsOpen(false)}>
      <div
        ref={mapRef}
        className="w-full h-[400px] rounded-xl overflow-hidden"
      />
      <div className="mt-2 text-right">
        <Button
          onClick={handleConfirm}
          disabled={!selectedCoords}
          variant="default"
          size="default"
          className="w-full"
        >
          Choose
        </Button>
      </div>
    </Modal>
  );
}
