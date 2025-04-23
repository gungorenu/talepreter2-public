using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Talepreter.GUI.Common.Controls
{
    /// <summary>
    /// Arrow direction for arrow control
    /// </summary>
    public enum ArrowDirection
    {
        /// <summary>
        /// Left 
        /// </summary>
        Left,
        /// <summary>
        /// Right
        /// </summary>
        Right,
        /// <summary>
        /// Up
        /// </summary>
        Up,
        /// <summary>
        /// Down
        /// </summary>
        Down
    }

    /// <summary>
    /// Simple control to draw arrows inside other controls (scrollbar, updown, ...)
    /// </summary>
    public sealed class Arrow : Control
    {
        #region Dependency Properties

        #region Declerations
        /// <summary>
        /// Arrow direction property
        /// </summary>
        public static readonly DependencyProperty DirectionProperty;

        /// <summary>
        /// Arrow brush property
        /// </summary>
        public static readonly DependencyProperty ArrowBrushProperty;

        /// <summary>
        /// Arrow is triangle property
        /// </summary>
        public static readonly DependencyProperty IsTriangleProperty;
        #endregion

        #region Property Accessors

        /// <summary>
        /// Arrow brush to fill the arrow shape
        /// </summary>
        public Brush ArrowBrush
        {
            get { return (Brush)GetValue(ArrowBrushProperty); }
            set { SetValue(ArrowBrushProperty, value); }
        }

        /// <summary>
        /// Flag, if set then will show triangle instead of arrows
        /// </summary>
        public bool IsTriangle
        {
            get { return (bool)GetValue(IsTriangleProperty); }
            set { SetValue(IsTriangleProperty, value); }
        }

        /// <summary>
        /// Arrow direction
        /// </summary>
        public ArrowDirection Direction
        {
            get { return (ArrowDirection)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        #endregion

        #endregion

        /// <summary>
        /// Static constructor
        /// </summary>
        static Arrow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Arrow), new FrameworkPropertyMetadata(typeof(Arrow)));
            DirectionProperty = DependencyProperty.Register("Direction", typeof(ArrowDirection), typeof(Arrow), new FrameworkPropertyMetadata(ArrowDirection.Up, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
            ArrowBrushProperty = DependencyProperty.Register("ArrowBrush", typeof(Brush), typeof(Arrow), new UIPropertyMetadata(Brushes.Black));
            IsTriangleProperty = DependencyProperty.Register("IsTriangle", typeof(bool), typeof(Arrow), new UIPropertyMetadata(false));
        }
    }
}
