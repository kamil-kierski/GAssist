namespace GAssist
{
    internal static class HtmlResponseParser
    {
        public static string ParseHtmlResponse(string html)
        {
            //  var bodyOld = "body{background:transparent;margin:0}";
            //  var bodyNew = "body { background: transparent; margin: 0; -webkit - transform: scale(0.5); }";
            //  var shadow = "<div class=\"popout-shadow\" id=\"assistant-shadow\"></div>";
            //  var htmlOld = "html";
            //  var htmlNew = "html {position: relative; max-width: 100vw; height: 100%;}";

            var positionAbs = "position: fixed;";
            var popoutOld = "#popout{";
            var popoutNew = "#popout{ -moz-transform: scale(0.5, 0.5); zoom: 0.5; zoom: 50%;";
            var flex = "display:-webkit-flex;";
            var wrap = "display: -webkit-flex;\nheight: 100%;\nwidth: 100%;\n-webkit-flex-wrap: wrap;\n-webkit-flex-shrink: 1; scale = 1, maximum - scale = 0";

            return html.Replace(popoutOld, popoutNew).Replace(flex, wrap).Replace(positionAbs, "");
        }
    }
}