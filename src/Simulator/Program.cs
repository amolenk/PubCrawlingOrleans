IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddCommandLine(args);
    })
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering();
        siloBuilder.AddMemoryGrainStorageAsDefault();
        siloBuilder.UseInMemoryReminderService();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient<PubCrawlService>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration["PubCrawlApiAddress"]
                ?? "https://localhost:5001");
        });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
