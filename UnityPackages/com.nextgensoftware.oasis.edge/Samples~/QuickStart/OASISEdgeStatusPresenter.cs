using NextGenSoftware.OASIS.Edge.Runtime;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity.Samples
{
    public sealed class OASISEdgeStatusPresenter : MonoBehaviour
    {
        [SerializeField] private OASISEdgeUnityHost edgeHost;
        [SerializeField] private string connectionState = "Not initialized";
        [SerializeField] private long pendingOperations;

        private void OnEnable()
        {
            if (edgeHost != null) edgeHost.StatusChanged += OnStatusChanged;
        }

        private void OnDisable()
        {
            if (edgeHost != null) edgeHost.StatusChanged -= OnStatusChanged;
        }

        private void OnStatusChanged(object sender, EdgeRuntimeStatus status)
        {
            connectionState = status == null
                ? "Not initialized"
                : $"{status.Connectivity} / {status.Synchronization}";
            pendingOperations = status == null ? 0 : status.PendingOperationCount;
            Debug.Log($"[OGEngineClient] {connectionState}; pending operations: {pendingOperations}");
        }
    }
}
