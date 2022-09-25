using System.Collections.Generic;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewData;

internal class PresetValues : List<PresetValues.Item>
{
    public PresetValues()
    {
        Add(new Item("Default") {
            WavelengthCount = null,
            Quality = 1f,
            Radius = null,
            Scale = null,
            Distance = null,
            BladeCount = null,
        });

        Add(new Item("Full") {
            WavelengthCount = 441,
            Quality = 10f,
            Radius = null,
            Scale = null,
            Distance = null,
            BladeCount = null,
        });
    }

    public class Item
    {
        public string Name {get;}
        public int? WavelengthCount {get; set;}
        public float Quality {get; set;}
        public float? Radius {get; set;}
        public float? Scale {get; set;}
        public float? Distance {get; set;}
        public int? BladeCount {get; set;}


        public Item(string name)
        {
            Name = name;
        }
    }
}
