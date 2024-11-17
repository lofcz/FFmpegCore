using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FFmpegCore.Tests.Fixtures
{
    public class BaseFixture
    {
        public BaseFixture()
        {
            using (StreamReader file = File.OpenText("Properties\\launchSettings.json"))
            {
                JsonTextReader reader = new JsonTextReader(file);
                JObject jObject = JObject.Load(reader);

                List<JProperty> variables = jObject
                    .GetValue("profiles")
                    .SelectMany(profiles => profiles.Children())
                    .SelectMany(profile => profile.Children<JProperty>())
                    .Where(prop => prop.Name == "environmentVariables")
                    .SelectMany(prop => prop.Value.Children<JProperty>())
                    .ToList();

                foreach (JProperty variable in variables)
                {
                    Environment.SetEnvironmentVariable(variable.Name, variable.Value.ToString());
                }
            }
        }

        public string FFmpegPath => Environment.GetEnvironmentVariable("FFMPEG");
    }
}
