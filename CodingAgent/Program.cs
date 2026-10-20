using DotNetEnv;

Env.Load();

var agent = new Agent();
await agent.RunAsync();