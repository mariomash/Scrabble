using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Scrabble.Config {
    public class Utils {
        /// <summary>
        /// Deserializes xml file to object
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object DeSerializeObjectFromXml(string filePath, Type type)
        {

            object data;
            Stream stream = null;
            try
            {
                stream = File.Open(filePath, FileMode.Open);
                var x = new XmlSerializer(type);
                data = x.Deserialize(new XmlTextReader(stream));

                stream.Close();
                stream.Dispose();
            }
            catch (Exception ex)
            {

                var exMessage = ex.Message;
                if (ex.InnerException != null) exMessage = ex.InnerException.Message;

                Configuration.Instance.Logger.Log(LogType.Error, $"{exMessage}", new StackTrace());

                try
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                throw new Exception(ex.Message);

            }
            return data;

        }

    }

}