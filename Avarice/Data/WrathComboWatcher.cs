using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc;

namespace Avarice.Data;

internal sealed class WrathComboWatcher : IDisposable
{
    private const string HintGate = "WrathCombo.GetUpcomingPositionalHint";
    private const string HintChangedGate = "OnUpcomingPositionalHint";
    private const int HintFieldCount = 7;
    private const int PollIntervalMs = 1000;

    private readonly ICallGateSubscriber<uint[]> getHintSubscriber;
    private readonly ICallGateSubscriber<object> hintChangedSubscriber;

    private WrathComboPositionalHint currentHint = WrathComboPositionalHint.Empty;
    private long nextPollTick;

    internal WrathComboWatcher()
    {
        getHintSubscriber = Svc.PluginInterface.GetIpcSubscriber<uint[]>(HintGate);
        hintChangedSubscriber = Svc.PluginInterface.GetIpcSubscriber<object>(HintChangedGate);

        try
        {
            hintChangedSubscriber.Subscribe(OnHintChanged);
        }
        catch (Exception ex)
        {
            PluginLog.Debug($"Unable to subscribe to WrathCombo positional hints: {ex.Message}");
        }

        Refresh();
    }

    internal bool Available { get; private set; }

    internal void Tick()
    {
        var now = Environment.TickCount64;

        if (currentHint.IsExpired(now))
            currentHint = WrathComboPositionalHint.Empty;

        if (now < nextPollTick)
            return;

        nextPollTick = now + PollIntervalMs;
        Refresh();
    }

    internal bool TryGetHintForTarget(IBattleNpc target, out WrathComboPositionalDirection direction)
    {
        direction = WrathComboPositionalDirection.None;

        if (!currentHint.IsActive(Environment.TickCount64) ||
            currentHint.IsSatisfied ||
            currentHint.TargetObjectId != target.GameObjectId)
            return false;

        direction = currentHint.Direction;
        return direction is WrathComboPositionalDirection.Rear or WrathComboPositionalDirection.Flank;
    }

    public void Dispose()
    {
        try
        {
            hintChangedSubscriber.Unsubscribe(OnHintChanged);
        }
        catch (Exception ex)
        {
            PluginLog.Debug($"Unable to unsubscribe from WrathCombo positional hints: {ex.Message}");
        }
    }

    private void OnHintChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            var wire = getHintSubscriber.InvokeFunc();
            Available = true;

            currentHint = TryParse(wire, out var hint)
                ? hint
                : WrathComboPositionalHint.Empty;
        }
        catch
        {
            Available = false;
            currentHint = WrathComboPositionalHint.Empty;
        }
    }

    private static bool TryParse(uint[] wire, out WrathComboPositionalHint hint)
    {
        hint = WrathComboPositionalHint.Empty;

        if (wire is null || wire.Length < HintFieldCount)
            return false;

        var direction = (WrathComboPositionalDirection)wire[0];
        var expiresInMs = (int)wire[4];

        if (direction is WrathComboPositionalDirection.None or WrathComboPositionalDirection.Unknown ||
            wire[1] is 0 ||
            wire[2] is 0 ||
            expiresInMs <= 0)
            return false;

        hint = new WrathComboPositionalHint
        {
            Direction = direction,
            ActionId = wire[1],
            GcdsUntil = (int)wire[2],
            TargetObjectId = wire[3],
            ExpiresAtTick = Environment.TickCount64 + expiresInMs,
            CurrentAngle = wire[5],
            IsSatisfied = wire[6] is not 0,
        };

        return true;
    }
}

internal enum WrathComboPositionalDirection : uint
{
    None = 0,
    Rear = 1,
    Flank = 2,
    Unknown = 3,
}

internal readonly struct WrathComboPositionalHint
{
    internal static WrathComboPositionalHint Empty => new();

    internal WrathComboPositionalDirection Direction { get; init; }
    internal uint ActionId { get; init; }
    internal int GcdsUntil { get; init; }
    internal ulong TargetObjectId { get; init; }
    internal long ExpiresAtTick { get; init; }
    internal uint CurrentAngle { get; init; }
    internal bool IsSatisfied { get; init; }

    internal bool IsActive(long now) =>
        Direction is WrathComboPositionalDirection.Rear or WrathComboPositionalDirection.Flank &&
        ActionId is not 0 &&
        GcdsUntil > 0 &&
        ExpiresAtTick > now;

    internal bool IsExpired(long now) =>
        Direction is not WrathComboPositionalDirection.None && ExpiresAtTick <= now;
}
