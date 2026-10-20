
10/18/2026: Our design follows Thorsten Ball, "How to Build an Agent", except for the The list_files tool which was not implemented

added a run_command tool to execute code and tests to the coding agent.

so now we have
    read_file to read existing files.
    edit_file to create or modify files.
    run_command to execute PowerShell commands and run code or tests.

putting restrictions to the run_command function like having it be specific keywords that would trigger the function

removed the restrictions because we want YOLO

added error handling in the run_command function. 60 second timeout and empty output

removed the error handling again because we want it simple

10/20/2026: swapping to C# since we are more familiare with the framework.

Problem: 
    trouble shooting bugs 

The first version did not compile because the Anthropic C# SDK uses different types, properties, constructors and message conversions

chat gpt helped us understand the specific library API and helped us convert the types and message conversion

Problem:
    if have to give it 1 of the commands otherwise it crashes 

solved its because if we dont write a tool we want used we need to break the inner loop


