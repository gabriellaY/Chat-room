using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChatRoomServer
{
    [XmlRoot(ElementName = "Configurations")]
    public class Configurations
    {
        [XmlElement(ElementName = "ConnectionString")]
        public string ConnectionString { get; set; }
    }
}
