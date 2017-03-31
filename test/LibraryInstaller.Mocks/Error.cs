using LibraryInstaller.Contracts;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="IError"/> object.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IError" />
    internal class Error : IError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// The error code used to uniquely identify the error.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The user friendly description of the error.
        /// </summary>
        public string Message { get; set; }
    }
}
