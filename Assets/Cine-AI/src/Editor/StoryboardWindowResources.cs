using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class StoryboardWindowResources
{
    // GUIStyles
    public GUIStyle m_bigLabel;
    public GUIStyle m_smallLabel;
    public GUIStyle m_miniBoldLabel;
    public GUIStyle m_mediumLabel;
    public GUIStyle m_miniTitle;
    public GUIStyle m_nodeTitleLabel;
    public GUIStyle m_nodeFieldLabel;
    public GUIStyle m_lockButtonLockedStyle;
    public GUIStyle m_lockButtonUnlockedStyle;
    public Color m_colorBlackTransparent = new Color(0.0f, 0.0f, 0.0f, 0.5f);
    public Color m_colorWhiteTransparent = new Color(255, 255, 255, 0.5f);
    public Font m_editorFont1;
    public string m_EditorFont1Path = "EditorFonts/AnonymousPro-Bold";


    public void SetupResources()
    {
        // Load the font.
        m_editorFont1 = Resources.Load(m_EditorFont1Path) as Font;

        // Setup big label
        m_bigLabel = new GUIStyle();
        m_bigLabel.fontSize = 60;
        m_bigLabel.normal.textColor = m_colorBlackTransparent;

        // Mini bold label.
        m_miniBoldLabel = new GUIStyle();
        m_miniBoldLabel.fontSize = 12;
        m_miniBoldLabel.normal.textColor = Color.white;
        m_miniBoldLabel.fontStyle = FontStyle.Bold;

        // Small label
        m_smallLabel = new GUIStyle();
        m_smallLabel.fontSize = 14;
        m_smallLabel.normal.textColor = m_colorWhiteTransparent;

        // Setup small label.
        m_mediumLabel = new GUIStyle();
        m_mediumLabel.fontSize = 20;
        m_mediumLabel.normal.textColor = m_colorWhiteTransparent;

        // Setup mini title.
        m_miniTitle = new GUIStyle();
        m_miniTitle.fontSize = 15;
        m_miniTitle.normal.textColor = Color.white;
        m_miniTitle.alignment = TextAnchor.MiddleCenter;
        m_miniTitle.fontStyle = FontStyle.Bold;

        // Node title label
        m_nodeTitleLabel = new GUIStyle();
        m_nodeTitleLabel.fontSize = 19;

        Color textColor = new Color(1, 1, 1, 0.4f);
        ColorUtility.TryParseHtmlString("#A58961", out textColor);

        m_nodeTitleLabel.normal.textColor = textColor;
        m_nodeFieldLabel = new GUIStyle();
        m_nodeFieldLabel.normal.textColor = Color.white;

        // Lock button
        m_lockButtonUnlockedStyle = "Button";

        // Lock Button
        m_lockButtonLockedStyle = new GUIStyle(m_lockButtonUnlockedStyle);
        //m_lockButtonLockedStyle.normal.background = null;
        //m_lockButtonLockedStyle.normal.textColor = textColor;

        // Attach fonts.
        if (m_editorFont1 != null)
        {
            m_bigLabel.font = m_editorFont1;
            m_mediumLabel.font = m_editorFont1;
        }


    }
}
