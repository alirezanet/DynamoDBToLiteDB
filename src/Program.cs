using System.Text;
using CliFx;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.InputEncoding  = Encoding.UTF8;

var exitCode = await new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    // .UseTypeActivator(BuildServiceProvider().GetService)
    .SetExecutableName("DynamoDBToLiteDB")
    .Build()
    .RunAsync(args);


return exitCode;