// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Extensions.Hosting;
using SequentialWorkflow;

Func<string, string> orderParserFunc = input =>
{
    // We accept both short ordereId(Ex:12345) and long order reference number(MSFT12345)
    // OrderId is the last 5 digigs of order reference number.
    const int OrderIdPartLength = 5;
    if (input.Length > OrderIdPartLength)
    {
        return input[^OrderIdPartLength..];
    }

    return input;
};
var orderParserExecutor = orderParserFunc.BindAsExecutor("ParseOrderId");

OrderLookup orderLookupExecutor = new();
OrderEnrich orderEnricherExeecutor = new();
PaymentProcessor paymentProcessorExecutor = new();

Workflow fulfillOrder = new WorkflowBuilder(orderParserExecutor)
    .WithName("FulfillOrder")
    .WithDescription("Looks up an order by ID and run payment processing")
    .AddEdge(orderParserExecutor, orderLookupExecutor)
    .AddEdge(orderLookupExecutor, orderEnricherExeecutor)
    .AddEdge(orderEnricherExeecutor, paymentProcessorExecutor)
    .Build();

// Configure the function app to host the AI agent.
// This will automatically generate HTTP API endpoints for the agent.
using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableWorkflows(options => options.AddWorkflow(fulfillOrder))
    //  .ConfigureDurableAgents(options => options.AddAIAgent(agent, timeToLive: TimeSpan.FromHours(1)))
    .Build();
app.Run();
