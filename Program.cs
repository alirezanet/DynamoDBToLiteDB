using CliFx;

var exitCode = await new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    // .UseTypeActivator(BuildServiceProvider().GetService)
    .SetExecutableName("dynamoToLiteDB")
    .Build()
    .RunAsync(args);


return exitCode;