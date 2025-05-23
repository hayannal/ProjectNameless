﻿using UnityEngine;

namespace FIMSpace.FEditor
{
    /// <summary>
    /// FM: Class with predefined styles for custom inspectors etc.
    /// </summary>
    public class FEditor_StylesIn
    {
        public static GUIStyle GrayBackground
        {
            get { return Style(new Color32(128, 128, 127, 76)); }
        }

        public static GUIStyle LGrayBackground
        {
            get { return Style(new Color32(128, 128, 127, 36)); }
        }

        public static GUIStyle LBlueBackground
        {
            get { return Style(new Color32(0, 128, 255, 12)); }
        }

        public static GUIStyle LNavy
        {
            get { return Style(new Color32(167, 228, 243, 44)); }
        }

        public static GUIStyle Emerald
        {
            get { return Style(new Color32(0, 200, 100, 44)); }
        }

        public static GUIStyle GreenBackground
        {
            get { return Style(new Color32(0, 225, 86, 45)); }
        }

        public static GUIStyle BlueBackground
        {
            get { return Style(new Color32(0, 128, 255, 76)); }
        }

        public static GUIStyle RedBackground
        {
            get { return Style(new Color32(225, 72, 72, 45)); }
        }

        public static GUIStyle YellowBackground
        {
            get { return Style(new Color32(225, 244, 11, 45)); }
        }

        /// <summary>
        /// Creating simple style with background color
        /// </summary>
        public static GUIStyle Style(Color bgColor)
        {
            GUIStyle newStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(-1, -1, -1, -1)
            };

            // Textures is array of colors (pixels) and we're creating 1x1 texture
            Color[] solidColor = new Color[1];
            solidColor[0] = bgColor;

            // Creating one pixel texture with choosed color
            Texture2D bg = new Texture2D(1, 1);
            bg.SetPixels(solidColor);
            bg.Apply();

            // Applying new background image
            newStyle.normal.background = bg;

            return newStyle;
        }
    }
}