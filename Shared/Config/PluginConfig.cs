using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shared.Config;

public class PluginConfig : IPluginConfig
{
    public event PropertyChangedEventHandler PropertyChanged;

    private void SetValue<T>(ref T field, T value, [CallerMemberName] string propName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        OnPropertyChanged(propName);
    }

    private void OnPropertyChanged([CallerMemberName] string propName = "")
    {
        PropertyChangedEventHandler propertyChanged = PropertyChanged;
        if (propertyChanged == null)
            return;

        propertyChanged(this, new PropertyChangedEventArgs(propName));
    }

    private bool enabled = true;
    private bool kickOnValidationFailure = true;
    private int validationFailuresBeforeKick = 3;
    private int validationFailureWindowSeconds = 30;
    private bool rejectInvalidToolbarRequests = true;
    private bool rejectInvalidCameraSettings = true;
    private bool rejectOversizedSocialLists = true;
    private bool rejectKnownSectorOverflow = true;
    private bool rejectOversizedGpsMessages = true;
    private bool rejectOversizedFactionMessages = true;
    private bool rejectInvalidPlayerColorLists = true;
    private bool preventGrinderStockpileSpawns = false;
    private bool sanitizeToolTargets = true;
    private bool enforceToolTargetCaps = true;
    private int maxShipToolTargets = 30;
    private bool blockTimerDetachToolbarActions = true;
    private bool cleanUnsafeToolbarReferences = true;
    private bool sanitizeTerminalPropertyBounds = true;
    private int maxKnownSectorsPerClient = 4096;
    private int maxSocialListEntries = 2048;
    private int maxGpsStringLength = 2048;
    private int maxFactionStringLength = 2048;
    private bool adminAuditEnabled = true;
    private bool adminAuditExtendedEnabled = true;
    private int adminAuditLogIntervalSeconds = 10;

    public bool Enabled
    {
        get => enabled;
        set => SetValue(ref enabled, value);
    }

    public bool KickOnValidationFailure
    {
        get => kickOnValidationFailure;
        set => SetValue(ref kickOnValidationFailure, value);
    }

    public int ValidationFailuresBeforeKick
    {
        get => validationFailuresBeforeKick;
        set => SetValue(ref validationFailuresBeforeKick, value);
    }

    public int ValidationFailureWindowSeconds
    {
        get => validationFailureWindowSeconds;
        set => SetValue(ref validationFailureWindowSeconds, value);
    }

    public bool RejectInvalidToolbarRequests
    {
        get => rejectInvalidToolbarRequests;
        set => SetValue(ref rejectInvalidToolbarRequests, value);
    }

    public bool RejectInvalidCameraSettings
    {
        get => rejectInvalidCameraSettings;
        set => SetValue(ref rejectInvalidCameraSettings, value);
    }

    public bool RejectOversizedSocialLists
    {
        get => rejectOversizedSocialLists;
        set => SetValue(ref rejectOversizedSocialLists, value);
    }

    public bool RejectKnownSectorOverflow
    {
        get => rejectKnownSectorOverflow;
        set => SetValue(ref rejectKnownSectorOverflow, value);
    }

    public bool RejectOversizedGpsMessages
    {
        get => rejectOversizedGpsMessages;
        set => SetValue(ref rejectOversizedGpsMessages, value);
    }

    public bool RejectOversizedFactionMessages
    {
        get => rejectOversizedFactionMessages;
        set => SetValue(ref rejectOversizedFactionMessages, value);
    }

    public bool RejectInvalidPlayerColorLists
    {
        get => rejectInvalidPlayerColorLists;
        set => SetValue(ref rejectInvalidPlayerColorLists, value);
    }

    [Description("Prevents ship grinders from spawning construction stockpile item drops while grinding blocks. Useful item-spam/performance guard; changes grinder item return behavior.")]
    public bool PreventGrinderStockpileSpawns
    {
        get => preventGrinderStockpileSpawns;
        set => SetValue(ref preventGrinderStockpileSpawns, value);
    }

    [Description("Removes invalid, closed, self, or duplicate ship-tool targets before grinders, welders, and drills process them.")]
    public bool SanitizeToolTargets
    {
        get => sanitizeToolTargets;
        set => SetValue(ref sanitizeToolTargets, value);
    }

    [Description("Limits grinder, welder, and drill targets processed per activation to reduce item-spam and CPU spikes.")]
    public bool EnforceToolTargetCaps
    {
        get => enforceToolTargetCaps;
        set => SetValue(ref enforceToolTargetCaps, value);
    }

    [Description("Maximum grinder, welder, or drill targets allowed per activation when EnforceToolTargetCaps is enabled.")]
    public int MaxShipToolTargets
    {
        get => maxShipToolTargets;
        set => SetValue(ref maxShipToolTargets, value);
    }

    [Description("Removes Detach terminal actions from timer block toolbars to prevent stale mechanical detach abuse.")]
    public bool BlockTimerDetachToolbarActions
    {
        get => blockTimerDetachToolbarActions;
        set => SetValue(ref blockTimerDetachToolbarActions, value);
    }

    [Description("Clears unsafe toolbar references on ownerless toolbar blocks and when a block's ownership changes to Nobody.")]
    public bool CleanUnsafeToolbarReferences
    {
        get => cleanUnsafeToolbarReferences;
        set => SetValue(ref cleanUnsafeToolbarReferences, value);
    }

    [Description("Clamps bad replicated rotor, piston, wheel, gyro, and thrust override terminal values after sync apply.")]
    public bool SanitizeTerminalPropertyBounds
    {
        get => sanitizeTerminalPropertyBounds;
        set => SetValue(ref sanitizeTerminalPropertyBounds, value);
    }

    public int MaxKnownSectorsPerClient
    {
        get => maxKnownSectorsPerClient;
        set => SetValue(ref maxKnownSectorsPerClient, value);
    }

    public int MaxSocialListEntries
    {
        get => maxSocialListEntries;
        set => SetValue(ref maxSocialListEntries, value);
    }

    public int MaxGpsStringLength
    {
        get => maxGpsStringLength;
        set => SetValue(ref maxGpsStringLength, value);
    }

    public int MaxFactionStringLength
    {
        get => maxFactionStringLength;
        set => SetValue(ref maxFactionStringLength, value);
    }

    public bool AdminAuditEnabled
    {
        get => adminAuditEnabled;
        set => SetValue(ref adminAuditEnabled, value);
    }

    [Description("Extends admin audit logging to paste/delete/cut, ownership changes, forced toolbar cleanup, and terminal value sanitization.")]
    public bool AdminAuditExtendedEnabled
    {
        get => adminAuditExtendedEnabled;
        set => SetValue(ref adminAuditExtendedEnabled, value);
    }

    public int AdminAuditLogIntervalSeconds
    {
        get => adminAuditLogIntervalSeconds;
        set => SetValue(ref adminAuditLogIntervalSeconds, value);
    }
}
