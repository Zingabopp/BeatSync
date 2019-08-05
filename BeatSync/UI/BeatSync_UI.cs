using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomUI.Settings;
using CustomUI.GameplaySettings;

namespace BeatSync.UI
{
    class BeatSync_UI
    {
        public static void CreateUI()
        {
            CreateSettingsUI();
            CreateGameplayOptionsUI();
        }

        /// <summary>
        /// This is the code used to create a submenu in Beat Saber's Settings menu.
        /// </summary>
        public static void CreateSettingsUI()
        {
            ////This will create a menu tab in the settings menu for your plugin
            //var pluginSettingsSubmenu = SettingsUI.CreateSubMenu("BeatSync");

            //// Example code for creating a true/false toggle button 
            //var exampleToggle = pluginSettingsSubmenu.AddBool("Bool Example Name", "Example mouse over description.");
            //exampleToggle.GetValue += delegate { return Plugin.ExampleBoolSetting; };
            //exampleToggle.SetValue += delegate (bool value)
            //{
            //    // This code is run when the toggle is toggled.
            //    Plugin.ExampleBoolSetting = value;
            //};

            //// Example code for creating an integer setting.
            //int exampleIntMin = 0;
            //int exampleIntMax = 10;
            //int exampleIntIncrement = 1;
            //var exampleInt = pluginSettingsSubmenu.AddInt("Int Example Name", "Int example mouse over description", exampleIntMin, exampleIntMax, exampleIntIncrement);
            //exampleInt.GetValue += delegate { return Plugin.ExampleIntSetting; };
            //exampleInt.SetValue += delegate (int value)
            //{
            //    Plugin.ExampleIntSetting = value;
            //};


            //// Creates a setting with segments of text you can click on (doesn't scroll, all options are shown at once)
            ////            Index stored in the config:      0      1      2
            //string[] textSegmentOptions = new string[] { "ex1", "ex2", "ex3" };
            //var textSegmentsExample = pluginSettingsSubmenu.AddTextSegments("Example Text Seg", "Text segments example mouse over description", textSegmentOptions);
            //textSegmentsExample.GetValue += delegate { return Plugin.ExampleTextSegment; };
            //textSegmentsExample.SetValue += delegate (int value)
            //{
            //    Plugin.ExampleTextSegment = value;
            //};

            //// Example code to create a setting where you can enter text with the in-game keyboard.
            //var exampleString = pluginSettingsSubmenu.AddString("Example string", "String example mouse over description");
            //exampleString.GetValue += delegate { return Plugin.ExampleStringSetting; };
            //exampleString.SetValue += delegate (string value)
            //{
            //    Plugin.ExampleStringSetting = value;
            //};

            //// Creates a submenu inside your settings for organization.
            //var exampleSubMenu = pluginSettingsSubmenu.AddSubMenu("Example SubMenu", "Example SubMenu mouse over description", true);

            //// Creates a color picker inside the previously created SubMenu.
            //var exampleColorPick = exampleSubMenu.AddColorPicker("Example Color Picker", "Color picker example mouse over description", Plugin.ExampleColorSetting);
            //exampleColorPick.GetValue += delegate { return Plugin.ExampleColorSetting; };
            //exampleColorPick.SetValue += delegate (UnityEngine.Color value)
            //{
            //    Plugin.ExampleColorSetting = value;
            //};

            //// Creates a slider setting. 
            //float sliderMin = 0;
            //float sliderMax = 10;
            //float sliderIncrement = 1;
            //bool intValues = false; // Setting to true will show integer values on the slider.
            //var exampleSlider = exampleSubMenu.AddSlider("Example Slider", "Slider example mouse over description", sliderMin, sliderMax, sliderIncrement, intValues);
            //exampleSlider.GetValue += delegate { return Plugin.ExampleSliderSetting; };
            //exampleSlider.SetValue += delegate (float value)
            //{
            //    Plugin.ExampleSliderSetting = value;
            //};

            //// Creates a list setting you can scroll through with backwards and forwards buttons.
            //float[] floatValues = { 0f, 1f, 2f, 3f, 4f };
            //string[] textValues = { "ex0", "ex1", "ex2", "ex3", "ex4" };
            //var exampleList = exampleSubMenu.AddList("Example List", floatValues, "List example mouse over description");
            //exampleList.GetValue += delegate { return Plugin.ExampleListSetting; };
            //exampleList.SetValue += delegate (float value)
            //{
            //    Plugin.ExampleSliderSetting = value;
            //};
            //// Shows strings as the values instead of the float that's actually stored.
            //exampleList.GetTextForValue = (val) =>
            //{
            //    return textValues[(int)val];
            //};
        }

        /// <summary>
        /// This is the code used to create a submenu in Beat Saber's Gameplay Settings panel.
        /// </summary>
        public static void CreateGameplayOptionsUI()
        {
            //string pluginName = "BeatSync";
            //string parentMenu = "MainMenu";
            //string subMenuName = "ExamplePluginMenu"; // Name of SubMenu, pass this to the settings you want to have in this menu.
            ////Example submenu option
            //var pluginSubmenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.ModifiersLeft, pluginName, parentMenu, subMenuName, "You can keep all your plugin's gameplay options nested within this one button");

            ////Example Toggle Option within a submenu
            //var exampleToggle = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Example Toggle", subMenuName, "Put a toggle for a setting you want easily accessible in game here.");
            //exampleToggle.GetValue = /* Fetch the initial value for the option here*/ false;
            //exampleToggle.OnToggle += (value) =>
            //{
            //    /*  You can execute whatever you want to occur when the value is toggled here, usually that would include updating wherever the value is pulled from   */
            //    Logger.log.Debug($"Toggle is {(value ? "On" : "Off")}");
            //};
            //exampleToggle.AddConflict("Conflicting Option Name"); //You can add conflicts with other gameplay options settings here, preventing both from being active at the same time, including that of other mods

            //// Creates a list setting you can scroll through with backwards and forwards buttons.
            //float[] floatValues = { 0f, 1f, 2f, 3f, 4f };
            //string[] textValues = { "ex0", "ex1", "ex2", "ex3", "ex4" };
            //var exampleList = GameplaySettingsUI.CreateListOption(GameplaySettingsPanels.ModifiersLeft, "Example List", subMenuName, "List example mouse over description");
            //exampleList.GetValue += delegate { return Plugin.ExampleGameplayListSetting; };
            //for (int i = 0; i < 5; i++)
            //    exampleList.AddOption(i, textValues[i]);
            //exampleList.OnChange += (value) =>
            //{
            //    // Execute code based on what value is selected.
            //    Plugin.ExampleGameplayListSetting = value;
            //    Logger.log.Debug($"Example GameplaySetting List value changed to {textValues[(int)value]}");
            //};
        }

    }


}

namespace BeatSync
{
    /// <summary>
    /// Adds .ToFloatAry() method to instances of the UnityEngine.Color class, and .ToColor() to float arrays.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Converts a color to a float array. Ex: float[] colorAry = color.ToFloat()
        /// </summary>
        /// <param name="color">Color to convert to a float array</param>
        /// <returns>Float array with the color values</returns>
        public static float[] ToFloatAry(this UnityEngine.Color color)
        {
            return new float[] { color.r, color.g, color.b, color.a };
        }

        /// <summary>
        /// Converts a float array to a color. Ex: Color color = floatAry.ToColor()
        /// </summary>
        /// <param name="rgbaVals">Float[4] array with the values [Red, Green, Blue, Alpha] </param>
        /// <returns>A UnityEngine.Color</returns>
        public static UnityEngine.Color ToColor(this float[] rgbaVals)
        {
            return new UnityEngine.Color(rgbaVals[0], rgbaVals[1], rgbaVals[2], rgbaVals[3]);
        }
    }
}