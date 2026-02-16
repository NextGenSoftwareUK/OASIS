using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class PortalVisualSpin : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 65f;

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}

