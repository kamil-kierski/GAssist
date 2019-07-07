namespace GAssist
{
    internal static class HtmlResponseParser
    {
        public static string ParseHtmlResponse(string html)
        {
            //var bodyOld = "body{background:transparent;margin:0}";
            //var bodyNew =
            //    "body { background: transparent; margin: 0; -webkit - transform: scale(0.5); }";
            var shadow = "<div class=\"popout-shadow\" id=\"assistant-shadow\"></div>";
            var positionAbs = "position: absolute;";

            var htmlOld = "html";
            var htmlNew = "html {position: relative; max-width: 400px; height: 100%;}";

            var popoutOld = "#popout{";
            var popoutNew = "#popout{ -moz-transform: scale(0.5, 0.5); zoom: 0.5; zoom: 50%;";

            var flex = "display:-webkit-flex;";
            var wrap =
                "display: -webkit-flex;\nheight: 360;\nwidth: 360;\n-webkit-flex-wrap: wrap;\n-webkit-flex-shrink: 1;";

            return html.Replace(htmlOld, htmlNew).Replace(popoutOld, popoutNew).Replace(shadow, string.Empty).Replace(positionAbs, string.Empty).Replace(flex, wrap);
        }

        //public static string ParseHtmlResponse2(string html)
        //{


        //    var parsed = html.Replace(flex, wrap);
        //    Log.Debug("HTMLPARSER", parsed);
        //    return parsed;
        //}
    }
}