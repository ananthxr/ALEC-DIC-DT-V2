using UnityEngine;
using System;
using System.Collections.Generic;

public class MasterUI : MonoBehaviour
{
    [Header("UI Element Tracking")]
    [SerializeField] private bool debugMode = true;

    // Dictionary to track UI element states by name
    private Dictionary<string, UIElementState> uiElementStates = new Dictionary<string, UIElementState>();

    // Events for state changes
    public event Action<string, bool> OnUIElementVisibilityChanged;
    public event Action<string, UIElementState> OnUIElementStateChanged;

    private void Awake()
    {
        Debug.Log("[MasterUI] Master UI tracker initialized");
    }

    // Called by SlidingPanelController when panel state changes
    public void OnPanelStateChanged(bool isVisible)
    {
        string panelName = "SlidingPanel";
        UpdateUIElementState(panelName, isVisible);

        if (debugMode)
        {
            Debug.Log($"[MasterUI] Panel state changed - {panelName}: visible={isVisible}");
        }
    }

    // Called by FloorButtonStacking when floor button state changes
    public void UpdateFloorButtonState(int floorIndex, bool isSelected)
    {
        string floorName = GetFloorName(floorIndex);
        UpdateUIElementState(floorName, isSelected, $"Floor {floorIndex}");

        if (debugMode)
        {
            Debug.Log($"[MasterUI] Floor button state changed - {floorName}: selected={isSelected}");
        }
    }

    // Helper method to get floor display names
    private string GetFloorName(int floorIndex)
    {
        return floorIndex switch
        {
            0 => "GroundFloor",
            1 => "FirstFloor",
            2 => "RoofFloor",
            _ => $"Floor{floorIndex}"
        };
    }

    // Generic method to update any UI element state
    public void UpdateUIElementState(string elementName, bool isVisible, string additionalInfo = "")
    {
        // Get or create state for this element
        if (!uiElementStates.ContainsKey(elementName))
        {
            uiElementStates[elementName] = new UIElementState
            {
                elementName = elementName,
                isVisible = isVisible,
                lastUpdateTime = Time.time,
                additionalInfo = additionalInfo
            };
        }
        else
        {
            // Update existing state
            uiElementStates[elementName].isVisible = isVisible;
            uiElementStates[elementName].lastUpdateTime = Time.time;
            uiElementStates[elementName].additionalInfo = additionalInfo;
        }

        // Invoke events
        OnUIElementVisibilityChanged?.Invoke(elementName, isVisible);
        OnUIElementStateChanged?.Invoke(elementName, uiElementStates[elementName]);

        if (debugMode)
        {
            Debug.Log($"[MasterUI] UI Element '{elementName}' updated: visible={isVisible}, info='{additionalInfo}'");
        }
    }

    // Query methods to check UI element states
    public bool IsUIElementVisible(string elementName)
    {
        if (uiElementStates.TryGetValue(elementName, out UIElementState state))
        {
            return state.isVisible;
        }
        return false;
    }

    public UIElementState GetUIElementState(string elementName)
    {
        if (uiElementStates.TryGetValue(elementName, out UIElementState state))
        {
            return state;
        }
        return null;
    }

    public Dictionary<string, UIElementState> GetAllUIStates()
    {
        return new Dictionary<string, UIElementState>(uiElementStates);
    }

    // Debug method to print all current UI states
    [ContextMenu("Print All UI States")]
    public void PrintAllUIStates()
    {
        Debug.Log($"[MasterUI] === Current UI States ({uiElementStates.Count} elements) ===");
        foreach (var kvp in uiElementStates)
        {
            Debug.Log($"[MasterUI] {kvp.Key}: visible={kvp.Value.isVisible}, lastUpdate={kvp.Value.lastUpdateTime}, info='{kvp.Value.additionalInfo}'");
        }
    }

    // Method to clear a specific UI element state
    public void ClearUIElementState(string elementName)
    {
        if (uiElementStates.ContainsKey(elementName))
        {
            uiElementStates.Remove(elementName);
            Debug.Log($"[MasterUI] Cleared state for: {elementName}");
        }
    }

    // Method to clear all UI element states
    public void ClearAllStates()
    {
        uiElementStates.Clear();
        Debug.Log("[MasterUI] All UI states cleared");
    }
}

// Data structure to hold UI element state information
[System.Serializable]
public class UIElementState
{
    public string elementName;
    public bool isVisible;
    public float lastUpdateTime;
    public string additionalInfo;

    public override string ToString()
    {
        return $"{elementName}: visible={isVisible}, updated={lastUpdateTime}, info='{additionalInfo}'";
    }
}
