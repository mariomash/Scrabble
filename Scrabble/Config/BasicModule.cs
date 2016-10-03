using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Scrabble.Config {
    [DataContract]
    [Serializable]
    //[XmlInclude(typeof(ModuloArchivoBuffer))]
    public abstract class BasicModule {

        public bool Active;

        public int Id;

        [XmlIgnore]
        [IgnoreDataMember]
        protected static Configuration Configuration => Configuration.Instance;
        
        protected BasicModule()
        {
            Id = new Random().Next(100000000);
            Active = false;
        }

    }
}
