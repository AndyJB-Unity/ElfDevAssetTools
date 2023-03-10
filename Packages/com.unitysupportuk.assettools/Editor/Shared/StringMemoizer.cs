using System.Collections.Generic;

namespace ElfDev
{
    public class StringMemoizer
    {
        Dictionary<string, string> db = new Dictionary<string, string>();

        public string get(string key)
        {
            if (db.ContainsKey(key))
            {
                return db[key];
            }
            else
            {
                db[key] = key;
                return db[key];
            }
        }
    }
}


