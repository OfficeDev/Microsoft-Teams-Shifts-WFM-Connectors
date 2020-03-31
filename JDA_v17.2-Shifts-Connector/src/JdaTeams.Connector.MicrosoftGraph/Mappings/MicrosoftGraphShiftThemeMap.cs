using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph.Mappings
{
    public class MicrosoftGraphShiftThemeMap : IShiftThemeMap
    {
        private readonly Dictionary<Color, string> _teamsColours;
        private readonly ConcurrentDictionary<string, string> _themeMap;

        public MicrosoftGraphShiftThemeMap(MicrosoftGraphOptions options)
        {
            var builder = new DbConnectionStringBuilder();
            if(!string.IsNullOrEmpty(options.ThemeMap))
            {
                builder.ConnectionString = options.ThemeMap;
            }
            _themeMap = new ConcurrentDictionary<string, string>();
            foreach(var key in builder.Keys)
            {
                _themeMap[key.ToString()] = builder[key.ToString()].ToString();
            }

            _teamsColours = new Dictionary<Color, string>
            {
                { HexToColour("#a0aeb2"), "white" },          // light: #eceff0, dark: #a0aeb2
                { HexToColour("#0078d7"), "blue" },           // light: #ccedf7, dark: #0078d7
                { HexToColour("#8cbd18"), "green" },          // light: #e8f2d1, dark: #8cbd18
                { HexToColour("#8378de"), "purple" },         // light: #e6e4f8, dark: #8378de
                { HexToColour("#ed616f"), "pink" },           // light: #ffd9d9, dark: #ed616f
                { HexToColour("#ffb900"), "yellow" },         // light: #fff1cc, dark: #ffb900
                { HexToColour("#393939"), "gray" },           // light: #d7d7d7, dark: #393939
                { HexToColour("#004e8c"), "darkBlue" },       // light: #ccdce8, dark: #004e8c
                { HexToColour("#498205"), "darkGreen" },      // light: #dbe6cd, dark: #498205
                { HexToColour("#4e257f"), "darkPurple" },     // light: #dcd3e5, dark: #4e257f
                { HexToColour("#a4262c"), "darkPink" },       // light: #edd4d5, dark: #a4262c
                { HexToColour("#ffaa44"), "darkYellow" },     // light: #ffeeda, dark: #ffaa44
            };
        }

        public string MapTheme(string themeCode)
        {
            var fallbackColour = "white";

            if(string.IsNullOrEmpty(themeCode))
            {
                return fallbackColour;
            }

            // it has been observed that DbConnectionStringBuilder lower cases the key values
            // so ensure matches by lower casing the themeCode 
            if (_themeMap.TryGetValue(themeCode.ToLower(), out string value))
            {
                return value;
            }

            // try to match the source colour to one of the teams colours
            try
            {
                Color sourceColour = HexToColour(themeCode);
                var closestColour = GetClosestColour(_teamsColours.Keys.ToList(), sourceColour);
                if (_teamsColours.TryGetValue(closestColour, out string teamsColour))
                {
                    // add to the dictionary so we don't have to compute this again this session
                    _themeMap[themeCode] = teamsColour;
                    return teamsColour;
                }

                return fallbackColour;
            }
            catch
            {
                // there has been an error (possibly the colour code is not valid), 
                // so just return the fall back colour
                return fallbackColour;
            }
        }

        private Color HexToColour(string hexColour)
        {
            var argbColour = (int)new Int32Converter().ConvertFromString(hexColour);
            return Color.FromArgb(argbColour);
        }

        #region Colour Mapping functionality courtesy of StackOverflow

        // https://stackoverflow.com/questions/27374550/how-to-compare-color-object-and-get-closest-color-in-an-color

        // weighed distance using hue, saturation and brightness
        private Color GetClosestColour(List<Color> colors, Color target)
        {
            float hue1 = target.GetHue();
            var num1 = ColorNum(target);
            var diffs = colors.Select(n => Math.Abs(ColorNum(n) - num1) + GetHueDistance(n.GetHue(), hue1));
            var diffMin = diffs.Min(x => x);

            return colors[diffs.ToList().FindIndex(n => n == diffMin)];
        }

        //  weighed only by saturation and brightness
        private float ColorNum(Color c)
        {
            var factorSat = 100;
            var factorBri = 100;
            return c.GetSaturation() * factorSat + GetBrightness(c) * factorBri;
        }

        private float GetBrightness(Color c)
        {
            return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f) / 256f;
        }

        // distance between two hues:
        private float GetHueDistance(float hue1, float hue2)
        {
            float d = Math.Abs(hue1 - hue2);
            return d > 180 ? 360 - d : d;
        }

        #endregion
    }
}
