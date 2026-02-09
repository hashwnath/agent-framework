// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.DurableTask;
using Microsoft.Agents.AI.DurableTask.Workflows;
using Microsoft.Agents.AI.Hosting.AzureFunctions.Workflows;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Agents.AI.Hosting.AzureFunctions;

/// <summary>
/// Extension methods for the <see cref="FunctionsApplicationBuilder"/> class.
/// </summary>
public static class FunctionsApplicationBuilderExtensions
{
    /// <summary>
    /// Configures durable workflow services for the application and allows customization of durable workflow options.
    /// </summary>
    /// <remarks>This method registers the services required for durable workflows using
    /// Microsoft.DurableTask.Workflows. Call this method during application startup to enable durable workflows in your
    /// Azure Functions app.</remarks>
    /// <param name="builder">The application builder used to configure services and middleware for the Azure Functions app.</param>
    /// <param name="configure">A delegate that is used to configure the durable workflow options. Cannot be null.</param>
    /// <returns>The same <see cref="FunctionsApplicationBuilder"/> instance that this method was called on, to support method
    /// chaining.</returns>
    public static FunctionsApplicationBuilder ConfigureDurableWorkflows(this FunctionsApplicationBuilder builder, Action<DurableWorkflowOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddSingleton<IFunctionMetadataTransformer, DurableWorkflowFunctionMetadataTransformer>();

        // The main durable workflows services registration is done in Microsoft.DurableTask.Workflows.
        builder.Services.ConfigureDurableWorkflows(configure);
        return builder;
    }

    /// <summary>
    /// Configures the application to use durable agents with a builder pattern.
    /// </summary>
    /// <param name="builder">The functions application builder.</param>
    /// <param name="configure">A delegate to configure the durable agents.</param>
    /// <returns>The functions application builder.</returns>
    public static FunctionsApplicationBuilder ConfigureDurableAgents(
        this FunctionsApplicationBuilder builder,
        Action<DurableAgentsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // The main agent services registration is done in Microsoft.DurableTask.Agents.
        builder.Services.ConfigureDurableAgents(configure);

        builder.Services.TryAddSingleton<IFunctionsAgentOptionsProvider>(_ =>
            new DefaultFunctionsAgentOptionsProvider(DurableAgentsOptionsExtensions.GetAgentOptionsSnapshot()));

        builder.Services.AddSingleton<IFunctionMetadataTransformer, DurableAgentFunctionMetadataTransformer>();

        // Handling of built-in function execution for Agent HTTP, MCP tool, or Entity invocations.
        builder.UseWhen<BuiltInFunctionExecutionMiddleware>(static context =>
            string.Equals(context.FunctionDefinition.EntryPoint, BuiltInFunctions.RunAgentHttpFunctionEntryPoint, StringComparison.Ordinal) ||
            string.Equals(context.FunctionDefinition.EntryPoint, BuiltInFunctions.RunAgentMcpToolFunctionEntryPoint, StringComparison.Ordinal) ||
            string.Equals(context.FunctionDefinition.EntryPoint, BuiltInFunctions.RunAgentEntityFunctionEntryPoint, StringComparison.Ordinal));
        builder.Services.AddSingleton<BuiltInFunctionExecutor>();

        return builder;
    }
}
