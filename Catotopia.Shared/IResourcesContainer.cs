using Catotopia.Defs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catotopia.Shared
{
    public interface IResourcesContainer
    {
        T GetFrom<T>(JToken source, string field);
        //T GetFrom<T>(JToken token) where T : new();
        //T GetDefFrom<T>(JToken token) where T : new();
        //T GetSimpleFrom<T>(JToken token) where T : new();
        //T[] GetArrayFrom<T>(JToken token) where T : new();
        //T[,] GetArray2From<T>(JToken token) where T : new();
        //T[,,] GetArray3From<T>(JToken soutokenrce) where T : new();
    }
}
