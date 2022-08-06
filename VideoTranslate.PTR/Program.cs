using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;
using VideoTranslate.Shared.DTO.Configuration;

namespace VideoTranslate.PTR
{
    public class Program
    {
        static string YourSubscriptionKey = "b7998ec252ab4daf95f9e70f81308837";
        static string YourServiceRegion = "eastus";

        async static Task Main(string[] args)
        {
            var speechConfig = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
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