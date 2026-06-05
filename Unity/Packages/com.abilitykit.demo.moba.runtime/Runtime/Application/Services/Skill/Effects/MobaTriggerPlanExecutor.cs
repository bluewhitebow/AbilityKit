using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class MobaTriggerPlanExecutor
    {
        private readonly IWorldResolver _services;
        private readonly TriggerPlanJsonDatabase _planDb;
        private readonly IEventBus _eventBus;
        private readonly FunctionRegistry _functions;
        private readonly ActionRegistry _actions;
        private readonly IPayloadAccessorRegistry _payloads;

        public MobaTriggerPlanExecutor(
            IWorldResolver services,
            TriggerPlanJsonDatabase planDb,
            IEventBus eventBus,
            FunctionRegistry functions,
            ActionRegistry actions,
            IPayloadAccessorRegistry payloads = null)
        {
            _services = services;
            _planDb = planDb;
            _eventBus = eventBus;
            _functions = functions;
            _actions = actions;
            _payloads = payloads;
        }

        public bool TryGetPlan(int triggerId, out TriggerPlan<object> plan)
        {
            plan = default;
            return triggerId > 0 && _planDb != null && _planDb.TryGetPlanByTriggerId(triggerId, out plan);
        }

        public bool Execute(int triggerId, object args)
        {
            return ExecuteInternal(triggerId, args, predicateMissIsSuccess: true);
        }

        public bool ExecuteRulePlan(int triggerId, object args)
        {
            return ExecuteInternal(triggerId, args, predicateMissIsSuccess: false);
        }

        private bool ExecuteInternal(int triggerId, object args, bool predicateMissIsSuccess)
        {
            if (!TryGetPlan(triggerId, out var plan))
            {
                Log.Warning($"[MobaTriggerPlanExecutor] Rule plan not found. triggerId={triggerId}, hasPlanDb={_planDb != null}");
                return false;
            }

            if (_eventBus == null || _functions == null || _actions == null || _payloads == null)
            {
                Log.Warning($"[MobaTriggerPlanExecutor] Plan runtime deps missing; skip plan exec. triggerId={triggerId}, hasEventBus={_eventBus != null}, hasFunctions={_functions != null}, hasActions={_actions != null}, hasPayloads={_payloads != null}");
                return false;
            }

            if (_services == null)
            {
                Log.Warning($"[MobaTriggerPlanExecutor] Plan runtime services missing; skip plan exec. triggerId={triggerId}");
                return false;
            }

            var ctrl = new ExecutionControl();
            ctrl.Reset();

            var execCtx = new ExecCtx<IWorldResolver>(
                context: _services,
                eventBus: _eventBus,
                functions: _functions,
                actions: _actions,
                blackboards: null,
                payloads: _payloads,
                idNames: null,
                numericDomains: null,
                numericFunctions: null,
                policy: default,
                control: ctrl);

            var hasExecutionRoot = _planDb.TryGetExecutionRootByTriggerId(triggerId, out var executionRoot);

            bool ExecuteOnce()
            {
                var planned = new PlannedTrigger<object, IWorldResolver>(plan);
                var ok = planned.Evaluate(args, execCtx);
                if (ctrl.StopPropagation || ctrl.Cancel) return ok;
                if (!ok) return predicateMissIsSuccess;

                if (hasExecutionRoot && executionRoot != null)
                {
                    var result = executionRoot.Execute(args, in execCtx);
                    return result.IsSuccess && result.ExecutedCount > 0;
                }

                planned.Execute(args, execCtx);
                return true;
            }

            try
            {
                return ExecuteOnce();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaTriggerPlanExecutor] Plan execution failed. triggerId={triggerId}");
                return false;
            }
        }
    }
}
