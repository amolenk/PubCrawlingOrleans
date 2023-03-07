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
    .ConfigureServices(services =>
    {
        services.AddHttpClient<PubCrawlService>(client =>
        {
            // TODO Get from configuration
            client.BaseAddress = new Uri("https://localhost:5001");
        });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
