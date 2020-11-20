using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTable
{
    public class DragDropOptions
    {
        /// <summary>
        /// Возможность перемещения
        /// </summary>
        public bool IsDrag = true;

        /// <summary>
        /// Тип элементов для броска
        /// </summary>
        public DropTypes DropType = DropTypes.Local;

        /// <summary>
        /// Возможность броска файлов
        /// </summary>
        public bool IsDropFiles;

        public enum DropTypes
        {
            /// <summary>
            /// Бросок отключен
            /// </summary>
            None = 0,

            /// <summary>
            /// Все объекты
            /// </summary>
            Full = 1,

            /// <summary>
            /// Объекты в коллекции данного элемента
            /// </summary>
            Local = 2,
        }

        public bool IsPossibleDrag => IsDrag;

        public bool IsPossibleDrop => IsDropFiles || DropType != DropTypes.None;
    }
}
