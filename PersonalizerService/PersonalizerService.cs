using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    public partial class PersonalizerService
    {
        public void Train(TrainingCase[] cases)
        {
            if (cases != null)
            {
                foreach (TrainingCase testCase in cases)
                {
                    Console.WriteLine($"{testCase.Name}:");
                    string lessonId = Guid.NewGuid().ToString();
                    var request = new RankRequest(Actions, testCase.Features, testCase.Exclude, lessonId, false);
                    RankResponse response = Client.Rank(request);
                    double reward = 0.0;
                    if (response.RewardActionId.Equals(testCase.Expected))
                    {
                        reward = 1.0;
                    }
                    Client.Reward(response.EventId, new RewardRequest(reward));
                }
            }
        }
    }
}