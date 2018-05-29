using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Implements functionality to retrieve a resource as a stream 
    /// </summary>
    public interface IWebRequestHandler
    {
        /// <summary>
        /// Returns the requested resource as a Stream 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken);
    }
}