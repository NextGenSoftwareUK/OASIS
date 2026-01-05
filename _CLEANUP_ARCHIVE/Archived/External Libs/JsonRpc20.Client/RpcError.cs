namespace JsonRpc
{
    /// <summary>
    /// A generalized representation of an error response from an RPC call.
    /// </summary>
	[System.Serializable]
    public class RpcError
	{
		/// <summary>
		/// Gets or sets a numeric error code.
		/// </summary>
		/// <value>The code.</value>
		public int Code { get; set; }
		/// <summary>
		/// Gets or sets the error message.
		/// </summary>
		/// <value>The message.</value>
		public string Message { get; set; }
		/// <summary>
		/// Gets or sets any server/method specific data provided about the error.
		/// </summary>
		/// <value>The data.</value>
		public object Data { get; set; }
	}
}