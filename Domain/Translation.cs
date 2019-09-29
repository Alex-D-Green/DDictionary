using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDictionary.Domain
{
    public class Translation
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public PartOfSpeech Part { get; set; }

        public override string ToString()
        {
            string ret = Text;

            if(Part != PartOfSpeech.Unknown)
                ret = String.Format("{0} ({1})", ret, Part.ToShortString());

            return ret;
        }
    }
}
