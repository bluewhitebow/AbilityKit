namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 传输层事件接口。
/// 业务网关只感知通用传输会话，TCP/UDP/WebSocket 等具体传输负责适配该事件模型。
/// </summary>
public interface IGatewayTransportEvents
{
    void OnConnected(IGatewayTransportSession session);

    void OnRequest(long connectionId, uint opCode, uint seq, byte[] payload);

    void OnClosed(long connectionId);
}
