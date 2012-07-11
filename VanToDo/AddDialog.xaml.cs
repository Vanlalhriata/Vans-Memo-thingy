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
using System.Windows.Shapes;

namespace VanToDo
{
    /// <summary>
    /// Interaction logic for AddDialog.xaml
    /// </summary>
    public partial class AddDialog : Window
    {
        public AddDialog(ref ToDoItem item)
        {
            InitializeComponent();

            this.DataContext = item;
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void DescriptionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                if (Keyboard.IsKeyDown(Key.Enter))
                {
                    e.Handled = true;
                    int savedIndex = DescriptionTextBox.CaretIndex;
                    DescriptionTextBox.Text = DescriptionTextBox.Text.Insert(DescriptionTextBox.CaretIndex, "\r");
                    DescriptionTextBox.CaretIndex = savedIndex + 1;
                }
        }
    }
}
