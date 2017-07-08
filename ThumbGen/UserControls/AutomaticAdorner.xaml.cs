using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Threading;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for AutomaticAdorner.xaml
    /// </summary>
    public partial class AutomaticAdorner : UserControl
    {
        public AutomaticAdorner()
        {
            InitializeComponent();
        }

        public AutomaticAdorner(Control btn, int seconds)
            : this()
        {
            m_AdornedElement = btn;
            m_Cnt = seconds;
            tbCounter.Text = m_Cnt.ToString();
            if (m_AdornedElement != null)
            {
                CountdownTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, CountdownTimer_Tick, m_AdornedElement.Dispatcher);
                CountdownTimer.Start();
            }
        }

        private Control m_AdornedElement;
        public DispatcherTimer CountdownTimer { get; private set; }
        private int m_Cnt;


        void CountdownTimer_Tick(object sender, EventArgs e)
        {
            CountdownTimer.Stop();
            try
            {

                tbCounter.Text = m_Cnt.ToString();

                //this.Visibility = this.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                this.TheGrid.Visibility = this.TheGrid.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                this.TheGrid.Opacity = this.TheGrid.Visibility == Visibility.Visible ? 1.0 : 0.0;

                if (m_Cnt == 0)
                {
                    if (m_AdornedElement != null)
                    {
                        CountdownTimer.Stop();

                        if (m_AdornedElement is Button)
                        {
                            m_AdornedElement.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        if (m_AdornedElement is Fluent.Button)
                        {
                            m_AdornedElement.RaiseEvent(new RoutedEventArgs(Fluent.Button.ClickEvent));
                        }
                    }
                }
                m_Cnt--;
            }
            finally
            {
                if (m_Cnt >= 0)
                {
                    CountdownTimer.Start();
                }
            }
        }

    }


    public class AutomaticAdornerHelper
    {
        OverlayAdornerHelper m_Helper;
        AutomaticAdorner m_Adorner;

        public AutomaticAdornerHelper(Control btn, int seconds)
        {
            m_Adorner = new AutomaticAdorner(btn, seconds);
            m_Helper = new OverlayAdornerHelper(btn, m_Adorner);
        }

        public void Cancel()
        {
            try
            {
                if (m_Adorner != null)
                {
                    if (m_Adorner.CountdownTimer != null)
                    {
                        m_Adorner.CountdownTimer.Stop();
                    }
                    if (m_Helper.AdornedElement != null)
                    {
                        OverlayAdornerHelper.RemoveAllAdorners(m_Helper.AdornedElement);
                    }
                }
            }
            catch { }
        }
    }
}
