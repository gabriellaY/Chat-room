using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChatRoomClient
{
    [XmlRoot(ElementName = "Command")]
    public class Command
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }


        [XmlElement(ElementName = "Parameters")]
        public List<Parameter> Parameters { get; set; }
    }
}
