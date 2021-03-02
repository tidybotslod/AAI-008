﻿using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

using AAI;

namespace Personalize
{


    class Program
    {
        private IConfiguration config;
        private PersonalizerService Personalizer;

        public Program()
        {
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            Personalizer = new PersonalizerService(
                GetConfigString("PersonalizerEndpointKey"),
                GetConfigString("PersonalizerResourceName")
            );
        }

        public void LoadFeatures(string featureFile)
        {

            string input = File.ReadAllText(featureFile);
            if (input != null && input.Length > 0)
            {
                Personalizer.Features = JsonSerializer.Deserialize<PersonalizationFeature[]>(input);
            }
        }

        public void LoadActions(string actionFile)
        {
            string input = File.ReadAllText(actionFile);
            if (input != null && input.Length > 0)
            {
                Personalizer.Actions = JsonSerializer.Deserialize<List<RankableAction>>(input);
            }
        }

        public void InteractiveTraining(string[] select, string[] ignore)
        {
            Personalizer.InteractiveTraining(select, ignore);
        }

        public void TrainingFile(string trainingFile)
        {
            string input = File.ReadAllText(trainingFile);
            if (input != null && input.Length > 0)
            {
                PersonalizerTraining[] trainingData = JsonSerializer.Deserialize<PersonalizerTraining[]>(input);
                Personalizer.TrainingFile(trainingData);
            }
        }

        private string GetConfigString(string key)
        {
            string result = config[key];
            if (result != null && result.Length == 0)
            {
                result = null;
            }
            return result;
        }

        static void Main(string[] args)
        {
            Program program = new Program();

            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new string[] { "-a", "--actions"},
                    description: "Load Personalizer.Actions from JSON file"),
                new Option<string>(
                    new string [] { "-f", "--features" },
                    description: "Load features for training"),
                new Option<string[]>(
                    new string [] { "-e", "--exclude" },
                    description: "Exclude one or more actons from being selected"),
                new Option<string[]>(
                    new string [] { "-s", "--select" },
                    description:"Select one or more features to ask for"),
                new Option<string>(
                    new string [] { "-t", "--training"},
                    description: "Training file (disables interactive training")
            };


            rootCommand.Description = "Manage a Question and Answer knowledge base hosted in Azure.";

            rootCommand.Handler = CommandHandler.Create<string, string, string[], string[], string>((actions, features, exclude, select, training) =>
            {
                if (actions != null)
                {
                    program.LoadActions(actions);
                }

                if (features != null)
                {
                    program.LoadFeatures(features);
                }

                if(training != null)
                {
                    program.TrainingFile(training);
                }
                else
                {
                    program.InteractiveTraining(select, exclude);
                }

            });
            rootCommand.Invoke(args);
        }
    }
}
