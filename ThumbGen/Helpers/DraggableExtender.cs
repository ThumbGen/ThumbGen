using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ThumbGen
{
    public class DraggableExtender : DependencyObject
    {


        public static bool GetIsWatermark(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsWatermarkProperty);
        }

        public static void SetIsWatermark(DependencyObject obj, bool value)
        {
            obj.SetValue(IsWatermarkProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsWatermark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWatermarkProperty =
            DependencyProperty.RegisterAttached("IsWatermark", typeof(bool), typeof(DraggableExtender), new UIPropertyMetadata(true));



        // This is the dependency property we're exposing - we'll  
        // access this as DraggableExtender.CanDrag="true"/"false" 
        public static readonly DependencyProperty CanDragProperty =
            DependencyProperty.RegisterAttached("CanDrag",
            typeof(bool),
            typeof(DraggableExtender),
            new UIPropertyMetadata(false, OnChangeCanDragProperty));

        // The expected static setter 
        public static void SetCanDrag(UIElement element, bool o)
        {
            element.SetValue(CanDragProperty, o);
        }

        // the expected static getter 
        public static bool GetCanDrag(UIElement element)
        {
            return (bool)element.GetValue(CanDragProperty);
        }

        // This is triggered when the CanDrag property is set. We'll 
        // simply check the element is a UI element and that it is 
        // within a canvas. If it is, we'll hook into the mouse events 
        private static void OnChangeCanDragProperty(DependencyObject d,
                  DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;
            if (element == null) return;

            if (e.NewValue != e.OldValue)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseDown += element_PreviewMouseDown;
                    element.PreviewMouseUp += element_PreviewMouseUp;
                    element.PreviewMouseMove += element_PreviewMouseMove;
                }
                else
                {
                    element.PreviewMouseDown -= element_PreviewMouseDown;
                    element.PreviewMouseUp -= element_PreviewMouseUp;
                    element.PreviewMouseMove -= element_PreviewMouseMove;
                }
            }
        }

        // Determine if we're presently dragging 
        private static bool _isDragging = false;
        // The offset from the top, left of the item being dragged  
        // and the original mouse down 
        private static Point _offset;

        // This is triggered when the mouse button is pressed  
        // on the element being hooked 
        static void element_PreviewMouseDown(object sender,
                System.Windows.Input.MouseButtonEventArgs e)
        {
            // Ensure it's a framework element as we'll need to  
            // get access to the visual tree 
            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            // start dragging and get the offset of the mouse  
            // relative to the element 
            _isDragging = true;
            element.Cursor = Cursors.ScrollAll;
            _offset = e.GetPosition(element);
        }

        // This is triggered when the mouse is moved over the element 
        private static void element_PreviewMouseMove(object sender,
                  MouseEventArgs e)
        {
            // If we're not dragging, don't bother - also validate the element 
            if (!_isDragging) return;

            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            Canvas canvas = element.Parent as Canvas;
            if (canvas == null) return;

            Image image = null;
            if ((bool)element.GetValue(DraggableExtender.IsWatermarkProperty))
            {
                image = canvas.Children[1] as Image;
                if (image == null) return;
            }

            // Get the position of the mouse relative to the canvas 
            Point mousePoint = e.GetPosition(canvas);

            // Offset the mouse position by the original offset position 
            mousePoint.Offset(-_offset.X, -_offset.Y);

            mousePoint.X = Math.Max(0, mousePoint.X);
            mousePoint.Y = Math.Max(0, mousePoint.Y);

            if ((bool)element.GetValue(DraggableExtender.IsWatermarkProperty))
            {
                mousePoint.X = Math.Min(image.ActualWidth - element.ActualWidth, mousePoint.X);
                mousePoint.Y = Math.Min(image.ActualHeight - element.ActualHeight, mousePoint.Y);
            }
            // Move the element on the canvas 
            element.SetValue(Canvas.LeftProperty, mousePoint.X);
            element.SetValue(Canvas.TopProperty, mousePoint.Y);

            try
            {
                if ((bool)element.GetValue(DraggableExtender.IsWatermarkProperty))
                {
                    FileManager.Configuration.Options.WatermarkOptions.Position = new Size((int)mousePoint.X, (int)mousePoint.Y);
                }
            }
            catch { }
            
        }

        // this is triggered when the mouse is released 
        private static void element_PreviewMouseUp(object sender,
                MouseButtonEventArgs e)
        {
            _isDragging = false;
            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;
            element.Cursor = Cursors.Hand;
        }

    }
}

