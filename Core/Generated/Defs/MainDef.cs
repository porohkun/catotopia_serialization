using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catotopia.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catotopia.Defs
{
    public partial class MainDef
    {
        public override void Fill(JToken source, IResourcesContainer resources)
        {
            base.Fill(source, resources);
            _intField = resources.GetFrom<System.Int32>(source, "intField");
            StringProp1 = resources.GetFrom<string>(source, "stringProp1");
            _stringField = resources.GetFrom<string>(source, "stringProp_2");
            _ints = resources.GetFrom<int[, ]>(source, "ints");
            _sides = resources.GetFrom<SideDef[]>(source, "sides");
        }
    }
}