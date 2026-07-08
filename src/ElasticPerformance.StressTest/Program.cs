using ElasticPerformance.Infrastructure;
using ElasticPerformance.StressTest;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<StressTestConfig>(builder.Configuration.GetSection("StressTestConfig"));
builder.Services.AddHostedService<StressWorker>();

var host = builder.Build();
host.Run();
