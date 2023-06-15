using System;
using System.Windows;
using System.Windows.Input;

namespace Bikerental
{ 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            frame.NavigationService.Navigate(new Uri("Page1.xaml", UriKind.Relative));
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            DragMove();
        }
    }
}