using System;
using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ChangelogEditor;

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
static class FejdStartupStartPatch
{
    static void Postfix(FejdStartup __instance)
    {
        if (__instance.GetComponent<ChangelogWindow>() != null) return;
        __instance.gameObject.AddComponent<ChangelogWindow>().Initialize(__instance);
    }
}

[HarmonyPatch(typeof(FejdStartup), "OnButtonShowChangelog")]
static class ShowChangelogPatch
{
    static bool Prefix(FejdStartup __instance)
    {
        ChangelogWindow window = __instance.GetComponent<ChangelogWindow>();
        if (window == null) return true;
        window.Toggle();
        return false;
    }
}

public sealed class ChangelogWindow : MonoBehaviour
{
    private FejdStartup _startup = null!;
    private GameObject _button = null!;
    private bool _originalButtonActive;
    private Button[] _menuButtons = Array.Empty<Button>();
    private Component _canvas = null!;
    private readonly Vector3[] _buttonCorners = new Vector3[4];
    private string _defaultText = "";
    private string _text = "";
    private Vector2 _scroll;
    private bool _visible;
    private GUIStyle _textStyle = null!;
    private Rect _windowRect;
    internal static ChangelogWindow Instance = null!;

    internal void Initialize(FejdStartup startup)
    {
        _startup = startup;
        Instance = this;
        _button = startup.m_showChangelogButton;
        if (_button != null)
        {
            _originalButtonActive = _button.activeSelf;
            Transform buttonGroup = _button.transform.parent;
            _menuButtons = buttonGroup != null ? buttonGroup.GetComponentsInChildren<Button>(true) : _button.GetComponents<Button>();
            Type canvasType = AccessTools.TypeByName("UnityEngine.Canvas");
            if (canvasType != null) _canvas = _button.GetComponentInParent(canvasType);
        }
        ChangelogEditorPlugin.shouldShowChangelog.SettingChanged += UpdateButtonVisibility;
        UpdateButtonVisibility(null, EventArgs.Empty);
        foreach (ChangeLog changelog in startup.GetComponentsInChildren<ChangeLog>(true))
        {
            if (changelog.m_textField == null || changelog.m_changeLog == null) continue;
            _defaultText = changelog.m_changeLog.text;
            break;
        }
        RefreshContent();
        _visible = ChangelogEditorPlugin.forceShowChangelog.Value == ChangelogEditorPlugin.Toggle.On;
    }

    internal void Toggle()
    {
        if (!_visible) RefreshContent();
        _visible = !_visible;
    }

    private void UpdateButtonVisibility(object sender, EventArgs e)
    {
        if (_button != null)
            _button.SetActive(ChangelogEditorPlugin.shouldShowChangelog.Value == ChangelogEditorPlugin.Toggle.On);
    }

    internal void RefreshContent()
    {
        string customText = NormalizeText(ChangelogEditorPlugin.customFileText);
        if (ChangelogEditorPlugin.overrideText.Value == ChangelogEditorPlugin.Toggle.On)
            _text = customText;
        else if (ChangelogEditorPlugin.shouldChangeText.Value == ChangelogEditorPlugin.Toggle.On)
            _text = customText + _defaultText;
        else
            _text = _defaultText;
        _scroll = Vector2.zero;
    }

    internal static string NormalizeText(string text)
    {
        string decoded = System.Text.RegularExpressions.Regex.Replace(text,
            @"\\u([0-9a-fA-F]{4})|\\r\\n|\\[rnt]", match =>
            {
                if (match.Groups[1].Success)
                    return ((char)int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture)).ToString();
                switch (match.Value)
                {
                    case @"\r\n":
                    case @"\r":
                    case @"\n":
                        return "\n";
                    case @"\t":
                        return "\t";
                    default:
                        return match.Value;
                }
            });
        return decoded.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private void OnGUI()
    {
        if (_startup == null) return;
        if (_textStyle == null)
            _textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 16 };

        int previousDepth = GUI.depth;
        GUI.depth = -100;
        try
        {
            if (!_visible) return;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _visible = false;
                Event.current.Use();
                return;
            }

            float configuredWidth = ChangelogEditorPlugin.changelogWidth.Value;
            if (float.IsNaN(configuredWidth) || float.IsInfinity(configuredWidth)) configuredWidth = 445f;
            float width = Mathf.Min(Mathf.Max(250f, configuredWidth), Mathf.Max(100f, Screen.width - 20f));
            float bottom = GetButtonAreaTop() - 12f;
            float height = Mathf.Min(650f, Mathf.Max(80f, bottom - 20f));
            float left = Mathf.Clamp(30f, 10f, Mathf.Max(10f, Screen.width - width - 10f));
            _windowRect = new Rect(left, Mathf.Max(10f, bottom - height), width, height);
            GUI.Window(GetInstanceID(), _windowRect, DrawWindow, ChangelogEditorPlugin.topicText.Value);
        }
        finally
        {
            GUI.depth = previousDepth;
        }
    }

    private float GetButtonAreaTop()
    {
        Camera camera = null;
        if (_canvas != null && Convert.ToInt32(AccessTools.Property(_canvas.GetType(), "renderMode").GetValue(_canvas, null)) != 0)
            camera = AccessTools.Property(_canvas.GetType(), "worldCamera").GetValue(_canvas, null) as Camera;

        float top = Screen.height;
        bool found = false;
        foreach (Button button in _menuButtons)
        {
            if (button == null || (!button.gameObject.activeInHierarchy && button.gameObject != _button)) continue;
            RectTransform rect = button.transform as RectTransform;
            if (rect == null) continue;
            rect.GetWorldCorners(_buttonCorners);
            foreach (Vector3 corner in _buttonCorners)
            {
                Vector3 screenPoint = camera != null ? camera.WorldToScreenPoint(corner) : corner;
                top = Mathf.Min(top, Screen.height - screenPoint.y);
            }
            found = true;
        }
        return found ? top : Mathf.Max(100f, Screen.height - 180f);
    }

    private void DrawWindow(int id)
    {
        if (GUI.Button(new Rect(10f, 24f, _windowRect.width - 20f, 28f), "Close"))
        {
            _visible = false;
        }
        GUILayout.BeginArea(new Rect(10f, 60f, _windowRect.width - 20f, Mathf.Max(1f, _windowRect.height - 70f)));
        _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.ExpandHeight(true));
        GUILayout.Label(_text, _textStyle, GUILayout.Width(Mathf.Max(60f, _windowRect.width - 48f)));
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        ChangelogEditorPlugin.shouldShowChangelog.SettingChanged -= UpdateButtonVisibility;
        if (_button != null) _button.SetActive(_originalButtonActive);
        if (ReferenceEquals(Instance, this)) Instance = null!;
        _startup = null!;
    }

}
