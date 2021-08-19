using Enums;
using Models.Requests;
using Services.MapboxMaps;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mono
{
    public class MapMediator : MonoBehaviour
    {
        [FormerlySerializedAs("OutputTransform")] 
        public Transform outputTransform;

        private IMapboxMapService _mapboxMapService;

        private async void Start()
        {
            _mapboxMapService = new MapboxMapService();
            var rasterImage = await _mapboxMapService
                .GetRasterTileMap(new GetRasterTileRequest() { });
            if (rasterImage.StatusCode == ResponseStatusCode.Success)
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(rasterImage.Payload.Bytes);
                outputTransform.GetComponent<Renderer>().material.mainTexture = tex;
            }
            else
            {
                Debug.LogError(rasterImage.Message);
            }
        }
    }
}