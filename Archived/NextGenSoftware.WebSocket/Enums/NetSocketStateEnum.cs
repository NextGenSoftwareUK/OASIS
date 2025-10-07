using System;

namespace NextGenSoftware.WebSocket
{
        public enum NetSocketState
        {
            //
            // Summary:
            //     Reserved for future use.
            None = 0,
            //
            // Summary:
            //     The connection is negotiating the handshake with the remote endpoint.
            Connecting = 1,
            //
            // Summary:
            //     The initial state after the HTTP handshake has been completed.
            Open = 2,
            //
            // Summary:
            //     A close message was sent to the remote endpoint.
            CloseSent = 3,
            //
            // Summary:
            //     A close message was received from the remote endpoint.
            CloseReceived = 4,
            //
            // Summary:
            //     Indicates the WebSocket close handshake completed gracefully.
            Closed = 5,
            //
            // Summary:
            //     Reserved for future use.
            Aborted = 6
        }
    
}
