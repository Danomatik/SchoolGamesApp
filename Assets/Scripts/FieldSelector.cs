using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FieldSelector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private List<int> allowedFieldIds; // which fields are selectable
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Color previewColor = Color.cyan;
    [SerializeField] private Button confirmButton; // Assign in Inspector
    
    private GameObject selectedField;
    private GameObject previewField;
    private Dictionary<int, FieldOutline3D> allFields = new Dictionary<int, FieldOutline3D>();
    private Dictionary<int, FieldState> fieldStates = new Dictionary<int, FieldState>();
    private int currentSelectedFieldId = -1;
    private bool hasConfirmedSelection = false;
    private bool isEnabled = false; // Selection is disabled by default
    
    private struct FieldState
    {
        public Color color;
        public bool isHighlighted;
    }
    
    void Start()
    {
        // Find all FieldOutline3D scripts in the scene
        FieldOutline3D[] outlines = FindObjectsOfType<FieldOutline3D>(true);
        foreach (var outline in outlines)
        {
            int id = GetFieldId(outline.gameObject.name);
            if (id != -1 && !allFields.ContainsKey(id))
            {
                allFields.Add(id, outline);
                // Store initial state
                fieldStates[id] = new FieldState { color = Color.white, isHighlighted = false };
            }
        }
        
        // Setup confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
            confirmButton.gameObject.SetActive(false); // Hide initially
        }
        
        UpdateFieldStates();
    }
    
    void Update()
    {
        // Only process input if enabled
        if (!isEnabled) return;
        
        // Only allow selection if we haven't confirmed yet
        if (!hasConfirmedSelection && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                int fieldId = GetFieldId(hitObject.name);
                
                if (fieldId != -1 && allowedFieldIds.Contains(fieldId))
                {
                    PreviewField(fieldId);
                }
            }
        }
        
        // Desktop: Right click or Enter/Space to confirm (if no button assigned)
        if (!hasConfirmedSelection && previewField != null && confirmButton == null)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmSelection();
            }
        }
        
        // Escape to cancel preview
        if (!hasConfirmedSelection && previewField != null && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPreview();
        }
    }
    
    private int GetFieldId(string name)
    {
        if (name.StartsWith("Field_"))
        {
            string num = name.Substring("Field_".Length);
            if (int.TryParse(num, out int id)) return id;
        }
        return -1;
    }
    
    private void PreviewField(int fieldId)
    {
        // If we're already previewing a field, restore it first
        if (previewField != null)
        {
            int previewId = GetFieldId(previewField.name);
            RestoreFieldState(previewId);
        }
        
        // Set new preview
        if (allFields.TryGetValue(fieldId, out var outline))
        {
            previewField = outline.gameObject;
            outline.SetOwned(previewColor, true);
            
            // Show confirm button if assigned
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
            }
            
            Debug.Log($"Previewing field: {fieldId}");
        }
    }

    private void ConfirmSelection()
    {
        if (previewField == null || hasConfirmedSelection || !isEnabled) return;

        int newFieldId = GetFieldId(previewField.name);

        // This is now the final selection - no more changes allowed
        selectedField = previewField;
        currentSelectedFieldId = newFieldId;
        hasConfirmedSelection = true;

        if (allFields.TryGetValue(newFieldId, out var outline))
        {
            outline.SetOwned(highlightColor, true);
            Debug.Log($"Confirmed final selection: {newFieldId}");
        }

        // Update field state
        fieldStates[newFieldId] = new FieldState { color = highlightColor, isHighlighted = true };

        // Hide confirm button
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        previewField = null;
    }
    
    private void CancelPreview()
    {
        if (previewField == null) return;
        
        int previewId = GetFieldId(previewField.name);
        RestoreFieldState(previewId);
        previewField = null;
        
        // Hide confirm button
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }
        
        Debug.Log("Selection cancelled");
    }
    
    private void RestoreFieldState(int fieldId)
    {
        if (allFields.TryGetValue(fieldId, out var outline) && fieldStates.ContainsKey(fieldId))
        {
            FieldState state = fieldStates[fieldId];
            outline.SetOwned(state.color, state.isHighlighted);
        }
    }
    
    private void UpdateFieldStates()
    {
        foreach (var kvp in allFields)
        {
            int id = kvp.Key;
            FieldOutline3D outline = kvp.Value;
            
            if (allowedFieldIds.Contains(id))
            {
                // Reset to inactive (hidden) so they can light up when selected
                outline.SetOwned(Color.white, false);
                fieldStates[id] = new FieldState { color = Color.white, isHighlighted = false };
            }
            else
            {
                // Grey out unselectable fields
                outline.SetOwned(disabledColor, true);
                fieldStates[id] = new FieldState { color = disabledColor, isHighlighted = true };
            }
        }
    }
    
    // PUBLIC METHOD: Enable field selection
    public void EnableSelection()
    {
        isEnabled = true;
        Debug.Log("Field selection enabled");
    }
    
    // PUBLIC METHOD: Disable field selection
    public void DisableSelection()
    {
        isEnabled = false;
        
        // Cancel any active preview when disabling
        if (previewField != null)
        {
            CancelPreview();
        }
        
        Debug.Log("Field selection disabled");
    }
    
    // PUBLIC METHOD: Check if selection is currently enabled
    public bool IsSelectionEnabled()
    {
        return isEnabled;
    }
    
    // PUBLIC METHOD: Get the selected field ID
    // Returns -1 if no field has been selected yet
    public int GetSelectedFieldId()
    {
        return currentSelectedFieldId;
    }
    
    // PUBLIC METHOD: Check if a selection has been confirmed
    public bool HasConfirmedSelection()
    {
        return hasConfirmedSelection;
    }
    
    // Optional: Reset to allow new selection
    public void ResetSelection()
    {
        if (selectedField != null)
        {
            int oldId = GetFieldId(selectedField.name);
            RestoreFieldState(oldId);
        }
        
        selectedField = null;
        previewField = null;
        currentSelectedFieldId = -1;
        hasConfirmedSelection = false;
        
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }
        
        UpdateFieldStates();
    }
    
    // Optional: call this externally when allowedFieldIds change
    public void SetAllowedFields(List<int> newAllowedIds)
    {
        allowedFieldIds = newAllowedIds;
        
        // Cancel any active preview
        if (previewField != null)
        {
            CancelPreview();
        }
        
        UpdateFieldStates();
    }
}