using System;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace ThumbGen
{
    internal class OverlayAdornerHelper : IDisposable
    {
        private UIElement m_adornedElement;
        public UIElement AdornedElement
        {
            get
            {
                return m_adornedElement;
            }
        }

        private OverlayAdorner m_adorner;

        public OverlayAdornerHelper(UIElement adornedElement, UIElement adorningElement)
        {
            m_adornedElement = adornedElement;
            m_adorner = new OverlayAdorner(adornedElement, adorningElement);
            AdornerLayer _layer = AdornerLayer.GetAdornerLayer(adornedElement);
            if (_layer != null)
            {
                _layer.Add(m_adorner);
            }
        }

        public static void RemoveAllAdorners(UIElement adornedElement)
        {
            if (adornedElement != null)
            {
                AdornerLayer _layer = AdornerLayer.GetAdornerLayer(adornedElement);
                if (_layer != null)
                {
                    Adorner[] _toRemoveArray = _layer.GetAdorners(adornedElement);
                    if (_toRemoveArray != null)
                    {
                        for (int _x = 0; _x < _toRemoveArray.Length; _x++)
                        {
                            _layer.Remove(_toRemoveArray[_x]);
                        }
                    }
                }
            }
        }

        #region IDisposable Member

        public void Dispose()
        {
            AdornerLayer _layer = AdornerLayer.GetAdornerLayer(m_adornedElement);
            if (_layer != null)
            {
                _layer.Remove(m_adorner);
            }
        }

        #endregion
    }

    internal class OverlayAdorner : Adorner
    {
        UIElement m_adorningElement;

        public OverlayAdorner(UIElement adornedElement, UIElement adorningElement)
            : base(adornedElement)
        {
            m_adorningElement = adorningElement;
            AddLogicalChild(m_adorningElement);
            AddVisualChild(m_adorningElement);
        }

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) { return m_adorningElement; }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (m_adorningElement != null)
            {
                m_adorningElement.Arrange(new Rect(finalSize));
            }
            return finalSize;

        }
    }
}
