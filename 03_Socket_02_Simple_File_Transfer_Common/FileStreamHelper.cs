using System;
using System.IO;

namespace Common
{
    public class FileStreamHelper
    {
        public byte[] Read(string path, long offset, int length) // leer una parte de un archivo
        {
            var data = new byte[length];

            using (var fs = new FileStream(path, FileMode.Open))
            {
                fs.Position = offset;
                var bytesRead = 0;
                while (bytesRead < length)
                {
                    var read = fs.Read(data, bytesRead, length - bytesRead);
                    if (read == 0)
                    {
                        throw new Exception("Couldn't not read file");
                    }
                    bytesRead += read;
                }
            } // se cierra/dispose del file fileStream , libero los recursos 

            return data; // el data contiene todos los bytes de la parte del archivo solitados
        }

        public void Write(string fileName, byte[] data) // escribir una parte de un archivo
        {
            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Append))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            else
            {
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
        }
    }
}