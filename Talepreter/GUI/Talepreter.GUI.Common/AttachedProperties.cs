using System.Windows.Controls;
using System.Windows;

namespace Talepreter.GUI.Common
{
    public class AttachedProperties : DependencyObject
    {
        public static bool GetHeaderEditor(DependencyObject obj)
        {
            return (bool)obj.GetValue(HeaderEditorProperty);
        }

        public static void SetHeaderEditor(DependencyObject obj, bool value)
        {
            obj.SetValue(HeaderEditorProperty, value);
        }

        public static bool GetDisableFocusBorder(DependencyObject obj)
        {
            return (bool)obj.GetValue(DisableFocusBorderProperty);
        }

        public static void SetDisableFocusBorder(DependencyObject obj, bool value)
        {
            obj.SetValue(DisableFocusBorderProperty, value);
        }

        public static bool GetSetReadOnly(DependencyObject obj)
        {
            return (bool)obj.GetValue(SetReadOnlyProperty);
        }

        public static void SetSetReadOnly(DependencyObject obj, bool value)
        {
            obj.SetValue(SetReadOnlyProperty, value);
        }

        private static void SetReadOnlyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is not ComboBox) return;

            if (Convert.ToBoolean(e.NewValue) && !Convert.ToBoolean(e.OldValue)) ((ComboBox)o).PreviewKeyDown += ComboBoxPreviewKeyDown;
            else if (!Convert.ToBoolean(e.NewValue) && Convert.ToBoolean(e.OldValue)) ((ComboBox)o).PreviewKeyDown -= ComboBoxPreviewKeyDown;
        }

        private static void ComboBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if (combo == null) return;
            if (combo.IsReadOnly) e.Handled = true;
        }

        public static bool GetShowTabHeaders(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowTabHeadersProperty);
        }

        public static void SetShowTabHeaders(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowTabHeadersProperty, value);
        }

        public static readonly DependencyProperty SetReadOnlyProperty = DependencyProperty.RegisterAttached("SetReadOnly", typeof(bool), typeof(AttachedProperties), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(SetReadOnlyChanged)));
        public static readonly DependencyProperty DisableFocusBorderProperty = DependencyProperty.RegisterAttached("DisableFocusBorder", typeof(bool), typeof(AttachedProperties), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty HeaderEditorProperty = DependencyProperty.RegisterAttached("HeaderEditor", typeof(bool), typeof(AttachedProperties), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ShowTabHeadersProperty = DependencyProperty.RegisterAttached("ShowTabHeaders", typeof(bool), typeof(AttachedProperties), new PropertyMetadata(false));
    }
}
