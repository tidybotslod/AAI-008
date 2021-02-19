using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    public partial class PersonalizerService
    {
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
                    if (answers[i] == null)
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

        public void TrainingFile(PersonalizerTraining[] cases)
        {
            if (cases != null)
            {
                foreach (PersonalizerTraining trial in cases)
                {
                    Console.WriteLine($"{trial.Name}:");
                    string lessonId = Guid.NewGuid().ToString();
                    var request = new RankRequest(Actions, trial.Features, trial.Exclude, lessonId, false);
                    RankResponse response = Client.Rank(request);
                    double reward = 0.0;
                    if (response.RewardActionId.Equals(trial.Expected))
                    {
                        reward = 1.0;
                    }
                    Client.Reward(response.EventId, new RewardRequest(reward));
                }
            }
        }
    }
}