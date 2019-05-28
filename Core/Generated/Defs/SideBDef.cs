using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Catotopia.Shared;

namespace Catotopia.Defs
{
    public partial class SideBDef
    {
        public override void Fill(JToken source, IResourcesContainer resources)
        {
            base.Fill(source, resources);
            _stringField = resources.GetFrom<string>(source, "stringField");
        }
    }
}