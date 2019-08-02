namespace BeatSync
{
    internal class PluginConfig
    {
        public bool RegenerateConfig = true;
        public bool ExampleBoolSetting = false;
        public int ExampleIntSetting = 5;
        public float[] ExampleColorSetting = { 0, 0, 1, 1 };
        public int ExampleTextSegment = 0; // Index from the string array
        public string ExampleStringSetting = "example";
        public float ExampleSliderSetting = 2f;
        public float ExampleListSetting = 3f;
    }
}
