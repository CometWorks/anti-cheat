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
    private int maxKnownSectorsPerClient = 4096;
    private int maxSocialListEntries = 2048;
    private int maxGpsStringLength = 2048;
    private int maxFactionStringLength = 2048;
    private bool adminAuditEnabled = true;
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

    public int AdminAuditLogIntervalSeconds
    {
        get => adminAuditLogIntervalSeconds;
        set => SetValue(ref adminAuditLogIntervalSeconds, value);
    }
}
