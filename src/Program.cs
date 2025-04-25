using CliFx;

var exitCode = await new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    // .UseTypeActivator(BuildServiceProvider().GetService)
    .SetExecutableName("DynamoDBToLiteDB")
    .Build()
    .RunAsync(args);


return exitCode;