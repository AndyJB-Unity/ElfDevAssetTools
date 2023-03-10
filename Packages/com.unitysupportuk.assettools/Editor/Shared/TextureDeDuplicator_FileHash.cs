using System.Security.Cryptography;

namespace ElfDev 
{
    public partial class TextureDeDuplicator
    {
        class FileHash
        {
            private static StringMemoizer hashMemoizer = new StringMemoizer();

            public byte[] md5hash;

            private string hashAsString;

            public FileHash(string filePath)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = System.IO.File.OpenRead(filePath))
                    {
                        md5hash = md5.ComputeHash(stream);
                    }
                }

                // Using memoized strings means we will be able to do fast compares
                hashAsString = hashMemoizer.get(System.BitConverter.ToString(md5hash));
            }

            public override string ToString()
            {
                if (md5hash == null)
                    return "{null}";
                if (hashAsString != null)
                    return hashAsString;
                return "{ERROR!}";
            }
        }
    }
}