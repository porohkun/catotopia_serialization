using Catotopia.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catotopia.Defs
{
    public interface IDef
    {
        void Fill(JToken source, IResourcesContainer resources);
    }
}
