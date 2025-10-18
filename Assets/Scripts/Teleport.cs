using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshPro
using UnityEngine.UI; // For regular UI Text

public class Teleport : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load (must be in Build Settings)")]
    public string sceneToLoad = "DungeonScene";
    
    [Header("UI Settings")]
    [Tooltip("UI Text/TextMeshPro to show the prompt")]
    public GameObject promptUI;
    public string promptText = "Press F to teleport to the Dungeon";
    
    [Header("Teleport Settings")]
    public KeyCode teleportKey = KeyCode.F;
    public bool useColliderTrigger = true; // Use OnTriggerEnter2D
    
    [Header("Auto-Create UI")]
    public bool autoCreateUI = true;
    public Vector3 uiOffset = new Vector3(0, 2, 0); // Offset above teleporter
    
    private bool playerInRange = false;
    private GameObject player;
    private Canvas worldCanvas;
    private TextMeshProUGUI tmpText;
    private Text regularText;

    void Start()
    {
        // Create UI automatically if needed
        if (autoCreateUI && promptUI == null)
        {
            CreatePromptUI();
        }
        
        // Make sure the prompt is hidden at start
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        
        // Ensure this object has a trigger collider
        if (useColliderTrigger)
        {
            SetupTriggerCollider();
        }
        
        Debug.Log($"[Teleport] Ready to teleport to scene: {sceneToLoad}");
    }

    void Update()
    {
        // Check if player is in range and presses the teleport key
        if (playerInRange && Input.GetKeyDown(teleportKey))
        {
            TeleportToScene();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player entered the teleport zone
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.gameObject;
            ShowPrompt();
            Debug.Log("[Teleport] Player entered teleport zone");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the player left the teleport zone
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
            HidePrompt();
            Debug.Log("[Teleport] Player left teleport zone");
        }
    }
    
    private void ShowPrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            
            // Update text content
            if (tmpText != null)
            {
                tmpText.text = promptText;
            }
            else if (regularText != null)
            {
                regularText.text = promptText;
            }
        }
    }
    
    private void HidePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }
    
    private void TeleportToScene()
    {
        Debug.Log($"[Teleport] Teleporting player to scene: {sceneToLoad}");
        
        // Check if the scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"[Teleport] Scene '{sceneToLoad}' not found! Make sure it's added to Build Settings.");
        }
    }
    
    private void CreatePromptUI()
    {
        Debug.Log("[Teleport] Auto-creating prompt UI");
        
        // Create a world space canvas
        GameObject canvasObj = new GameObject("TeleportPromptCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = uiOffset;
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 100; // Make sure it's visible
        
        // Scale the canvas
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 100);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Small scale for world space
        
        // Add CanvasScaler for better quality
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Try to use TextMeshPro first, fall back to regular Text
        if (TryCreateTextMeshPro(canvasObj))
        {
            Debug.Log("[Teleport] Created TextMeshPro UI");
        }
        else
        {
            CreateRegularText(canvasObj);
            Debug.Log("[Teleport] Created regular UI Text");
        }
        
        promptUI = canvasObj;
    }
    
    private bool TryCreateTextMeshPro(GameObject parent)
    {
        try
        {
            // Create TextMeshPro text
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(parent.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = Vector3.one;
            
            tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = promptText;
            tmpText.fontSize = 36;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.textWrappingMode = TextWrappingModes.Normal; // Updated property
            
            // Add outline for better visibility
            tmpText.outlineWidth = 0.2f;
            tmpText.outlineColor = Color.black;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(400, 100);
            textRect.anchoredPosition = Vector2.zero;
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private void CreateRegularText(GameObject parent)
    {
        // Create regular UI Text as fallback
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(parent.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = Vector3.one;
        
        regularText = textObj.AddComponent<Text>();
        regularText.text = promptText;
        regularText.fontSize = 24;
        regularText.color = Color.white;
        regularText.alignment = TextAnchor.MiddleCenter;
        regularText.horizontalOverflow = HorizontalWrapMode.Wrap;
        regularText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Try to find a font
        regularText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // Add outline for better visibility
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(400, 100);
        textRect.anchoredPosition = Vector2.zero;
    }
    
    private void SetupTriggerCollider()
    {
        // Check if there's already a collider
        Collider2D existingCollider = GetComponent<Collider2D>();
        
        if (existingCollider == null)
        {
            // Add a box collider as trigger
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(2f, 3f); // Adjust size as needed
            Debug.Log("[Teleport] Added BoxCollider2D as trigger");
        }
        else
        {
            // Make sure existing collider is a trigger
            existingCollider.isTrigger = true;
            Debug.Log("[Teleport] Set existing collider as trigger");
        }
    }
    
    // Context menu for testing
    [ContextMenu("Test Teleport")]
    void TestTeleport()
    {
        TeleportToScene();
    }
    
    [ContextMenu("Toggle Prompt")]
    void TogglePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(!promptUI.activeSelf);
        }
    }
    
    // Draw gizmo to show teleport zone
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
        
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(2f, 3f, 0f));
        }
        
        // Draw label
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 3f, 0f));
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw more prominent gizmo when selected
        Gizmos.color = Color.yellow;
        
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        Vector3 size = boxCollider != null ? boxCollider.size : new Vector3(2f, 3f, 0f);
        Vector3 offset = boxCollider != null ? boxCollider.offset : Vector3.zero;
        
        Gizmos.DrawWireCube(transform.position + offset, size);
    }
}
