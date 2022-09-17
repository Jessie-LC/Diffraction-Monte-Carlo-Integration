using System.Collections.Generic;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewData
{
    internal class TextureSizeValues : List<TextureSizeValues.Item>
    {
        public TextureSizeValues()
        {
            Add(new Item(128));
            Add(new Item(256));
            Add(new Item(512));
            Add(new Item(1024));
            Add(new Item(2048));
        }

        public class Item
        {
            public string Text => Value.ToString();
            public int Value {get; set;}


            public Item(int value)
            {
                Value = value;
            }
        }
    }
}
