using System.Diagnostics;
using Avarice.StaticData;
using Dalamud.Plugin.Ipc;
using ECommons.EzIpcManager;

namespace Avarice.Data;

internal class RotationSolverWatcher : IDisposable
{
    private readonly ICallGateSubscriber<byte> _getDesiredPositional;
    private readonly ICallGateSubscriber<byte, object?> _desiredPositionalChanged;

    public RotationSolverWatcher() 
    {
        EzIPC.Init(this);

        _getDesiredPositional = Svc.PluginInterface.GetIpcSubscriber<byte>("RotationSolverReborn.GetDesiredPositional");
        _desiredPositionalChanged = Svc.PluginInterface.GetIpcSubscriber<byte, object?>("RotationSolverReborn.ActionUpdater.DesiredPositionalChanged");

        try
        {
            _desiredPositionalChanged.Subscribe(OnDesiredPositionalChanged);
        }
        catch (Exception ex)
        {
            // RSR not installed/loaded yet - ignore, we'll still be able to poll GetDesiredPositional() later
            Svc.Log.Debug($"Failed to subscribe to RSR DesiredPositionalChanged: {ex.Message}");
        }

        DesiredPositional = PollDesiredPositional(out var pollSucceeded);
		if (pollSucceeded)
		{
			IPCAvailable = true;
		}
	}

	public bool IsRSREnabled()
	{
		try
		{
			const string rsrName = "Rotation Solver Reborn";
			foreach (var p in Svc.PluginInterface.InstalledPlugins)
			{
				if ((p.Name.Equals(rsrName, StringComparison.OrdinalIgnoreCase) || p.InternalName.Equals(rsrName, StringComparison.OrdinalIgnoreCase)) && p.IsLoaded)
				{
					return true;
				}
			}
		}
		catch { }
		return false;
	}

	public void Dispose()
    {
        try
        {
            _desiredPositionalChanged.Unsubscribe(OnDesiredPositionalChanged);
        }
        catch
        {
            // ignore
        }
    }

    public bool IPCAvailable { get; private set; }
    private readonly Stopwatch DataAge = new();
    private uint _nextGcdActionId;
    public uint NextGCDActionId 
    {
        get => DataAge.ElapsedMilliseconds < 5000 ? _nextGcdActionId : 0;
        private set 
        {
			IPCAvailable = true;
            Svc.Log.Debug($"Next GCD Action: {value}");
            DataAge.Restart();
            _nextGcdActionId = value;
        }
    }

    [EzIPCEvent("RotationSolverReborn.ActionUpdater.NextActionChanged", false)]
    public void NextGCDActionChanged(uint action) 
    {
        NextGCDActionId = action;
    }

    public bool TryGetNextGCDActionId(out ActionID o) 
    {
        o = (ActionID) NextGCDActionId;
        return o != 0;
    }

    /// <summary>
    /// RSR's currently desired positional for its next GCD action, kept up to date via the DesiredPositionalChanged IPC event.
    /// Falls back to None if RSR is not installed/loaded.
    /// </summary>
    public EnemyPositional DesiredPositional { get; private set; } = EnemyPositional.None;

    private void OnDesiredPositionalChanged(byte value)
    {
		IPCAvailable = true;
        DesiredPositional = MapPositional(value);
    }

    /// <summary>
    /// Polls RSR's current desired positional directly instead of relying on the event.
    /// Returns None if RSR is not installed/loaded.
    /// </summary>
    public EnemyPositional PollDesiredPositional() => PollDesiredPositional(out _);

    private EnemyPositional PollDesiredPositional(out bool succeeded)
    {
        try
        {
            var result = MapPositional(_getDesiredPositional.InvokeFunc());
            succeeded = true;
            return result;
        }
        catch
        {
            succeeded = false;
            return EnemyPositional.None;
        }
    }

    private static EnemyPositional MapPositional(byte value) => value switch
    {
        1 => EnemyPositional.Rear,
        2 => EnemyPositional.Flank,
        3 => EnemyPositional.Front,
        _ => EnemyPositional.None,
    };
}
