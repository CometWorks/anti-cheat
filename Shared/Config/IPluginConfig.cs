using System.ComponentModel;

namespace Shared.Config;

public interface IPluginConfig : INotifyPropertyChanged
{
    // Enables the plugin
    bool Enabled { get; set; }

    bool KickOnValidationFailure { get; set; }
    int ValidationFailuresBeforeKick { get; set; }
    int ValidationFailureWindowSeconds { get; set; }
    bool RejectInvalidToolbarRequests { get; set; }
    bool RejectInvalidCameraSettings { get; set; }
    bool RejectOversizedSocialLists { get; set; }
    bool RejectKnownSectorOverflow { get; set; }
    bool RejectOversizedGpsMessages { get; set; }
    bool RejectOversizedFactionMessages { get; set; }
    bool RejectInvalidPlayerColorLists { get; set; }
    bool PreventGrinderStockpileSpawns { get; set; }
    bool SanitizeToolTargets { get; set; }
    bool EnforceToolTargetCaps { get; set; }
    int MaxShipToolTargets { get; set; }
    bool BlockTimerDetachToolbarActions { get; set; }
    bool CleanUnsafeToolbarReferences { get; set; }
    bool SanitizeTerminalPropertyBounds { get; set; }
    int MaxKnownSectorsPerClient { get; set; }
    int MaxSocialListEntries { get; set; }
    int MaxGpsStringLength { get; set; }
    int MaxFactionStringLength { get; set; }
    bool AdminAuditEnabled { get; set; }
    bool AdminAuditExtendedEnabled { get; set; }
    int AdminAuditLogIntervalSeconds { get; set; }
}
