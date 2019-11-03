﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChatRoomServer
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
