using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace ItemsViewer
{
    /// <summary>
    /// Не наследуйте этот класс, если хотите использовать дубликаты объектов в коллекции ItemsSource
    /// </summary>
    [Serializable]
    public class ViewItem : INotifyPropertyChanged
    {
        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        [XmlIgnore]
        private int index = -1;

        [NonSerialized]
        [XmlIgnore]
        private bool focused;

        [NonSerialized]
        [XmlIgnore]
        private bool selected;

        [NonSerialized]
        [XmlIgnore]
        private object source;

        [XmlIgnore]
        public int Index { get => index; internal set => index = value; }

        [XmlIgnore]
        public bool Focused { get => focused; internal set { if (focused != value) { focused = value; NotifyPropertyChanged(); } } }

        [XmlIgnore]
        public bool Selected { get => selected; internal set { if (selected != value) { selected = value; NotifyPropertyChanged(); } } }

        [XmlIgnore]
        public object Source { get => source == null ? this : source; internal set => source = value; }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(MethodBase.GetCurrentMethod()?.Name?.Substring(4)));
    }
}
