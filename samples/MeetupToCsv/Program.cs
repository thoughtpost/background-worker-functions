using System;
using System.IO;
using System.Text;

namespace MeetupToCsv
{
    class Program
    {
        static void Main(string[] args)
        {
            string csvFilename = @"Attendees.csv";

            string meetupContent = File.ReadAllText(@"meetup.html");

            StringBuilder sbCsv = new StringBuilder();
            sbCsv.AppendLine("member_name,introduction,joined,registered,meetup_image");

            string tagSearch = "<img class=\"avatar-print\" src=\"";
            string altSearch = "alt=\"";

            int pos = meetupContent.IndexOf(tagSearch);

            while (pos != -1)
            {
                pos = pos + tagSearch.Length;

                int posEnd = meetupContent.IndexOf("\"", pos);
                string img = meetupContent.Substring(pos, posEnd - pos);

                meetupContent = meetupContent.Substring(posEnd);

                int posAlt = meetupContent.IndexOf(altSearch);
                if ( posAlt != -1 )
                {
                    posAlt = posAlt + altSearch.Length;

                    int posAltEnd = meetupContent.IndexOf( "\"", posAlt);

                    string alt = meetupContent.Substring(posAlt, posAltEnd - posAlt);

                    sbCsv.AppendLine( alt + ",,,," + img );

                    meetupContent = meetupContent.Substring(posAltEnd);

                }

                pos = meetupContent.IndexOf(tagSearch);
            }

            if ( File.Exists(csvFilename ))
            {
                File.Delete(csvFilename);
            }

            File.WriteAllText(csvFilename, sbCsv.ToString());
        }
    }
}
