using System.CommandLine;
using KrnlAI.Cli.Services;
using KrnlAI.Cognition.Runtime;
using KrnlAI.Contracts;
using KrnlAI.Core.Abstractions;

namespace KrnlAI.Cli.Commands;

public sealed class PlanCommand(CliContext ctx, ConsoleRenderer renderer)
{
    public Command Build()
    {
        var cmd = new Command("plan", "Manage Plan/Act mode and plan artifacts");

        // plan start <goal>
        var goalArg = new Argument<string>("goal") { Description = "The goal to plan for" };
        var startCmd = new Command("start", "Start a new plan for a goal") { goalArg };
        startCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var goal = r.GetValue(goalArg);
            if (goal is null) { renderer.RenderError("Goal is required"); return; }
            var orchestrator = ctx.GetService<PlanActOrchestrator>();
            var artifact = await orchestrator.StartPlanAsync(goal, ct: ct).ConfigureAwait(false);
            renderer.RenderPlanArtifact(artifact);
        });
        cmd.Add(startCmd);

        // plan list
        var listCmd = new Command("list", "List plan artifacts");
        listCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var store = ctx.GetService<IPlanArtifactStore>();
            var plans = await store.ListAsync(ct).ConfigureAwait(false);
            renderer.RenderPlanList(plans);
        });
        cmd.Add(listCmd);

        // plan apply <id>
        var idArg = new Argument<string>("id") { Description = "Plan artifact ID" };
        var applyCmd = new Command("apply", "Execute a plan artifact") { idArg };
        applyCmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var id = r.GetValue(idArg);
            if (id is null) { renderer.RenderError("Plan ID is required"); return; }
            var store = ctx.GetService<IPlanArtifactStore>();
            var plan = await store.GetAsync(id, ct).ConfigureAwait(false);
            if (plan is null)
            {
                renderer.RenderError($"Plan not found: {id}");
                return;
            }
            var orchestrator = ctx.GetService<PlanActOrchestrator>();
            var result = await orchestrator.ExecutePlanAsync(plan, ct: ct).ConfigureAwait(false);
            renderer.RenderSuccess($"Plan executed:\n{result}");
        });
        cmd.Add(applyCmd);

        // plan mode --set <plan|act>
        var setOption = new Option<string>("--set", "Mode to set: plan|act|idle");
        var modeCmd = new Command("mode", "Set the current mode") { setOption };
        modeCmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var orchestrator = ctx.GetService<PlanActOrchestrator>();
            var modeStr = r.GetValue(setOption);
            var mode = modeStr?.ToLowerInvariant() switch
            {
                "plan" => SessionMode.Plan,
                "act" => SessionMode.Act,
                _ => SessionMode.Idle
            };
            orchestrator.SwitchMode(mode);
            renderer.RenderSuccess($"Mode switched to {mode}");
            return Task.FromResult(0);
        });
        cmd.Add(modeCmd);

        // plan model --plan <model> --act <model>
        var planModelOption = new Option<string>("--plan", "Model for Plan mode");
        var actModelOption = new Option<string>("--act", "Model for Act mode");
        var modelCmd = new Command("model", "Configure models for Plan and Act modes") { planModelOption, actModelOption };
        modelCmd.SetAction((ParseResult r, CancellationToken ct) =>
        {
            var orchestrator = ctx.GetService<PlanActOrchestrator>();
            var planModel = r.GetValue(planModelOption);
            var actModel = r.GetValue(actModelOption);
            if (planModel is not null || actModel is not null)
            {
                var config = orchestrator.Config with
                {
                    PlanModel = planModel ?? orchestrator.Config.PlanModel,
                    ActModel = actModel ?? orchestrator.Config.ActModel
                };
                orchestrator.Configure(config);
            }
            renderer.RenderSuccess($"Plan model: {orchestrator.Config.PlanModel}\nAct model: {orchestrator.Config.ActModel}");
            return Task.FromResult(0);
        });
        cmd.Add(modelCmd);

        return cmd;
    }
}
