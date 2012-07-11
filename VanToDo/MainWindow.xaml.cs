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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VanToDo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<ToDoItem> ToDoList { set; get; }

        private ListCollectionView collectionView;

        public MainWindow()
        {
            // Read the List from database
            using (ToDoEntities da = new ToDoEntities())
            {
                IEnumerable<ToDoItem> ToDoItems = from t in da.ToDoItems
                                                  select t;
                ToDoList = new ObservableCollection<ToDoItem>(ToDoItems);
            }

            InitializeComponent();

            // Sort the displayed list
            collectionView = CollectionViewSource.GetDefaultView(ToDoList) as ListCollectionView;
            collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Descending));

            // Search filter
            collectionView.Filter = delegate(object obj)
            {
                ToDoItem item = obj as ToDoItem;
                return IsContainingText(item, SearchTextBox.Text);
            };

            ToDoListBox.ItemsSource = collectionView;

            // Reset all indices
            int i = ToDoList.Count;
            foreach (ToDoItem item in collectionView)
            {
                using (ToDoEntities da = new ToDoEntities())
                {
                    ToDoItem currentItem = da.ToDoItems.FirstOrDefault<ToDoItem>(a => a.Id == item.Id);
                    if (currentItem != null)
                        currentItem.Index = --i;
                    da.SaveChanges();
                    item.Index = i;
                }
            }
            
        }

        #region Event handlers

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            ToDoItem item = new ToDoItem();
            AddDialog addDialog = new AddDialog(ref item);
            Nullable<bool> dialogResult = addDialog.ShowDialog();
            if (dialogResult == true)
            {
                //Get new Index
                if (ToDoList.Count == 0)
                    item.Index = 0;
                else
                {
                    var maxIndex = ToDoList.Max(m => m.Index);
                    item.Index = ++maxIndex;
                }

                //Get new GUID
                item.Id = Guid.NewGuid();

                //Save the item to db
                int rowsAffected = 0;
                using (ToDoEntities da = new ToDoEntities())
                {
                    da.ToDoItems.AddObject(item);
                    rowsAffected = da.SaveChanges();
                }

                // Save the item to the list
                if (rowsAffected != 0)
                    ToDoList.Add(item);
                else
                    MessageBox.Show("Error while accessing the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the deleted item
            ToDoItem item = ((Button)sender).DataContext as ToDoItem;

            // Delete the item from the database
            int rowsAffected = 0;
            using (ToDoEntities da = new ToDoEntities())
            {
                IQueryable<ToDoItem> toDelete = from t in da.ToDoItems
                                                where t.Id == item.Id
                                                select t;

                da.ToDoItems.DeleteObject(toDelete.Single());
                rowsAffected = da.SaveChanges();
            }

            // Delete the item from the list
            if (rowsAffected != 0)
                ToDoList.Remove(item);
            else
                MessageBox.Show("Error while accessing the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the item being edited
            ToDoItem item = ((Button)sender).DataContext as ToDoItem;

            // Call the Add/Edit window
            AddDialog addDialog = new AddDialog(ref item);
            Nullable<bool> dialogResult = addDialog.ShowDialog();

            // Save the edited item
            if (dialogResult == true)
            {
                //Save the changes to the database
                int rowsAffected = 0;
                using (ToDoEntities da = new ToDoEntities())
                {
                    ToDoItem daItem = da.ToDoItems.FirstOrDefault<ToDoItem>(p => p.Id == item.Id);
                    daItem.Index = item.Index;
                    daItem.Title = item.Title;
                    daItem.Description = item.Description;
                    daItem.DueDateTime = item.DueDateTime;
                    daItem.Done = item.Done;

                    rowsAffected = da.SaveChanges();
                }

                if (rowsAffected != 0)
                {
                    int index = ToDoList.IndexOf(item);
                    ToDoList[index] = item;
                }
                else
                    MessageBox.Show("Error while accessing the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void helpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.ShowDialog();
        }

        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ToDoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Record Changed 'Done' status to database
            if (e.AddedItems.Count != 0 || e.RemovedItems.Count != 0)
            {
                using (ToDoEntities da = new ToDoEntities())
                {
                    foreach (object addedObject in e.AddedItems)
                    {
                        ToDoItem addedItem = da.ToDoItems.FirstOrDefault<ToDoItem>(p => p.Id == ((ToDoItem)addedObject).Id);
                        if (addedItem != null)
                            addedItem.Done = true;
                    }
                    foreach (object removedObject in e.RemovedItems)
                    {
                        ToDoItem removedItem = da.ToDoItems.FirstOrDefault<ToDoItem>(p => p.Id == ((ToDoItem)removedObject).Id);
                        if (removedItem != null)
                            removedItem.Done = false;
                    }
                    da.SaveChanges();
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #endregion

        #region DragDrop handlers

        // Avoid constant to and fro swiching by moving items only when mouse has entered original item again
        private bool canMoveItems = false;

        private void ToDoListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Button parent = sender as Button;
            ToDoItem item = GetItemFromPoint(parent, e.GetPosition(parent)) as ToDoItem;
            if (item != null)
                DragDrop.DoDragDrop(ToDoListBox, item, DragDropEffects.Move);
        }

        private void ToDoListBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            ListBox parent = sender as ListBox;
            ToDoItem item = e.Data.GetData(typeof(ToDoItem)) as ToDoItem;
            ToDoItem target = GetItemFromPoint(parent, e.GetPosition(parent)) as ToDoItem;

            if (item == target)
                canMoveItems = true;

            if (item != target && item != null && target != null && canMoveItems)
            {
                int itemIndex = int.Parse(item.Index.ToString());
                int targetIndex = int.Parse(target.Index.ToString());

                if (itemIndex < targetIndex)
                    for (int i = itemIndex + 1; i <= targetIndex; i++)
                    {
                        ToDoItem currentItem = ToDoList.FirstOrDefault<ToDoItem>(a => a.Index == i);
                        if (currentItem != null)
                            currentItem.Index--;

                        using (ToDoEntities da = new ToDoEntities())
                        {
                            currentItem = da.ToDoItems.FirstOrDefault<ToDoItem>(a => a.Index == i);
                            if (currentItem != null)
                                currentItem.Index--;
                            da.SaveChanges();
                        }
                    }
                else if (itemIndex > targetIndex)
                    for (int i = itemIndex - 1; i >= targetIndex; i--)
                    {
                        ToDoItem currentItem = ToDoList.FirstOrDefault<ToDoItem>(a => a.Index == i);
                        if (currentItem != null)
                            currentItem.Index++;

                        using (ToDoEntities da = new ToDoEntities())
                        {
                            currentItem = da.ToDoItems.FirstOrDefault<ToDoItem>(a => a.Index == i);
                            if (currentItem != null)
                                currentItem.Index++;
                            da.SaveChanges();
                        }
                    }

                ToDoList[ToDoList.IndexOf(item)].Index = targetIndex;
                using (ToDoEntities da = new ToDoEntities())
                {
                    ToDoItem currentItem = da.ToDoItems.FirstOrDefault<ToDoItem>(a => a.Id == item.Id);
                    if (currentItem != null)
                        currentItem.Index = targetIndex;
                    da.SaveChanges();
                }

                collectionView.Refresh();
                canMoveItems = false;
            }
        }

        private object GetItemFromPoint(UIElement source, Point point)
        {
            UIElement element = source.InputHitTest(point) as UIElement;
            //if (element != null)
            //{
            //    object data = DependencyProperty.UnsetValue;
            //    while (data == DependencyProperty.UnsetValue)
            //    {
            //        data = source.ItemContainerGenerator.ItemFromContainer(element);
            //        if (data == DependencyProperty.UnsetValue)
            //            element = VisualTreeHelper.GetParent(element) as UIElement;
            //        if (element == source)
            //            return null;
            //    }
            //    if (data != DependencyProperty.UnsetValue)
            //        return data;
            //}
            //return null;
            if (element != null)
            {
                while (element != null)
                {
                    element = VisualTreeHelper.GetParent(element) as UIElement;
                    if (element is ListBoxItem)
                        return ToDoListBox.ItemContainerGenerator.ItemFromContainer(element);
                }
            }
            return null;
        }

        #endregion

        #region Search handlers

        private bool IsContainingText(ToDoItem item, string searchString)
        {
            if (searchString == "")
                return true;

            if (item.Title != null)
                if (item.Title.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            if (item.Description != null)
                if (item.Description.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            
            return false;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            collectionView.Refresh();
        }

        private void clearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
        }

        #endregion
    }
}
