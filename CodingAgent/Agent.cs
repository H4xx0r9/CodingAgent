using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using System.IO;

public class Agent
{
    private readonly AnthropicClient _client;
    private readonly List<ToolDefinition> _tools = new();

    private static readonly Dictionary<string, (double Input, double Output)> ModelRates = new()
    {
        ["claude-fable-5-1"] = (10.0, 50.0),
        ["claude-sonnet-5-5"] = (2.0, 10.0),
        ["claude-haiku-5-5"] = (0.10, 0.50)
    };

    public Agent()
    {
        _client = new AnthropicClient();
        _tools.Add(new ToolDefinition("read_file",
            "Read the contents of a file.",
            ReadFileSchema(), ReadFile));
        _tools.Add(new ToolDefinition("edit_file",
            "Create or edit a text file by replacing old_str with new_str. Use an empty old_str to create a new file.",
            EditFileSchema(), EditFile));
        _tools.Add(new ToolDefinition(
        "delete_file",
        "Delete a file at the specified path.",
        DeleteFileSchema(),
        DeleteFile));
        _tools.Add(new ToolDefinition("run_command",
            "Execute any PowerShell command in the current project directory.",
            RunCommandSchema(), RunCommand));
    }


public async Task RunAsync()
{
    var conversation = new List<MessageParam>();
    string model_name = "claude-haiku-5-5";

    Console.WriteLine("Chat with Claude (Ctrl+C to quit)");

    while (true)
    {
        Console.Write("\u001b[94mYou\u001b[0m: ");
        var input = Console.ReadLine();

        conversation.Add(new MessageParam
        {
            Role = Role.User,
            Content = input
        });

        while (true)
        {   
            
            var stopwatch = Stopwatch.StartNew();
            
            var response = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = model_name,
                    MaxTokens = 1024,
                    Messages = conversation,
                    Tools = _tools.Select(t => new ToolUnion(
                        new Tool
                        {
                            Name = t.Name,
                            Description = t.Description,
                            InputSchema = t.Schema
                        }
                    )).ToList()
                });

            stopwatch.Stop();

            LogTokenUsage(
                model_name,
                response.Usage.InputTokens,
                response.Usage.OutputTokens,
                stopwatch.Elapsed.TotalSeconds
            );

            var assistantContent = new List<ContentBlockParam>();
            var toolResults = new List<ContentBlockParam>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    Console.WriteLine($"\u001b[93mClaude\u001b[0m: {text.Text}");

                    assistantContent.Add(new TextBlockParam
                    {
                        Text = text.Text
                    });
                }
                else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    assistantContent.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input
                    });

                    Console.WriteLine(
                        $"\u001b[92mtool\u001b[0m: {toolUse.Name}");

                    var tool = _tools.FirstOrDefault(
                        t => t.Name == toolUse.Name);

                    string result;
                    bool failed = false;
                    
                    try
                    {
                        result = await tool.Execute(
                            JsonSerializer.Serialize(toolUse.Input));
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        failed = true;
                    }
                    
                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = toolUse.ID,
                        Content = result,
                        IsError = failed
                    });
                }
            }

            conversation.Add(new MessageParam
            {
                Role = Role.Assistant,
                Content = assistantContent
            });
            
            if (toolResults.Count == 0)
                break;
            
            conversation.Add(new MessageParam
            {
                Role = Role.User,
                Content = toolResults
            });
        }
    }
}
    private static void LogTokenUsage(
        string model,
        long inputTokens,
        long outputTokens,
        double durationSeconds)
    {
        var rates = ModelRates[model];

        long totalTokens = inputTokens + outputTokens;

        double inputCost = inputTokens * rates.Input / 1_000_000;
        double outputCost = outputTokens * rates.Output / 1_000_000;
        double totalCost = inputCost + outputCost;

        string log = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} | " +
                    $"Model: {model} | " +
                    $"Duration: {durationSeconds:F2}s | " +
                    $"Input: {inputTokens} | " +
                    $"Output: {outputTokens} | " +
                    $"Total: {totalTokens} | " +
                    $"Cost: ${totalCost:F6}";

        File.AppendAllText("tokens.log", log + Environment.NewLine);
    }
    private static async Task<string> ReadFile(string json)
    {
        var input = JsonNode.Parse(json)!;
        return await File.ReadAllTextAsync(input["path"]!.GetValue<string>());
    }

    private static Task<string> DeleteFile(string json)
        {
        var input = JsonNode.Parse(json)!;
        var path = input["path"]!.GetValue<string>();

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        if (!path.StartsWith(".") || path.Contains(".."))
        {
            throw new UnauthorizedAccessException("Only files in the current directory are allowed.");
        }

        File.Delete(path);

        return Task.FromResult($"Successfully deleted {path}");
    }

    private static async Task<string> EditFile(string json)
    {
        var input = JsonNode.Parse(json)!;
        var path = input["path"]!.GetValue<string>();
        var oldText = input["old_str"]?.GetValue<string>() ?? "";
        var newText = input["new_str"]!.GetValue<string>();

        if (!File.Exists(path))
        {
            if (oldText != "")
                throw new FileNotFoundException(path);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, newText);
            return $"Successfully created file {path}";
        }

        var content = await File.ReadAllTextAsync(path);

        if (oldText != "")
        {
            if (!content.Contains(oldText))
                throw new InvalidOperationException("old_str not found in file");

            content = content.Replace(oldText, newText);
        }
        else
        {
            content = newText;
        }

        await File.WriteAllTextAsync(path, content);
        return "OK";
    }

    private static async Task<string> RunCommand(string json)
    {
        var input = JsonNode.Parse(json)!;
        var command = input["command"]!.GetValue<string>();

        using var process = new Process();
        process.StartInfo.FileName = "powershell.exe";
        process.StartInfo.Arguments = "-NoProfile -Command " + command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return stdout + stderr;
    }
    private static InputSchema ReadFileSchema() => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["path"] = JsonSerializer.SerializeToElement(
                new { type = "string", description = "File path" })
        },
        Required = ["path"]
    };

    private static InputSchema EditFileSchema() => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["path"] = JsonSerializer.SerializeToElement(new { type = "string" }),
            ["old_str"] = JsonSerializer.SerializeToElement(new { type = "string" }),
            ["new_str"] = JsonSerializer.SerializeToElement(new { type = "string" })
        },
        Required = ["path", "old_str", "new_str"]
    };

    private static InputSchema DeleteFileSchema() => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["path"] = JsonSerializer.SerializeToElement(
                new { type = "string", description = "Path of the file to delete" })
        },
        Required = ["path"]
    };

    private static InputSchema RunCommandSchema() => new()
    {
        Properties = new Dictionary<string, JsonElement>
        {
            ["command"] = JsonSerializer.SerializeToElement(
                new { type = "string", description = "PowerShell command" })
        },
        Required = ["command"]
    };

    private sealed record ToolDefinition(
        string Name,
        string Description,
        InputSchema Schema,
        Func<string, Task<string>> Execute);
}