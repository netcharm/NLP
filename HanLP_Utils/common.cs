using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace HanLP_Utils
{
    public partial class MainForm : Form
    {
        internal bool _KeepNumber = true;
        internal bool _FilterHtmlTag = true;
        internal bool _FilterLrcTag = true;
        internal bool _FilterAssTag = true;

        private string replaceCharEntity( string htmlstr )
        {
            string result = htmlstr;
            Dictionary<string, string> CHAR_ENTITIES = new Dictionary<string, string>()
            {
                { "&nbsp", " "}, 
                { "&160", " "},
                { "&lt", "<"}, 
                { "&60", "<"},
                { "&gt", ">"},
                { "&62", ">"},
                { "&amp", "&"},
                { "&38", "&"},
                { "&quot", "\""},
                { "&34", "\""}
            };

            foreach(var k in CHAR_ENTITIES)
            {
                result = result.Replace( k.Key, k.Value );
            }
            return( result );
        }

        internal string filterHtmlTag( string text )
        {
            string result = text;

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( text );
            var scripts = doc.DocumentNode.SelectNodes( "//script" );
            var styles = doc.DocumentNode.SelectNodes( "//style" );
            var forms = doc.DocumentNode.SelectNodes( "//form" );
            var links = doc.DocumentNode.SelectNodes( "//a" );
            var comments = doc.DocumentNode.SelectNodes( "//comment()" );
            if ( scripts != null ) foreach ( var node in scripts ) { node.Remove(); }
            if ( styles != null ) foreach ( var node in styles ) { node.Remove(); }
            if ( forms != null ) foreach ( var node in forms ) { node.Remove(); }
            if ( links != null ) foreach ( var node in links ) { node.Remove(); }
            if ( comments != null ) foreach ( var node in comments ) { node.Remove(); }

            result = doc.DocumentNode.SelectSingleNode( "//body" ).InnerText.Trim();

            result = Regex.Replace( result, @"((\r\n)|(\n\r)|(\r)|(\n)){2,}", "", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            result = Regex.Replace( result, @"(</.*?>)", "", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            //result = Regex.Replace( result, @"[(</.*?>)|(&gt;)|(&lt;)|(&amp;)]{1,}", " ", RegexOptions.IgnoreCase | RegexOptions.Multiline );

            return ( result.Trim() );
        }

        internal string filterASS(string text)
        {
            string result = text;

            //string pat_ass_header = @"(\[Script Info\])(((\n)|(\r)|(\n\r)|(\r\n)).*?$)*?(((\n)|(\r)|(\n\r)|(\r\n))\[Events\]$)";
            //string pat_ass_header = @"(\[Script Info\])(((\n)|(\r)|(\n\r)|(\r\n)).*?)*?(\[Events\]$)";
            //string pat_ass_head = @"(^\[Script Info\](([(\r)|(\n)|(\r\n)].*?)*?)^\[Events\][(\r)|(\n)|(\r\n)].*?Text)";
            string pat_ass_diag = @"(^Format:.*?Text)|(^Dialogue:.*?,.*?,.*?,.*?,.*?,.*?,.*?,.*?,.*?,)|(\\N)|(\{\\kf.*?\})|(\{\\f.*?\})|(\\f.*?%)|(\{\\(3){0,1}c&H.*?&\})|(\\(3){0,1}c&H.*?&)|(\{\\a\d+\})|(\{\\.*?\})";

            int ass_s = result.IndexOf( "[Script Info]" );
            int ass_e = result.IndexOf( "[Events]" );
            if ( ass_s >= 0 && ass_e > ass_s )
                result = result.Remove( ass_s, ass_e - ass_s + 8 );

            //result = Regex.Replace( result, pat_ass_header, "", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            //result = Regex.Replace( result, pat_ass_head, "", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            result = Regex.Replace( result, pat_ass_diag, "", RegexOptions.IgnoreCase | RegexOptions.Multiline );

            return ( result.Trim() );
        }

        internal string filterLrc( string text )
        {
            string result = text;

            string pat_lyric = @"(\[id:.*?\])|(\[al:.*?\])|(\[ar:.*?\])|(\[ti:.*?\])|(\[by:.*?\])|(\[la:.*?\])|(\[lg:.*?\])|(\[offset:.*?\])|(\[\d+:\d+(\.\d+){0,1}\])";
            result = Regex.Replace( result, pat_lyric, "", RegexOptions.IgnoreCase | RegexOptions.Multiline );

            return ( result.Trim() );
        }

        internal string filterMisc( string text )
        {
            string result = text;

            string pat_misc = @"(&#\d+;)|([\u0001-\u001F,\u0021-\u0040,\u005B-\u0060,\u007B-\u00FF,\u2000-\u206F,\u2190-\u2426,\u3000-\u303F,\u31C0-\u31E3,\uFE10-\uFE4F])|([\.|·|　|…])";
            result = Regex.Replace( result, pat_misc, "", RegexOptions.IgnoreCase ); 

            if(!_KeepNumber)
                result = Regex.Replace( result, @"\d+", "", RegexOptions.IgnoreCase );

            return ( result.Trim() );
        }

    }
}
