using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace Talepreter.GUI.Common.Controls
{
    public class FormattedTextBlock : TextBlock
    {
        public FormattedTextBlock()
        {
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                Inlines.Clear();
                // string is simple, just instantiate a span recursively
                if (e.NewValue is string str) Inlines.Add(FormattedTextHelper.FormSpan(e.NewValue.ToString()!));
                // DO-NOT-THROW: because in first call we get the event but for an unsupported one (owner), next call shall be correct if binding set properly
                //else throw new InvalidOperationException($"Formatted Text Block cannot bind to the given data type {e.NewValue.GetType().Name}: {e.NewValue}");
                else ToolTip = null;
            }
            catch (Exception ex)
            {
                // maybe a better way of giving error
                var r = new Run("ERROR");
                r.SetResourceReference(TextElement.ForegroundProperty, "FTBC8");
                Inlines.Add(r);
                ToolTip = ex.Message + "\r\n" + e.NewValue.ToString()!;
            }
        }
    }
}
