using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Produce a Html file with the given clauses. 
    /// The file uses JavaScript and Bootstrap to get some interactivity.
    /// </summary>
    public static class BootstrapHTMLPublisher
    {
        #region HTML header/footer

        private const string title = @"
<!DOCTYPE html>

<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>{0}</title>
";

        private const string header = @"
    <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css"">
    <link rel=""stylesheet"" href=""https://use.fontawesome.com/releases/v5.7.0/css/all.css""
          integrity=""sha384-lZN37f5QGtY3VHgisS14W3ExzMWZxybE1SJSEsQp9S+oqd12jhcu+A56Ebc1zFSJ"" crossorigin=""anonymous"">

    <script src=""https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js""></script>
    <script src=""https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js""></script>

    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">

    <style type=""text/css"">
        .dict-word {
                <!--font-weight: bold;-->
            }

        .dict-transl {
                <!--font-weight: bold;-->
            }

        .dict-rel {
                <!--font-weight: bold;-->
            }

        .dict-context {
                font-size: xx-small;
            }
    </style>
</head>

<body>
";

        private const string footer = @"
    <script> 
        function playAudio(id) { document.getElementById(id).play(); }
    </script>

</body>
</html>
";

        private const string tableHeader = @"
    <a href=""#_{0}"" class=""btn btn-primary"" data-toggle=""collapse"">{1}</a>
    <div id=""_{0}"" class=""collapse"">
        <table class=""table table-hover table-sm"">
            <thead class=""thead-light"">
                <tr>
                    <th></th>
                    <th>Word</th>
                    <th>Translations</th>
                    <th>Relations</th>
                    <th>Context</th>
                </tr>
            </thead>
            <tbody>
";

        private const string tableFooter = @"
            </tbody>
        </table>     
    </div>
";

        #endregion

        /// <summary>
        /// Get data for a Html file.
        /// </summary>
        /// <param name="title">Will be put in the Html title.</param>
        /// <param name="clauses">Clauses that should be put in the file.</param>
        public static string Publish(string title, IEnumerable<Clause> clauses)
        {
            if(clauses is null)
                throw new ArgumentNullException(nameof(clauses));


            var sb = new StringBuilder();
            sb.AppendFormat(BootstrapHTMLPublisher.title, title);
            sb.Append(header);

            foreach(WordGroup gr in Enum.GetValues(typeof(WordGroup)).Cast<WordGroup>())
            {
                var sb2 = new StringBuilder();
                int cnt = 0;

                foreach(Clause w in clauses.Where(o => o.Group == gr).OrderByDescending(o => o.Added))
                {
                    sb2.AppendLine(FormatWord(w));
                    cnt++;
                }

                if(cnt == 0)
                    continue;

                sb.AppendFormat(tableHeader, gr.ToGradeStr(), String.Format("{0} ({1})", gr.ToFullStr(), cnt));
                sb.Append(sb2.ToString());
                sb.Append(tableFooter);
            }

            sb.Append(footer);

            return sb.ToString();
        }

        private static string FormatWord(Clause word)
        {
            const string row = @"
                <tr>
                    <td>
                        <audio src=""{0}"" id=""{1}"" preload=""none""></audio>
                        <a href=""javascript:playAudio('{1}');"" title=""[{2}]"">
                            <span class=""fas fa-volume-up""></span>
                        </a>
                    </td>
                    <td class=""dict-word"" title=""Created: {3} 
Updated: {4}"">{5}</td>
                    <td class=""dict-transl"">{6}</td>
                    <td class=""dict-rel"">{7}</td>
                    <td class=""dict-context"">{8}</td>
                </tr>
";

            return String.Format(row,
                word.Sound ?? "#",
                word.Id,
                word.Transcription,
                word.Added.ToString("yyyy-MM-dd"),
                word.Updated.ToString("yyyy-MM-dd"),
                word.Word,
                ClauseToDataGridClauseMapper.MakeTranslationsString(word.Translations),
                (word.Relations.Count > 0)
                    ? word.Relations.Aggregate("", (s, o) => $"{s}<div title=\"{o.Description}\">{o.ToClause.Word}; </div>")
                    : "---",
                word.Context);
        }
    }
}
