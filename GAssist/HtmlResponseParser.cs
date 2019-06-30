using System;
using Tizen;

namespace GAssist
{
    internal class HtmlResponseParser
    {
        public static string ParseHtmlResponse(string html)
        {
            var bodyOld = "body{background:transparent;margin:0}";
            var bodyNew =
                "body{background:transparent;margin:0; transform: scale(0.5); transform - origin: 0 0;}";

            return html.Replace(bodyOld, bodyNew);
        }

        //public static string ParseHtmlResponse2(string html)
        //{
        //    var flex = "display: -webkit-flex";
        //    var wrap =
        //        "display: -webkit-flex;\nheight: 360;\nwidth: 360;\n-webkit-flex-wrap: wrap;\n-webkit-flex-shrink: 1;";

        //    var parsed = html.Replace(flex, wrap);
        //    Log.Debug("HTMLPARSER", parsed);
        //    return parsed;
        //}
    }
}