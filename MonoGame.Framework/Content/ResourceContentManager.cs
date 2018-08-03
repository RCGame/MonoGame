using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;

namespace Microsoft.Xna.Framework.Content
{
    public class ResourceContentManager : ContentManager
    {
        private ResourceManager resource;

        public ResourceContentManager(IServiceProvider servicesProvider, ResourceManager resource)
            : base(servicesProvider)
        {
            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }
            this.resource = resource;
        }

  
    }
}
