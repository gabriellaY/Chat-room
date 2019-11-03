using System.Xml.Serialization;

namespace ChatRoomClient
{
    [XmlRoot(ElementName = "Parameter")]
    public class Parameter
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }


        [XmlText]
        public string Text { get; set; }
    }
}