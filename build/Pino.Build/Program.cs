using Pino.Build;

if (args.Length != 5)
{
    await Console.Error.WriteLineAsync("Expected: project-directory root-namespace define-constants components-manifest compile-manifest");
    return 2;
}

var validator = new ValidateRazorCodeBehind
{
    ProjectDirectory = args[0],
    RootNamespace = args[1],
    DefineConstants = args[2],
    Components = [.. (await File.ReadAllLinesAsync(args[3])).Where(line => !string.IsNullOrWhiteSpace(line))],
    CompileFiles = [.. (await File.ReadAllLinesAsync(args[4])).Where(line => !string.IsNullOrWhiteSpace(line))],
};

return validator.Execute() ? 0 : 1;
