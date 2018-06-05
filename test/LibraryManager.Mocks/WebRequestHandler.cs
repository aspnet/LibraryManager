using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// Public Mock class for unit testing 
    /// </summary>
    public class WebRequestHandler : IWebRequestHandler
    {
        /// <summary>
        /// Returns a Stream for testing purposes
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            byte[] content = uniEncoding.GetBytes("Stream content for test");

            return Task.FromResult<Stream>( new MemoryStream(content));
        }
    }
}
