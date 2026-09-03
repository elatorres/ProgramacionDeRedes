namespace Common
{
    public class Protocol
    {
        public const int LargoFijo = 4; // 4 bytes, Tamaño del largo del mensaje, int
        public const int LargoFijoArchivo = 8; // 8 bytes (64 bits) / long, tamaño del largo del archivo
        public const int MaxFileSizePart = 32768; // 32KB 

        public static long CalcularCantidadDePartes(long fileSize) 
        {
            long parts = fileSize / MaxFileSizePart;
            if (parts * MaxFileSizePart == fileSize)
            {
                return parts;
            }
            else
            {
                return parts + 1;
            }
        }
    }
}