using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

using AAI;

namespace Personalizer
{
    [TestClass]
    public class Tests
    {
        private static PersonalizerService Personalizer;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            Personalizer = new PersonalizerService(
                GetConfigString(config, "PersonalizerEndpointKey"),
                GetConfigString(config, "PersonalizerResourceName"));
        }


        [TestMethod]
        public void TestClient()
        {
            IList<RankableAction> actions = new List<RankableAction>
            {
                new RankableAction
                {
                    Id = "pasta",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "medium" }, new { nutritionLevel = 5, cuisine = "italian" } }
                },

                new RankableAction
                {
                    Id = "salad",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "low" }, new { nutritionLevel = 8 } }
                }
            };

            string id = Guid.NewGuid().ToString();
            IList<object> currentContext = new List<object>() {
                new { time = "morning" },
                new { taste = "salty" }
            };

            IList<string> exclude = new List<string> { "pasta" };
            var request = new RankRequest(actions, currentContext, exclude, id);
            RankResponse resp = Personalizer.Client.Rank(request);
            Assert.AreEqual("salad", resp.RewardActionId);
        }

        private static string GetConfigString(IConfiguration config, string key)
        {
            string result = config[key];
            if (result != null && result.Length == 0)
            {
                result = null;
            }
            return result;
        }
    }
}
