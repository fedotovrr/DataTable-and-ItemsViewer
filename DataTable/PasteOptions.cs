using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTable
{
    public class PasteOptions
    {
        public PasteTypes PasteType { get; set; } = PasteTypes.InLevel;

        /// <summary>
        /// Разбирать текст из буфера
        /// </summary>
        public bool ParseText { get; set; } = true;

        /// <summary>
        /// Вствка объекта из буфера
        /// </summary>
        public bool PasteObject { get; set; } = true;

        public bool IsPossiblePaste => ParseText || PasteObject;

        public enum PasteTypes
        {
            /// <summary>
            /// Перемещение элементов только по коллекциям с одинаковым уровнем
            /// </summary>
            InLevel = 0,

            /// <summary>
            /// Вставка элементов в коллекцию дочерних элементов
            /// </summary>
            InChildren = 1,
        }
    }
}
