using System;

namespace JsonRpc
{

    /// <summary>
    /// Represents an exception thrown if the RPC server returns an error response.
    /// </summary>
    [System.Serializable]
	public class RpcException : Exception
	{

		private RpcError _RpcError;

		/// <summary>
		/// Initializes a new instance of the <see cref="RpcException"/> class.
		/// </summary>
		public RpcException() : base("Unknown RPC error") { }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="RpcException"/> class with a custom error message.
		/// </summary>
		/// <param name="message">The error message.</param>
		public RpcException(string message) : base(message) { }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="RpcException"/> class with a custom error message and inner exception.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="inner">The inner.</param>
		public RpcException(string message, Exception inner) : base(message, inner) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RpcException"/> class with the name of the method that failed and the error response from the RPC server.
		/// </summary>
		/// <param name="methodName">The name of the remote method that failed.</param>
		/// <param name="error">A <see cref="RpcError"/> instance containing details of the error.</param>
		public RpcException(string methodName, RpcError error) : base(error?.Message ?? "Unknown RPC error")
		{
			this.Data["MethodName"] = methodName;
			this.Data["ErrorCode"] = error?.Code;
			_RpcError = error;
		}

		protected RpcException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

		/// <summary>
		/// Gets the name of the remote method that returned an error.
		/// </summary>
		/// <value>The name of the method.</value>
		public string MethodName { get { return this.Data.Contains("MethodName") ? (string)this.Data["MethodName"] : String.Empty; } }
		/// <summary>
		/// Gets the <see cref="RpcError"/> describing detailed information about the error that occurred.
		/// </summary>
		/// <remarks>
		/// <para>This property is not serializable on all platforms, and may not pass across app domain/remoting boundaries etc.</para>
		/// </remarks>
		/// <value>The RPC error.</value>
		public RpcError RpcError { get { return _RpcError; } }
		/// <summary>
		/// Gets the RPC error code returned from the server, if any.
		/// </summary>
		/// <value>The RPC error as a nullable integer. Null if no error code was returned.</value>
		public int? ErrorCode { get { return this.Data.Contains("ErrorCode") ? (int?)this.Data["ErrorCode"] : null; } }
	}
}