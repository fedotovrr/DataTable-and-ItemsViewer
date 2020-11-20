using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataTable
{
    [Serializable]
    public class ClipboardData
    {
        public byte[] BufferData;

        public int DropLevel;

        public bool IsRemove;

        public ClipboardData(int dropLevel, Type itemType, List<object> items)
        {
            DropLevel = dropLevel;
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    BinaryFormatter serializer = new BinaryFormatter();
            //    serializer.Serialize(ms, obj);
            //    BufferData = ms.ToArray();
            //}

            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<object>), new Type[] { itemType });
                serializer.Serialize(ms, items);
                BufferData = ms.ToArray();
            }
        }

        public List<object> GetObject(Type itemType, bool IgnoreException)
        {
            try
            {
                //using (MemoryStream ms = new MemoryStream(BufferData))
                //{
                //    BinaryFormatter serializer = new BinaryFormatter();
                //    return serializer.Deserialize(ms) as List<object>;
                //}

                using (MemoryStream ms = new MemoryStream(BufferData))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<object>), new Type[] { itemType });
                    return serializer.Deserialize(ms) as List<object>;
                }
            }
            catch (Exception ex)
            {
                if (IgnoreException) return null;
                else throw ex;
            }
        }
    }
}
