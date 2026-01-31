using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;






public class MenuInstantiator : MonoBehaviour
{
    private GameObject _pauseMenu;
    
    private RawImage _fadePanelImage;

    private bool _timer, _toPanel;
    private float _fadeTimer;
    [SerializeField] private float fadeTime = 1;
    


    
    
    public void LoadSceneFadeToBlack(int sceneID)
    {
        StartCoroutine(FadeToBlackDelay(sceneID, true));
    }

    public void LoadScene(int sceneID)
    {
        SceneManager.LoadScene(sceneID);
    }

    [Tooltip("Change from pause menu to options menu")]
    public void ChangeActiveMenu(int activeChild)
    {
        for (int i = 0; i < _pauseMenu.transform.childCount; i++)
            _pauseMenu.transform.GetChild(i).gameObject.SetActive(false);

        _pauseMenu.transform.GetChild(activeChild).gameObject.SetActive(true);
    }
    
    
    
    private IEnumerator FadeToBlackDelay(int sceneID, bool tempToPanel)
    {
        _toPanel = tempToPanel;
        _timer = true;
        
        yield return new WaitForSeconds(fadeTime);

        SceneManager.LoadScene(sceneID);
    }
    
    private void Update()
    {
        // Timer ongoing
        if (_timer && _fadeTimer <= fadeTime)
        {
            _fadeTimer += Time.deltaTime;
            
            // Set panel alpha
            var color = _fadePanelImage.color;
            _fadePanelImage.color = new Color(color.r,color.g, color.b, Mathf.Lerp(0, 1, _toPanel ? _fadeTimer/fadeTime : fadeTime - _fadeTimer/fadeTime));
        }
        else if (_timer) // Timer finished
        {
            _timer = false;
            _fadeTimer = 0;
        }
    }
    
    
    
    
    #region Menu Generator
    
    private void OnValidate()
    {
        if (transform.childCount != 0) return;
        
        // Create Canvas
        name = "Canvas";
        var canvas = transform.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        var canvasScaler = transform.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var canvasScale = new Vector2(1920, 1080);
        canvasScaler.referenceResolution = canvasScale;
        transform.AddComponent<GraphicRaycaster>();
        
        // Create background child
        _pauseMenu = Instantiate(new GameObject(), transform);
        _pauseMenu.name = "PauseMenu Background";
        //TODO: Rect width/height: 1920, 1080
        //pauseMenuBackground.GetComponent<RectTransform>() = ; //TODO: Access rect in code, it is private in scripts :(
        _pauseMenu.AddComponent<CanvasRenderer>();
        var pmbImage = _pauseMenu.AddComponent<RawImage>();
        pmbImage.color = new Color(0, 0, 0, 0.2f);
        
        // Pause Menu Grid
        InstantiateGrid(_pauseMenu, "Pause",out var pauseGrid);

        // Buttons
        InstantiateButton(pauseGrid, "Resume");
        InstantiateButton(pauseGrid, "Options");
        InstantiateButton(pauseGrid, "Quit");
        
        
        // Options Menu Grid
        InstantiateGrid(_pauseMenu, "Options",out var optionsGrid);
        
        // Buttons
        InstantiateButton(optionsGrid, "Audio");
        InstantiateButton(optionsGrid, "Video");
        InstantiateButton(optionsGrid, "Controls");
        InstantiateButton(optionsGrid, "Back");
        
        
        // Fade Panel used for fading in and out
        var fadePanel = Instantiate(new GameObject(), transform);
        fadePanel.name = "Fade Panel";
        _fadePanelImage = fadePanel.AddComponent<RawImage>();
        _fadePanelImage.color = new Color(0, 0, 0, 0);
        
        
        
        // Enable/disable objects
        optionsGrid.SetActive(false);
        
        
        // Destroy all new gameobjects without name
        for (int i = 0; i < 50; i++)
            if (GameObject.Find("New Game Object"))
                EditorApplication.delayCall += () => { if (this != null) { DestroyImmediate(GameObject.Find("New Game Object")); } };
        
        // Destroy this script
        //EditorApplication.delayCall += () => { if (this != null) { DestroyImmediate(this); } };
    }

    private void InstantiateGrid(GameObject parent, string gridName, out GameObject grid)
    {
        var options = Instantiate(new GameObject(), parent.transform);
        options.name = gridName + " Grid";
        //TODO: Rect height: 1080
        var optionsGrid = options.AddComponent<GridLayoutGroup>();
        optionsGrid.cellSize = new Vector2(300, 80);
        optionsGrid.spacing = new Vector2(0, 80);
        optionsGrid.childAlignment = TextAnchor.MiddleCenter;

        grid = options;
    }
    
    private void InstantiateButton(GameObject parent, string buttonName)
    {
        var button = Instantiate(new GameObject(), parent.transform);
        button.name = buttonName + " Button";
        button.AddComponent<CanvasRenderer>();
        button.AddComponent<Image>();
        button.AddComponent<Button>();
        
        var buttonText = Instantiate(new GameObject(), button.transform);
        buttonText.name = buttonName + " Text";
        buttonText.AddComponent<CanvasRenderer>();
        
        var tmpText = buttonText.AddComponent<TextMeshProUGUI>();
        tmpText.text = buttonName.ToUpper();
        tmpText.color = Color.black;
        tmpText.alignment = TextAlignmentOptions.Center;
    }
    
    #endregion
}


public class MenuActions : MonoBehaviour
{
    private void Start()
    {
        
    }

    
}