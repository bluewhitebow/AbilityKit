namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 传输层会话接口。
/// 该接口是 Gateway 业务路由与具体网络传输之间的稳定边界，后续 TCP/UDP/WebSocket 会话都应实现它。
/// </summary>
public interface IGatewayTransportSession
{
    /// <summary>
    /// Gateway 内部连接 ID。
    /// </summary>
    long ConnectionId { get; }

    /// <summary>
    /// 传输实现名称。
    /// </summary>
    string TransportName { get; }

    /// <summary>
    /// 会话上下文。
    /// </summary>
    GatewaySessionContext Context { get; }

    /// <summary>
    /// 是否已连接或仍可发送。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 发送请求响应。
    /// </summary>
    Task SendResponseAsync(uint opCode, uint seq, byte[] payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送服务器推送。
    /// </summary>
    Task SendServerPushAsync(uint opCode, byte[] payload, CancellationToken cancellationToken = default);
}
