using System;

namespace AbilityKit.Ability.StateSync
{

/// <summary>
/// 预测协调器接口
/// 通用接口，不依赖具体输入类型
/// </summary>
public interface IPredictionCoordinator
{
    int LocalPlayerId { get; }
    int CurrentPredictedFrame { get; }
    int ServerConfirmedFrame { get; }
    bool NeedsRollback { get; }

    void RecordInput(int frame, IInputCommand input);
    void ApplyServerSnapshot(int serverFrame, int objectId, Prediction.StateSlots serverSlots);
    void ExecuteRollback();
    void AdvancePrediction();
    void Reset();
}

}
