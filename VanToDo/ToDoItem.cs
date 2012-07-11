using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VanToDo
{
    /// <summary>
    /// List item used in VanToDo
    /// </summary>
    public class ToDoItem
    {
        #region Public properties

        public Guid Id { set; get; }
        public int Index { set; get; }
        public string Title { set; get; }
        public string Description { set; get; }
        public DateTime? DueDateTime { set; get; }
        public bool Done { set; get; }
        
        #endregion

        // Constructors
        public ToDoItem(int Index, string Title, string Description, DateTime? DueDateTime)
        {
            this.Id = Guid.NewGuid();
            this.Index = Index;
            this.Title = Title;
            this.Description = Description;
            this.DueDateTime = DueDateTime;
            this.Done = false;
        }

        public ToDoItem()
        {
            this.Id = Guid.NewGuid();
            this.Title = "";
            this.Description = "";
            this.DueDateTime = null;
            this.Done = false;
        }
    }
}
