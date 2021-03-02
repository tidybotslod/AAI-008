using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    public static class Extensions
    {
        public static string Last(this string source, int numberOfChars = 1)
        {
            if (source == null || numberOfChars > source.Length)
            {
                return "";
            }
            return source.Substring(source.Length - numberOfChars);
        }
    }


    public partial class PersonalizerService
    {
        private string personalizerEndpointKey;
        private string personalizerResourceName;
        private PersonalizationFeature[] features;
        private PersonalizerClient client;

        public PersonalizerService(string endpointKey, string endpointResourceName)
        {
            personalizerEndpointKey = endpointKey;
            personalizerResourceName = endpointResourceName;
        }

        public List<RankableAction> Actions { get; set; }


        public PersonalizationFeature[] Features
        {
            get
            {
                return features;
            }
            set
            {
                Dictionary<string, PersonalizationFeature> lookup = new Dictionary<string, PersonalizationFeature>();
                if (value != null)
                {
                    foreach (PersonalizationFeature entry in value)
                    {
                        lookup.Add(entry.Name, entry);
                    }
                }
                features = value;
                Lookup = lookup;
            }
        }

        public PersonalizerClient Client
        {
            get
            {
                return client ?? CreatePersonalizer();
            }
        }


       public void InteractiveTraining(string[] select, string[] ignore)
        {
            if (Actions == null || Actions.Count == 0)
            {
                Console.WriteLine("Nothing to select.");
                return;
            }

            if (select == null || select.Length == 0)
            {
                Console.WriteLine("No features selected.");
                return;
            }

            int lessonCount = 1;
            do
            {
                Console.WriteLine($"Lesson {lessonCount}");

                // Build context list by creating a JSON string and then convert it to a list of objects.
                string[] answers = new string[select.Length];
                for (int i = 0; i < select.Length; i++)
                {
                    answers[i] = SelectFeatureInteractively(select[i]);
                    if (answers[i] == "Q")
                    {
                        // When null is returned the training session is over.
                        return;
                    }
                }
                IList<Object> contextFeatures = FeatureList(select, answers);

                // Create an id for this lesson, used when setting the reward.
                string lessonId = Guid.NewGuid().ToString();

                // Create a list of Personalizer.Actions that should be excluded from the ranking
                List<string> excludeActions = null;
                if (ignore != null && ignore.Length > 0)
                {
                    excludeActions = new List<string>(ignore);
                }

                // Create the rank requestr
                var request = new RankRequest(Actions, contextFeatures, excludeActions, lessonId, false);
                RankResponse response = null;
                response = Client.Rank(request);
                //response = new RankResponse();
                Console.WriteLine($"Personalizer service thinks you would like to have: {response.RewardActionId}. Is this correct (y/n)?");

                string answer = GetKey();
                Console.WriteLine();
                double reward = 0.0;
                if (answer == "Y")
                {
                    reward = 1.0;
                    Client.Reward(response.EventId, new RewardRequest(reward));
                    Console.WriteLine($"Set reward: {reward}");
                }
                else if (answer == "N")
                {
                    Client.Reward(response.EventId, new RewardRequest(reward));
                    Console.WriteLine($"Set reward: {reward}");
                }
                else
                {
                    Console.WriteLine("Entered choice is invalid. Not setting reward.");
                }
            } while (true);
        }

        public Dictionary<string, PersonalizationFeature> Lookup { get; private set; }

        private PersonalizerClient CreatePersonalizer()
        {
            string Endpoint = $"https://{personalizerResourceName}.cognitiveservices.azure.com";
            client = new PersonalizerClient(
             new ApiKeyServiceClientCredentials(personalizerEndpointKey)) { Endpoint = Endpoint };
            return client;
        }


        public PersonalizationFeature LookupFeature(string id)
        {
            PersonalizationFeature result = null;
            if(Lookup != null)
            {
                result = Lookup[id];
            }
            return result;
        }

        public IList<object> FeatureList(string[] select, string[] answers)
        {
            if ((select == null || select.Length == 0) ||
                (answers == null || answers.Length == 0) ||
                (answers.Length != select.Length))
            {
                return null;
            }

            PersonalizationFeature feature = LookupFeature(select[0]);
            StringBuilder contextFeaturesJson = new StringBuilder("[");
            contextFeaturesJson.Append($"{{ \"{feature.Name}\": \"{answers[0]}\" }}");
            for (int i = 1; i < select.Length; i++)
            {
                feature = LookupFeature(select[i]);
                contextFeaturesJson.Append($",{{ \"{feature.Name}\": \"{answers[i]}\" }}");
            }
            contextFeaturesJson.Append("]");
            return JsonSerializer.Deserialize<List<object>>(contextFeaturesJson.ToString());
        }

        public string SelectFeatureInteractively(string name)
        {
            PersonalizationFeature feature = LookupFeature(name);
            if (feature == null)
            {
                return null;
            }

            do
            {
                Console.WriteLine(feature.InteractivePrompt);
                string entry = GetKey();
                Console.WriteLine();
                if (!int.TryParse(entry, out int index) || index < 1 || index > feature.Values.Length)
                {
                    if (entry.Length > 0 && entry[0] == 'Q')
                    {
                        return "Q";
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection!\n");
                    }
                }
                else
                {

                    return feature.Values[index - 1];
                }
            } while (true);
        }
        public string GetKey()
        {
            return Console.ReadKey().Key.ToString().Last().ToUpper();
        }


    }

}


