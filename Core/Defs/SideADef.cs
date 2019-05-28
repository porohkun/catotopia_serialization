using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Catotopia
{
    namespace Defs
    {
        public partial class SideADef : SideDef
        {
            [JsonIgnore]
            public string StringProp => _stringField;

            [JsonProperty]
            private string _stringField;
        }
    }
}