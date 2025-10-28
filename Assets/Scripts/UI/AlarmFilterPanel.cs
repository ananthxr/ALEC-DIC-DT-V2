using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlarmFilterPanel : MonoBehaviour
{
    [Header("=== STATUS FILTER TOGGLES ===")]
    [SerializeField] private Toggle activeToggle;
    [SerializeField] private Toggle unackToggle;
    [SerializeField] private Toggle clearedToggle;
    [SerializeField] private Toggle ackToggle;

    [Header("=== SEVERITY FILTER TOGGLES ===")]
    [SerializeField] private Toggle criticalToggle;
    [SerializeField] private Toggle majorToggle;
    [SerializeField] private Toggle minorToggle;
    [SerializeField] private Toggle warningToggle;
    [SerializeField] private Toggle indeterminateToggle;

    [Header("=== UPDATE BUTTON ===")]
    [SerializeField] private Button updateButton;

    [Header("=== REFERENCE ===")]
    [SerializeField] private MasterAlarm masterAlarm;

    // Track current STATUS toggle states
    private bool isActiveSelected = false;
    private bool isUnackSelected = false;
    private bool isClearedSelected = false;
    private bool isAckSelected = false;

    // Track current SEVERITY toggle states
    private bool isCriticalSelected = false;
    private bool isMajorSelected = false;
    private bool isMinorSelected = false;
    private bool isWarningSelected = false;
    private bool isIndeterminateSelected = false;

    private void Start()
    {
        if (masterAlarm == null)
        {
            Debug.LogError("[AlarmFilterPanel] MasterAlarm reference is not assigned!");
            return;
        }

        // Set up toggle listeners to track state changes
        if (activeToggle != null)
        {
            activeToggle.onValueChanged.AddListener(OnActiveToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Active toggle is not assigned!");
        }

        if (unackToggle != null)
        {
            unackToggle.onValueChanged.AddListener(OnUnackToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Unack toggle is not assigned!");
        }

        if (clearedToggle != null)
        {
            clearedToggle.onValueChanged.AddListener(OnClearedToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Cleared toggle is not assigned!");
        }

        if (ackToggle != null)
        {
            ackToggle.onValueChanged.AddListener(OnAckToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Ack toggle is not assigned!");
        }

        // ========== SEVERITY TOGGLE LISTENERS ==========
        if (criticalToggle != null)
        {
            criticalToggle.onValueChanged.AddListener(OnCriticalToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Critical toggle is not assigned!");
        }

        if (majorToggle != null)
        {
            majorToggle.onValueChanged.AddListener(OnMajorToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Major toggle is not assigned!");
        }

        if (minorToggle != null)
        {
            minorToggle.onValueChanged.AddListener(OnMinorToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Minor toggle is not assigned!");
        }

        if (warningToggle != null)
        {
            warningToggle.onValueChanged.AddListener(OnWarningToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Warning toggle is not assigned!");
        }

        if (indeterminateToggle != null)
        {
            indeterminateToggle.onValueChanged.AddListener(OnIndeterminateToggleChanged);
        }
        else
        {
            Debug.LogWarning("[AlarmFilterPanel] Indeterminate toggle is not assigned!");
        }

        // Set up update button listener
        if (updateButton != null)
        {
            updateButton.onClick.AddListener(OnUpdateButtonClicked);
        }
        else
        {
            Debug.LogError("[AlarmFilterPanel] Update button is not assigned!");
        }

        Debug.Log("[AlarmFilterPanel] Initialized successfully");
    }

    private void OnActiveToggleChanged(bool isOn)
    {
        isActiveSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Active toggle: {isOn}");
    }

    private void OnUnackToggleChanged(bool isOn)
    {
        isUnackSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Unack toggle: {isOn}");
    }

    private void OnClearedToggleChanged(bool isOn)
    {
        isClearedSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Cleared toggle: {isOn}");
    }

    private void OnAckToggleChanged(bool isOn)
    {
        isAckSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Ack toggle: {isOn}");
    }

    // ========== SEVERITY TOGGLE CALLBACKS ==========

    private void OnCriticalToggleChanged(bool isOn)
    {
        isCriticalSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Critical toggle: {isOn}");
    }

    private void OnMajorToggleChanged(bool isOn)
    {
        isMajorSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Major toggle: {isOn}");
    }

    private void OnMinorToggleChanged(bool isOn)
    {
        isMinorSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Minor toggle: {isOn}");
    }

    private void OnWarningToggleChanged(bool isOn)
    {
        isWarningSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Warning toggle: {isOn}");
    }

    private void OnIndeterminateToggleChanged(bool isOn)
    {
        isIndeterminateSelected = isOn;
        Debug.Log($"[AlarmFilterPanel] Indeterminate toggle: {isOn}");
    }

    // ========== UPDATE BUTTON CALLBACK ==========

    private void OnUpdateButtonClicked()
    {
        Debug.Log("[AlarmFilterPanel] Update button clicked - applying filters...");

        // Build status list based on selected toggles
        List<string> statusList = BuildStatusList();

        // Build severity list based on selected toggles
        List<string> severityList = BuildSeverityList();

        Debug.Log($"[AlarmFilterPanel] Generated status filter: [{string.Join(", ", statusList)}]");
        Debug.Log($"[AlarmFilterPanel] Generated severity filter: [{string.Join(", ", severityList)}]");

        // Apply filters to MasterAlarm (resubscribe with new filters)
        if (masterAlarm != null)
        {
            masterAlarm.UpdateFilters(statusList, severityList);
        }
        else
        {
            Debug.LogError("[AlarmFilterPanel] MasterAlarm reference is null!");
        }
    }

    private List<string> BuildStatusList()
    {
        List<string> statusList = new List<string>();

        // API expects separate individual status values, NOT combined strings
        // Example: User selects Active + Ack → ["ACTIVE", "ACK"]
        // Example: User selects Cleared + Unack → ["CLEARED", "UNACK"]
        // The server will filter alarms that match ANY of these statuses

        // Add selected state filters
        if (isActiveSelected)
        {
            statusList.Add("ACTIVE");
        }
        if (isClearedSelected)
        {
            statusList.Add("CLEARED");
        }

        // Add selected acknowledgment filters
        if (isAckSelected)
        {
            statusList.Add("ACK");
        }
        if (isUnackSelected)
        {
            statusList.Add("UNACK");
        }

        // If nothing is selected, return empty list (will show all alarms)
        if (statusList.Count == 0)
        {
            Debug.Log("[AlarmFilterPanel] No filters selected - will show all alarms");
        }

        return statusList;
    }

    // ========== BUILD SEVERITY LIST ==========

    private List<string> BuildSeverityList()
    {
        List<string> severityList = new List<string>();

        // API expects severity values: "CRITICAL", "MAJOR", "MINOR", "WARNING", "INDETERMINATE"
        // Example: User selects Critical + Major → ["CRITICAL", "MAJOR"]
        // The server will filter alarms that match ANY of these severities

        if (isCriticalSelected)
        {
            severityList.Add("CRITICAL");
        }
        if (isMajorSelected)
        {
            severityList.Add("MAJOR");
        }
        if (isMinorSelected)
        {
            severityList.Add("MINOR");
        }
        if (isWarningSelected)
        {
            severityList.Add("WARNING");
        }
        if (isIndeterminateSelected)
        {
            severityList.Add("INDETERMINATE");
        }

        // If nothing is selected, return empty list (will show all severities)
        if (severityList.Count == 0)
        {
            Debug.Log("[AlarmFilterPanel] No severity filters selected - will show all severities");
        }

        return severityList;
    }

    // ========== CLEANUP ==========

    private void OnDestroy()
    {
        // Clean up STATUS toggle listeners
        if (activeToggle != null)
        {
            activeToggle.onValueChanged.RemoveListener(OnActiveToggleChanged);
        }
        if (unackToggle != null)
        {
            unackToggle.onValueChanged.RemoveListener(OnUnackToggleChanged);
        }
        if (clearedToggle != null)
        {
            clearedToggle.onValueChanged.RemoveListener(OnClearedToggleChanged);
        }
        if (ackToggle != null)
        {
            ackToggle.onValueChanged.RemoveListener(OnAckToggleChanged);
        }

        // Clean up SEVERITY toggle listeners
        if (criticalToggle != null)
        {
            criticalToggle.onValueChanged.RemoveListener(OnCriticalToggleChanged);
        }
        if (majorToggle != null)
        {
            majorToggle.onValueChanged.RemoveListener(OnMajorToggleChanged);
        }
        if (minorToggle != null)
        {
            minorToggle.onValueChanged.RemoveListener(OnMinorToggleChanged);
        }
        if (warningToggle != null)
        {
            warningToggle.onValueChanged.RemoveListener(OnWarningToggleChanged);
        }
        if (indeterminateToggle != null)
        {
            indeterminateToggle.onValueChanged.RemoveListener(OnIndeterminateToggleChanged);
        }

        // Clean up button listener
        if (updateButton != null)
        {
            updateButton.onClick.RemoveListener(OnUpdateButtonClicked);
        }
    }
}
