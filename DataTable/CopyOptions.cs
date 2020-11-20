using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTable
{
    public class CopyOptions
    {
        /// <summary>
        /// Помещать объект в буфер
        /// </summary>
        public bool SetClipboardObject { get; set; } = true;

        /// <summary>
        /// Преобразовать объект в текстовый формат в виде таблицы
        /// </summary>
        public bool SetClipboardText { get; set; } = true;

        public bool IsPossibleCopy => SetClipboardObject || SetClipboardText;
    }
}
