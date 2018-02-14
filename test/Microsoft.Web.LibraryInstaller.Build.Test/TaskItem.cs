using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Microsoft.Web.LibraryManager.Build.Test
{
    public class TaskItem : ITaskItem
    {
        public string ItemSpec { get; set; }

        public ICollection MetadataNames => throw new NotImplementedException();

        public int MetadataCount => throw new NotImplementedException();

        public IDictionary CloneCustomMetadata()
        {
            throw new NotImplementedException();
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            throw new NotImplementedException();
        }

        public string GetMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void RemoveMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
            throw new NotImplementedException();
        }
    }
}
