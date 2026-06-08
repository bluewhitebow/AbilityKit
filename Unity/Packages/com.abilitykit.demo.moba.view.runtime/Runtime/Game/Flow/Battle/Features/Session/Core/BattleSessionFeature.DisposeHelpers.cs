using AbilityKit.Core.Common.Log;
using AbilityKit.Ability.World.Abstractions;
using System;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TryDestroyBattleWorlds()
        {
            try
            {
                if (_handles.RemoteDriven.WorldRuntime != null)
                {
                    _handles.RemoteDriven.WorldRuntime.DestroyWorld();
                }
                else
                {
                    _handles.RemoteDriven.Runtime?.DestroyWorld(new WorldId(_plan.WorldId));
                }

                if (_handles.Confirmed.WorldRuntime != null)
                {
                    _handles.Confirmed.WorldRuntime.DestroyWorld();
                }
                else
                {
                    _handles.Confirmed.Runtime?.DestroyWorld(new WorldId((_plan.WorldId ?? "room_1") + "__confirmed"));
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void DisposeConfirmedView()
        {
            DetachConfirmedViewFeature();
            DisposeConfirmedViewSnapshotPipeline();
            DisposeConfirmedViewContext();
        }

        private void DetachConfirmedViewFeature()
        {
            if (_flow != null && _confirmedViewFeature != null)
            {
                _flow.Detach(_confirmedViewFeature);
                _confirmedViewFeature = null;
            }
        }

        private void DisposeConfirmedViewSnapshotPipeline()
        {
            var runtime = _confirmedViewSnapshotRuntime;
            _confirmedViewSnapshotRuntime = null;

            if (runtime == null) return;

            try
            {
                runtime.Dispose();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void DisposeConfirmedViewContext()
        {
            ConfirmedViewContextDisposer.Dispose(_confirmedViewCtx, DestroyEntityTree);
            _confirmedViewCtx = null;
        }

        private void DisposeRemoteDrivenWorld()
        {
            _handles.RemoteDriven.ClearWorldRuntime();
            _remoteDrivenLastTickedFrame = 0;
            _handles.RemoteDriven.DisposeInput();
        }

        private void DisposeConfirmedWorld()
        {
            ClearConfirmedWorldRuntime();
            DisposeConfirmedInput();
            DisposeConfirmedViewEventPipeline();
            ClearConfirmedDebugContext();
        }

        private void ClearConfirmedWorldRuntime()
        {
            _handles.Confirmed.ClearWorldRuntime();
            _confirmedLastTickedFrame = 0;
        }

        private void DisposeConfirmedInput()
        {
            _handles.Confirmed.DisposeInput();
        }

        private void DisposeConfirmedViewEventPipeline()
        {
            var pipeline = _confirmedViewEventPipeline;
            _confirmedViewEventPipeline = null;

            if (pipeline != null)
            {
                try
                {
                    pipeline.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
            else
            {
                _confirmedSnapshotViewAdapter?.Dispose();
                _confirmedTriggerBridge?.Dispose();
                _confirmedSnapshots?.Dispose();
            }

            _confirmedSnapshotViewAdapter = null;
            _confirmedTriggerBridge = null;
            _confirmedViewEventSink = null;
            _confirmedSnapshots = null;
        }

        private void ClearConfirmedDebugContext()
        {
            ConfirmedAuthorityDebugStatsPublisher.Clear(_ctx);
        }

        private void DisposeNetworkIoDispatcher()
        {
            try
            {
                _networkIoDispatcher?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            finally
            {
                _networkIoDispatcher = null;
            }
        }
    }
}
