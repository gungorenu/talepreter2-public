using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Collections.ObjectModel;

namespace Talepreter.GUI.Common.Controls
{
    public class FormattedRichTextBox : RichTextBox
    {
        public FormattedRichTextBox()
        {
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // unregister from collection
            if (e.OldValue != null && e.OldValue is ObservableCollection<string> v) v.CollectionChanged -= OnDataCollectionChanged;

            try
            {
                ToolTip = null;
                Document.Blocks.Clear();
                // string is simple, just instantiate a paragraph continuously
                if (e.NewValue is string str)
                    Document.Blocks.Add(FormattedTextHelper.FormParagraph(e.NewValue.ToString()!));
                // for observable collection, register to it, and then instantiate by items one by one and each entry is a paragraph
                else if (e.NewValue is ObservableCollection<string> collection)
                {
                    collection.CollectionChanged += OnDataCollectionChanged;
                    foreach (var entry in collection)
                    {
                        if (ReverseOrder && Document.Blocks.Count != 0) Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, FormattedTextHelper.FormParagraph(entry));
                        else Document.Blocks.Add(FormattedTextHelper.FormParagraph(entry));
                    }
                }
                // DO-NOT-THROW: because in first call we get the event but for an unsupported one (owner), next call shall be correct if binding set properly
                //else throw new InvalidOperationException($"Formatted Text Box cannot bind to the given data type {e.NewValue.GetType().Name}: {e.NewValue}");
                else ToolTip = null;
            }
            catch (Exception ex)
            {
                // maybe a better way of giving error
                var r = new Run("ERROR");
                r.SetResourceReference(TextElement.ForegroundProperty, "FTBC8");
                var prg = new Paragraph();
                prg.Inlines.Add(r);
                ToolTip = ex.Message + "\r\n" + e.NewValue.ToString()!;
                Document.Blocks.Add(prg);
            }
        }

        private void OnDataCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (e.NewItems != null)
                {
                    string entry = "";
                    try
                    {
                        ToolTip = null;
                        foreach (string k in e.NewItems)
                        {
                            entry = k;
                            if (ReverseOrder && Document.Blocks.Count != 0) Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, FormattedTextHelper.FormParagraph(entry));
                            else Document.Blocks.Add(FormattedTextHelper.FormParagraph(entry));
                        }
                    }
                    catch (Exception ex)
                    {
                        // maybe a better way of giving error
                        var r = new Run("ERROR");
                        r.SetResourceReference(TextElement.ForegroundProperty, "FTBC8");
                        var prg = new Paragraph();
                        prg.Inlines.Add(r);
                        ToolTip = ex.Message + "\r\n" + entry.ToString()!;
                        Document.Blocks.Add(prg);
                    }
                }

                var collection = (ObservableCollection<string>)DataContext;
                if (collection.Count == 0)
                {
                    ToolTip = null;
                    Document.Blocks.Clear();
                }
                else
                {
                    if (e.OldItems != null)
                    {
                        var prgs = Document.Blocks.Where(x => x is Paragraph).ToArray();
                        foreach (string entry in e.OldItems)
                        {
                            var prg = prgs.FirstOrDefault(x => entry == x.Tag.ToString());
                            if (prg != null) Document.Blocks.Remove(prg);
                        }
                    }
                }
            });
        }

        public bool ReverseOrder
        {
            get { return (bool)GetValue(ReverseOrderProperty); }
            set { SetValue(ReverseOrderProperty, value); }
        }
        public static readonly DependencyProperty ReverseOrderProperty = DependencyProperty.Register(nameof(ReverseOrder), typeof(bool), typeof(FormattedRichTextBox), new PropertyMetadata(false));
    }
}
