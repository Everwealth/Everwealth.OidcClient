namespace Everwealth.OidcClient
{
    /// <summary>
    /// Facilitates communication between the app entry point and the callback function.
    /// </summary>
    public class BrowserMediator
    {
        private static BrowserMediator _instance;
        private BrowserMediator() { }

        /// <summary>
        /// Singleton instance of the <see cref="BrowserMediator"/> class.
        /// </summary>
        public static BrowserMediator Instance
        {
            get { return _instance ?? (_instance = new BrowserMediator()); }
        }

        /// <summary>
        /// Method signature required for methods subscribing to the BrowserMessageReceived event.
        /// </summary>
        /// <param name="message">Message that has been received.</param>
        public delegate void MessageReceivedEventHandler(string message);

        /// <summary>
        /// Event listener for subscribing to message received events.
        /// </summary>
        public event MessageReceivedEventHandler BrowserMessageReceived;

        /// <summary>
        /// Send a response message to all listeners.
        /// </summary>
        /// <param name="response">Response message to send to all listeners.</param>
        public void Send(string response)
        {
            BrowserMessageReceived?.Invoke(response);
        }

        /// <summary>
        /// Send a cancellation response message "UserCancel" to all listeners.
        /// </summary>
        public void Cancel()
        {
            Send("UserCancel");
        }

        /// <summary>
        /// Send a cancellation response message "Success" to all listeners.
        /// </summary>
        public void Success()
        {
            Send("Success");
        }
    }
}