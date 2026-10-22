
10/18/2026: Our design follows Thorsten Ball, "How to Build an Agent", except for the The list_files tool which was not implemented

added a run_command tool to execute code and tests to the coding agent.

so now we have
    read_file to read existing files.
    delete_file to delete existing files but only from inside the current directory.
    edit_file to create or modify files.
    run_command to execute PowerShell commands and run code or tests.

putting restrictions to the run_command or the delete_file function like having it be specific keywords that would trigger the function

removed the restrictions because we want YOLO

added error handling in the run_command function. 60 second timeout and empty output

removed the error handling again because we want it simple

10/20/2026: swapping to C# since we are more familiare with the framework.

Problem: 
    trouble shooting bugs 

The first version did not compile because the Anthropic C# SDK uses different types, properties, constructors and message conversions

chat gpt helped us understand the specific library API and helped us convert the types and message conversion

Problem:
    if none of the commands are prompted it crashes 

solved its because if we dont write a tool we want used we need to break the inner loop

thinking about adding permissions to claude???


### log tokens functionality
We added a functionality that logs the tokens used per claude model execution. An example:


2026-10-21 14:23:45 | Model: claude-haiku-5-5 | Duration: 1.14s | Input: 682 | Output: 40 | Total: 722 | Cost: $0.000088


Right now we have added token to dollar conversions based on only 3 models, which can be seen on the table below:

| Model | Input / 1M tokens | Output / 1M tokens |
|---|---:|---|
| claude-fable-5-1 | $10.00 | $50.00 |
| claude-sonnet-5-5 | $2.00 | $10.00 | 
| claude-haiku-5-5 | $0.10 | $0.50 |

To build this we used anthropic functionalities to keep track of the tokens used, we check the conversion rates for each model online, and we create a list that for each model name it maps the correct conversions. Then for the duration we used a start and stop function when executing the anthropic response. Finally, we created a function that takes the total input and output tokens, the duration and the models name as parameters and adds a line with all of them in a tokens.log file.

### Permissions
We added permissions to prevent the agent from making unwanted changes or executing powershell commands without the users knowledge. We chose to allow reading files without approval because it does not modify the project.
