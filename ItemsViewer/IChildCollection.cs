using System.Collections.Generic;

namespace ItemsViewer
{
    public interface IChildCollection
    {
        byte DropLevel { get; set; }

        bool Dropped { get; set; }

        bool DropMarkVisible { get; }

        IChildCollection Parent { get; set; }

        object this[int index] { get; }

        int Count { get; }

        void Add(object item);

        void Insert(int index, object item);

        void InsertRange(int index, IEnumerable<object> collection);

        bool Remove(object item);

        void Clear();
    }
}