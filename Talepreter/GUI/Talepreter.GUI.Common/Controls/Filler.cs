using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Talepreter.GUI.Common.Controls
{
    /// <summary>
    /// Filler control
    /// </summary>
    public class Filler : Control
    {
        #region Dependency Property Definitions

        /// <summary>
        /// Is Pressed Property
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty;

        /// <summary>
        /// Corner Radius Property
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty;

        /// <summary>
        /// Pressed Background Property
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty;

        /// <summary>
        /// Is Mouse Over Property
        /// </summary>
        public new static readonly DependencyProperty IsMouseOverProperty;

        #endregion

        /// <summary>
        /// Static Constructor, defines dependency properties
        /// </summary>
        static Filler()
        {
            object False = false;

            DefaultStyleKeyProperty.OverrideMetadata(typeof(Filler), new FrameworkPropertyMetadata(typeof(Filler)));
            IsPressedProperty = DependencyProperty.Register("IsPressed", typeof(bool), typeof(Filler), new FrameworkPropertyMetadata(False, FrameworkPropertyMetadataOptions.AffectsRender));
            CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Filler), new FrameworkPropertyMetadata(new CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender));
            PressedBackgroundProperty = DependencyProperty.Register("PressedBackground", typeof(Brush), typeof(Filler), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
            IsMouseOverProperty = DependencyProperty.Register("IsMouseOver", typeof(bool), typeof(Filler), new FrameworkPropertyMetadata(False, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        #region Properties

        /// <summary>
        /// Corner radius value
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Is pressed
        /// </summary>
        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        /// <summary>
        /// Background on pressed state
        /// </summary>
        public Brush PressedBackground
        {
            get { return (Brush)GetValue(PressedBackgroundProperty); }
            set { SetValue(PressedBackgroundProperty, value); }
        }

        /// <summary>
        /// Is mouse over flag
        /// </summary>
        public new bool IsMouseOver
        {
            get { return (bool)GetValue(IsMouseOverProperty); }
            set { SetValue(IsMouseOverProperty, value); }
        }

        #endregion
    }
}
