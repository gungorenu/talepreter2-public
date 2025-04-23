namespace Talepreter.GUI.Common
{
    public class ComboboxItem
    {
        public ComboboxItem()
        {
        }
        public ComboboxItem(string text, object value)
        {
            Text = text;
            Value = value;
        }

        public string Text { get; init; } = default!;

        public object Value { get; init; } = default!;

        public override string ToString() => Text;
    }

    public class ComboboxItem<T>
    {
        public ComboboxItem(string text)
        {
            Text = text;
            Value = default!;
        }
        public ComboboxItem(string text, T value)
        {
            Text = text;
            Value = value;
        }

        public string Text { get; set; }

        public T Value { get; set; }

        public override string ToString() => Text;
    }
}
