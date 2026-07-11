using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class PortalTrigger : MonoBehaviour
    {
        public string GameId;

        private bool _isEntering;

        private async void OnTriggerEnter(Collider other)
        {
            if (_isEntering)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            _isEntering = true;
            var result = await OmniverseKernel.Instance.EnterPortalAsync(GameId, null);
            if (result.IsError)
            {
                Debug.LogError($"Portal transition failed: {result.Message}");
            }

            _isEntering = false;
        }
    }
}

