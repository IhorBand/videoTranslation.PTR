using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Reflection;
using System.Text;
using VideoTranslate.Shared.DTO.Configuration;

namespace VideoTranslate.PTR
{
    public class Program
    {
        static string RecognitionSubscriptionKey = "";
        static string RecognitionServiceRegion = "eastus";

        static readonly string TranslatorKey = "";
        static readonly string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        static readonly string TranslatorLocation = "eastus";


        static readonly string SpeechKey = "";
        static readonly string SpeechLocation = "eastus";
        static readonly string SpeechVoiceId = "uk-UA-OstapNeural";

        static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
        {
            switch (speechSynthesisResult.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Console.WriteLine($"Speech synthesized for text: [{text}]");
                    break;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
                default:
                    break;
            }
        }

        public class TranslatorResultTranslation
        {
            public string text { get; set; }
            public string to { get; set; }
        }

        public class TranslatorResultJson
        {
            public List<TranslatorResultTranslation> translations { get; set; }
        }

        async static Task Main(string[] args)
        {
            var speechConfig = SpeechConfig.FromSubscription(RecognitionSubscriptionKey, RecognitionServiceRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            Console.WriteLine("Speak into your microphone.");
            //var speechRecognitionResult = await recognizer.RecognizeOnceAsync();
            //OutputSpeechRecognitionResult(speechRecognitionResult);

            var stopRecognition = new TaskCompletionSource<int>();

            recognizer.Recognizing += (s, e) =>
            {
                Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    // Input and output languages are defined as parameters.
                    string route = "/translate?api-version=3.0&from=en&to=uk";
                    object[] body = new object[] { new { Text = e.Result.Text } };
                    var requestBody = JsonConvert.SerializeObject(body);

                    using (var client = new HttpClient())
                    using (var request = new HttpRequestMessage())
                    {
                        // Build the request.
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(TranslatorEndpoint + route);
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        request.Headers.Add("Ocp-Apim-Subscription-Key", TranslatorKey);
                        // location required if you're using a multi-service or regional (not global) resource.
                        request.Headers.Add("Ocp-Apim-Subscription-Region", TranslatorLocation);

                        // Send the request and get response.
                        HttpResponseMessage response = client.Send(request);
                        // Read response as a string.
                        Stream translatorResultStream = response.Content.ReadAsStream();

                        // convert stream to string
                        StreamReader reader = new StreamReader(translatorResultStream);
                        string translatorResult = reader.ReadToEnd();
                        var translations = JsonConvert.DeserializeObject<List<TranslatorResultJson>>(translatorResult);
                        if (translations != null && translations.Count > 0 && translations[0] != null && translations[0].translations != null && translations[0].translations.Count > 0)
                        {
                            Console.OutputEncoding = Encoding.UTF8;
                            Console.WriteLine($"Translated: {translations[0].translations[0].text}");

                            var speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechLocation);

                            // The language of the voice that speaks.
                            speechConfig.SpeechSynthesisVoiceName = SpeechVoiceId;
                            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm);

                            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
                            {
                                var speakTextTask = speechSynthesizer.SpeakTextAsync(translations[0].translations[0].text);
                                speakTextTask.Wait();
                                var speechSynthesisResult = speakTextTask.Result;

                                // Place the data into a stream
                                using (MemoryStream ms = new MemoryStream(speechSynthesisResult.AudioData))
                                {
                                    // Construct the sound player
                                    var player = new System.Media.SoundPlayer(ms);
                                    ms.Position = 0;     // Manually rewind stream 
                                    player.Stream = null;    // Then we have to set stream to null 
                                    player.Stream = ms;  // And set it again, to force it to be loaded again..
                                    try
                                    {
                                        player.Play();
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }

                                OutputSpeechSynthesisResult(speechSynthesisResult, translations[0].translations[0].text);
                            }
                        }
                    }
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            await recognizer.StartContinuousRecognitionAsync();
            // Waits for completion. Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { stopRecognition.Task });
        }



        //static async Task Main(string[] args)
        //{
        //    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        //    Console.WriteLine($"environment: {environment}");

        //    var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        //    var configurationBuilder = new ConfigurationBuilder();
        //    var configuration = BuildConfig(configurationBuilder, baseDirectory, environment);

        //    // Specifying the configuration for serilog
        //    Log.Logger = new LoggerConfiguration() // initiate the logger configuration
        //                    .ReadFrom.Configuration(configuration) // connect serilog to our configuration folder
        //                    .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog
        //                    .CreateLogger(); //initialise the logger

        //    Log.Logger.Information("Application Starting");

        //    try
        //    {
        //        // Setup Host
        //        //var host = CreateDefaultBuilder(configuration, baseDirectory, environment).Build();

        //        // Invoke Worker
        //        //using IServiceScope serviceScope = host.Services.CreateScope();
        //        //IServiceProvider provider = serviceScope.ServiceProvider;

        //        //var application = provider.GetRequiredService<Application>();
        //        //application.Start();

        //        await CreateDefaultBuilder(configuration, baseDirectory, environment)
        //            .RunConsoleAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Logger.Error(ex, "Error while Starting Application");
        //    }
        //}
        //static IHostBuilder CreateDefaultBuilder(IConfigurationRoot configuration, string? baseDirectory, string? environment)
        //{
        //    return Host.CreateDefaultBuilder()
        //        .UseSerilog()
        //        .ConfigureAppConfiguration(app =>
        //        {
        //            BuildConfig(app, baseDirectory, environment);
        //        })
        //        .ConfigureServices(services =>
        //        {
        //            var connectionStrings = configuration.GetSection("ConnectionStrings").Get<ConnectionStringConfiguration>();
        //            services.AddSingleton(connectionStrings);
        //            var rabbitMQConfiguration = configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
        //            services.AddSingleton(rabbitMQConfiguration);
        //            var azureSubscriptionConfiguration = configuration.GetSection("SpeechRecognitionAzure").Get<Models.Configuration.AzureSubscriptionConfiguration>();
        //            services.AddSingleton(azureSubscriptionConfiguration);

        //            services.AddSingleton<Application>();

        //            services.AddHostedService<HostedServices.ServiceRabbitMQ>();
        //        });
        //}
        //static IConfigurationRoot BuildConfig(IConfigurationBuilder builder, string? baseDirectory, string? environment)
        //{
        //    // Check the current directory that the application is running on 
        //    // Then once the file 'appsetting.json' is found, we are adding it.
        //    // We add env variables, which can override the configs in appsettings.json
        //    return builder.SetBasePath(Directory.GetCurrentDirectory())
        //        .SetBasePath(baseDirectory)
        //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
        //        .Build();
        //}

    }
}