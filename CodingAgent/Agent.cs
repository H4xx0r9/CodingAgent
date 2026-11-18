using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;

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
            "Create or edit a text file. Always provide path, old_str and new_str. Use an empty old_str to create a new file.",
            EditFileSchema(), EditFile));
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

        if (string.IsNullOrWhiteSpace(input))
            continue;

        conversation.Add(new MessageParam
        {
            Role = Role.User,
            Content = input
        });

        bool version2 = false;

        var filesForReview = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var generatedOutput = new StringBuilder();

        while (true)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = model_name,
                    MaxTokens = 4096,
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
            var pendingTools = new List<(string ID, string Name, string Input, ToolDefinition Tool)>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    Console.WriteLine($"\u001b[93mClaude\u001b[0m: {text.Text}");
                    generatedOutput.AppendLine(text.Text);

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

                    Console.WriteLine($"\u001b[92mtool\u001b[0m: {toolUse.Name}");

                    var tool = _tools.FirstOrDefault(t => t.Name == toolUse.Name);

                    if (tool == null)
                    {
                        toolResults.Add(new ToolResultBlockParam
                        {
                            ToolUseID = toolUse.ID,
                            Content = $"Unknown tool: {toolUse.Name}",
                            IsError = true
                        });

                        continue;
                    }

                    if (tool.Name == "read_file")
                    {
                        try
                        {
                            var json = JsonSerializer.Serialize(toolUse.Input);
                            var result = await tool.Execute(json);

                            toolResults.Add(new ToolResultBlockParam
                            {
                                ToolUseID = toolUse.ID,
                                Content = result
                            });
                        }
                        catch (Exception ex)
                        {
                            toolResults.Add(new ToolResultBlockParam
                            {
                                ToolUseID = toolUse.ID,
                                Content = ex.Message,
                                IsError = true
                            });
                        }
                    }
                    else
                    {
                        pendingTools.Add((
                            toolUse.ID,
                            toolUse.Name,
                            JsonSerializer.Serialize(toolUse.Input),
                            tool
                        ));
                    }
                }
            }

            if (pendingTools.Count > 0)
            {
                Console.WriteLine("\nClaude wants to execute:");

                for (int i = 0; i < pendingTools.Count; i++)
                {
                    var item = pendingTools[i];

                    Console.WriteLine($"\n{i + 1}. {item.Name}");
                    Console.WriteLine(item.Input);
                }

                Console.Write("\nAllow all operations (y/n): ");
                var approval = Console.ReadLine();

                foreach (var item in pendingTools)
                {
                    string result;
                    bool failed = false;

                    if (approval?.Trim().ToLower() == "y")
                    {
                        try
                        {
                            result = await item.Tool.Execute(item.Input);

                            if (item.Name == "edit_file")
                            {
                                var fileInput = JsonNode.Parse(item.Input);
                                var filePath = fileInput?["path"]?.GetValue<string>();

                                if (!string.IsNullOrWhiteSpace(filePath))
                                    filesForReview.Add(Path.GetFullPath(filePath));
                            }
                        }
                        catch (Exception ex)
                        {
                            result = ex.Message;
                            failed = true;
                        }
                    }
                    else
                    {
                        result = "User denied permission to execute this tool.";
                        failed = true;
                    }

                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = item.ID,
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

            if (toolResults.Count > 0)
            {
                conversation.Add(new MessageParam
                {
                    Role = Role.User,
                    Content = toolResults
                });

                continue;
            }

            // Collect files for Corti review
            Console.WriteLine("\nCollecting files for Corti review...");

            var codeContext = new StringBuilder();

            foreach (var path in filesForReview)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(path);
                    var extension = Path.GetExtension(path).TrimStart('.');
                    var relativePath = Path.GetRelativePath(
                        Directory.GetCurrentDirectory(), path)
                        .Replace('\\', '/');

                    codeContext.AppendLine($"File: {relativePath}");
                    codeContext.AppendLine($"```{extension}");
                    codeContext.AppendLine(content);
                    codeContext.AppendLine("```");
                    codeContext.AppendLine();
                }
                catch (Exception ex)
                {
                    codeContext.AppendLine(
                        $"Could not read {path}: {ex.Message}");
                }
            }

            // Corti reviews Version 1
            if (!version2)
            {
                Console.WriteLine("\nCorti is reviewing Version 1...");

                var review = await CheckOutput(
                    input,
                    generatedOutput.ToString(),
                    codeContext.ToString(),
                    filesForReview);

                Console.WriteLine("\n--- Corti Version 1 Review ---");
                Console.WriteLine(review);

                conversation.Add(new MessageParam
                {
                    Role = Role.User,
                    Content =
                        "Corti has reviewed your Version 1. Use its feedback below " +
                        "to improve your solution. Create VERSION 2 and clearly label it " +
                        "as VERSION 2. Address the identified problems and incorporate " +
                        "the improvement suggestions.\n\n" +
                        "Corti's review:\n" + review
                });

                // Prepare to track Version 2 files separately
                filesForReview.Clear();
                generatedOutput.Clear();
                version2 = true;

                Console.WriteLine("\n--- Asking Claude for Version 2 ---");

                continue;
            }

            // Corti reviews Version 2
            Console.WriteLine("\nCorti is reviewing Version 2...");

            var version2Review = await CheckOutput(
                input,
                generatedOutput.ToString(),
                codeContext.ToString(),
                filesForReview);

            Console.WriteLine("\n--- Corti Version 2 Review ---");
            Console.WriteLine(version2Review);

            Console.WriteLine("\n--- Both versions reviewed. Workflow finished. ---");

            break;
        }
    }
}



private async Task<string> CheckOutput(
    string prompt,
    string output,
    string codeContext,
    HashSet<string> filesForReview)
{
    var client = new ChatClient(
        "corti-s1-mini",
        new ApiKeyCredential(
            Environment.GetEnvironmentVariable("CORTI_API_KEY")!),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(
                Environment.GetEnvironmentVariable("CORTI_BASE_URL")!)
        });

    var root = Directory.GetCurrentDirectory();

    var fileList = filesForReview.Select(path =>
        Path.GetRelativePath(root, Path.GetFullPath(path))
            .Replace('\\', '/'));

    var response = await client.CompleteChatAsync(
    [
        
        new SystemChatMessage(
            """
            Act as a senior software engineer reviewing code.

            Review each provided source file and assign it a quality
            score from 1 to 100.

            Evaluate correctness, readability, maintainability and
            how well it fulfils the original task.

            Return ONLY valid JSON in this structure:

            {
              "files": [
                {
                  "path": "Program.cs",
                  "score": 85,
                  "strengths": ["Short strength"],
                  "weaknesses": ["Short weakness"],
                  "suggestions": ["Short suggestion"]
                }
              ]
            }

            Rules:
            - Score every file.
            - Use integer scores from 1 to 100.
            - Maximum 2 strengths, 2 weaknesses and 2 suggestions per file.
            - Keep each point to one short sentence.
            - Maximum 80 words per file.
            - Do not repeat information.
            - Do not include markdown or text outside the JSON.
            """),

        new UserChatMessage(
            $"Original task:\n{prompt}\n\n" +
            $"Claude's response:\n{output}\n\n" +
            $"Files to score:\n{string.Join("\n", fileList)}\n\n" +
            $"Actual source files:\n{codeContext}")
    ]);

    var reviewText = response.Value.Content[0].Text;

    var review = JsonNode.Parse(reviewText) as JsonObject
        ?? throw new InvalidOperationException(
            "Corti returned invalid review JSON.");

    var reviewedFiles = review["files"] as JsonArray
        ?? throw new InvalidOperationException(
            "Corti did not return a files array.");

    var validationPath = Path.Combine(root, "validation.json");

    var validation = File.Exists(validationPath)
        ? JsonNode.Parse(await File.ReadAllTextAsync(validationPath))
            as JsonObject ?? new JsonObject()
        : new JsonObject();

    foreach (var file in reviewedFiles)
    {
        var path = file?["path"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Missing file path.");

        var score = file?["score"]?.GetValue<int>()
            ?? throw new InvalidOperationException("Missing file score.");

        if (score < 1 || score > 100)
            throw new InvalidOperationException("Invalid score from Corti.");

        var fullPath = Path.GetFullPath(Path.Combine(root, path));

        if (!filesForReview.Any(f =>
            string.Equals(
                Path.GetFullPath(f),
                fullPath,
                StringComparison.OrdinalIgnoreCase)))
            continue;

        var relativePath = Path.GetRelativePath(root, fullPath)
            .Replace('\\', '/');

        if (validation[relativePath] is not JsonObject entry)
        {
            entry = new JsonObject();
            validation[relativePath] = entry;
        }

        entry["score"] = score;
        entry["reviewer"] = "Corti";
        entry["lastReviewed"] = DateTime.UtcNow.ToString("o");
    }

    await File.WriteAllTextAsync(
        validationPath,
        validation.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        }));

    return review.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = true
    });
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


    private static async Task<string> EditFile(string json)
    {
        var input = JsonNode.Parse(json)!;
        var path = input["path"]!.GetValue<string>();
        var oldText = input["old_str"]?.GetValue<string>() ?? "";
        var newText = input["new_str"]?.GetValue<string>()
                      ?? throw new ArgumentException("Missing required argument: new_str");

        string result;

        if (!File.Exists(path))
        {
            if (oldText != "")
                throw new FileNotFoundException(path);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, newText);
            result = $"Successfully created file {path}";
        }
        else
        {
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
            result = "OK";
        }

        await MarkFileUnverified(path);

        return result;
    }
    
    
    private static async Task MarkFileUnverified(string path)
    {
        var root = Directory.GetCurrentDirectory();
        var validationPath = Path.Combine(root, "validation.json");

        var relativePath = Path.GetRelativePath(
            root, Path.GetFullPath(path)).Replace('\\', '/');

        JsonObject validation;

        if (File.Exists(validationPath))
        {
            validation = (JsonObject)(JsonNode.Parse(
                await File.ReadAllTextAsync(validationPath)) ?? new JsonObject());
        }
        else
        {
            validation = new JsonObject();
        }

        validation[relativePath] = new JsonObject
        {
            ["score"] = null,
            ["reviewer"] = null,
            ["lastReviewed"] = null
        };

        await File.WriteAllTextAsync(
            validationPath,
            validation.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
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