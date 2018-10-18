using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace WorkspaceLoginManagement
{
    public class WebHandle
    {
        public string GetJTokenStr(string jsonStr, string jsonKey)
        {
            string result = "";
            JObject jResult = JObject.Parse(jsonStr);
            JToken jToken = jResult[jsonKey];
            result = jToken.ToString();

            return result;
        }
    }

}
