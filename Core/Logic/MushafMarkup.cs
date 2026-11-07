using System.Xml;
using System.Xml.Linq;

namespace AlMithaq.Core.Logic;

public static class MushafMarkup
{
    private static readonly HashSet<string> Elements = ["svg", "g", "path", "defs", "clipPath", "use", "rect", "polygon", "polyline", "circle", "ellipse", "line", "title", "desc"];
    public static string PrepareSvg(string svg, int page)
    {
        using var reader = XmlReader.Create(new StringReader(svg), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("صفحة مصحف غير صالحة");
        if (root.Name.LocalName != "svg") throw new InvalidDataException("صفحة مصحف غير صالحة");
        foreach (var element in root.DescendantsAndSelf().ToList())
        {
            if (!Elements.Contains(element.Name.LocalName)) { element.Remove(); continue; }
            foreach (var attribute in element.Attributes().ToList())
                if (attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase) || attribute.Name.LocalName == "style" ||
                    (attribute.Name.LocalName == "href" && !attribute.Value.StartsWith('#'))) attribute.Remove();
        }
        var polygons = root.Elements().Where(e => (string?)e.Attribute("class") == "ayahPolygon").ToList();
        if (polygons.Count == 0) throw new InvalidDataException("لا توجد إحداثيات للآيات");
        var surahFrames = MushafSurahFrames.Create(page, root).ToList();
        if (page > 2 && surahFrames.Count > 0) AlignFrames(surahFrames, polygons);
        AddClassicOrnaments(surahFrames);
        foreach (var polygon in polygons) polygon.Remove();
        XNamespace ns = root.Name.Namespace;
        var art = new XElement(ns + "g", new XAttribute("id", "alm_art"), root.Nodes().ToList());
        root.RemoveNodes();
        root.Add(surahFrames);
        root.Add(art);
        root.Add(new XElement(ns + "defs",
            new XElement(ns + "clipPath", new XAttribute("id", "alm_clip"), new XElement(ns + "path", new XAttribute("id", "alm_clip_path"), new XAttribute("d", ""))),
            new XElement(ns + "filter", new XAttribute("id", "alm_ink"), new XElement(ns + "feColorMatrix", new XAttribute("id", "alm_ink_matrix"), new XAttribute("type", "matrix"), new XAttribute("values", "0 0 0 0 .06 0 0 0 0 .43 0 0 0 0 .31 0 0 0 1 0")))));
        root.Add(new XElement(ns + "g", new XAttribute("clip-path", "url(#alm_clip)"), new XAttribute("filter", "url(#alm_ink)"), new XAttribute("pointer-events", "none"), new XElement(ns + "use", new XAttribute("href", "#alm_art"))));
        root.Add(polygons);
        var prefix = $"p{page}_";
        foreach (var attribute in root.DescendantsAndSelf().Attributes())
        {
            if (attribute.Name.LocalName == "id") attribute.Value = prefix + attribute.Value;
            else if (attribute.Name.LocalName == "href" && attribute.Value.StartsWith('#')) attribute.Value = "#" + prefix + attribute.Value[1..];
            else if (attribute.Value.Contains("url(#", StringComparison.Ordinal)) attribute.Value = attribute.Value.Replace("url(#", "url(#" + prefix, StringComparison.Ordinal);
        }
        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static void AddClassicOrnaments(IEnumerable<XElement> frames)
    {
        foreach (var frame in frames)
        {
            var ns = frame.Name.Namespace;
            var sprays = frame.Elements().Where(e => (string?)e.Attribute("class") == "surah-spray").ToArray();
            foreach (var spray in sprays)
            {
                var panel = new XElement(ns + "g", new XAttribute("class", "surah-classic"),
                    new XAttribute("transform", (string)spray.Attribute("transform")!),
                    new XAttribute("fill", "none"), new XAttribute("stroke", "var(--ink)"), new XAttribute("stroke-width", "2.2"));
                panel.Add(new XElement(ns + "path", new XAttribute("d", "M5 48 Q20 30 5 7 Q31 13 47 4 Q65 23 83 7 Q99 23 117 6 Q134 30 151 17 Q161 41 171 48 Q161 55 151 79 Q134 66 117 90 Q99 73 83 89 Q65 73 47 92 Q31 83 5 89 Q20 66 5 48Z M13 48 C34 19 54 30 66 48 S104 72 126 48 S151 35 162 48 M13 48 C34 77 54 66 66 48 S104 24 126 48 S151 61 162 48")));
                foreach (var (x, y) in new[] { (36, 24), (36, 72), (83, 48), (123, 24), (123, 72) })
                {
                    panel.Add(new XElement(ns + "path", new XAttribute("transform", $"translate({x} {y})"),
                        new XAttribute("d", "M0 -10 Q7 -16 8 -6 Q18 -5 11 2 Q15 12 5 10 Q0 19 -5 10 Q-15 12 -11 2 Q-18 -5 -8 -6 Q-7 -16 0 -10Z")));
                    panel.Add(new XElement(ns + "circle", new XAttribute("cx", x), new XAttribute("cy", y), new XAttribute("r", "3")));
                }
                panel.Add(new XElement(ns + "ellipse", new XAttribute("cx", "194"), new XAttribute("cy", "48"), new XAttribute("rx", "24"), new XAttribute("ry", "35")),
                    new XElement(ns + "ellipse", new XAttribute("cx", "194"), new XAttribute("cy", "48"), new XAttribute("rx", "20"), new XAttribute("ry", "30")));
                frame.Add(panel);
            }
        }
    }

    private static void AlignFrames(IReadOnlyList<XElement> frames, IReadOnlyList<XElement> verses)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var horizontal = verses.SelectMany(verse => System.Text.RegularExpressions.Regex
            .Matches((string?)verse.Attribute("d") ?? "", @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?")
            .Where((_, index) => index % 2 == 0).Select(match => double.Parse(match.Value, culture))).ToArray();
        if (horizontal.Length == 0) return;
        var left = horizontal.Min() + .5;
        var right = horizontal.Max() - .5;
        foreach (var frame in frames)
        {
            var border = frame.Elements().First();
            var oldLeft = (double)border.Attribute("x")!;
            var oldWidth = (double)border.Attribute("width")!;
            if (left >= oldLeft || right <= oldLeft + oldWidth) continue;
            var scale = (right - left) / oldWidth;
            frame.SetAttributeValue("transform", FormattableString.Invariant($"translate({left} 0) scale({scale} 1) translate({-oldLeft} 0)"));
        }
    }
    public static string Build(string svg, int theme = 0) => BuildDocument(1, PrepareSvg(svg, 1), null, null, theme);
    public static string BuildDocument(int page, string current, string? previous, string? next, int theme, int frameStyle = 0)
    {
        var background = theme == 2 ? "#101f1c" : theme == 0 ? "#f0e5d0" : "#fffcf2";
        return $$$"""
        <!doctype html><html lang="ar" style="background:{{{background}}}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no">
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; img-src data:">
        <style>
        html,body{margin:0;width:100%;height:100%;overflow:hidden;background:{{{background}}};overscroll-behavior:none}
        body{--ink:#231f20;--paper:#f0e5d0;--surah-ornament:#24735e;--surah-gold:#ae894c;touch-action:none}body[data-theme="1"]{--paper:#fffcf2}body[data-theme="2"]{--ink:#ece8da;--paper:#101f1c;--surah-ornament:#bdad78;--surah-gold:#bea369}
        #deck{position:fixed;top:0;right:0;bottom:0;left:0;background:var(--paper);overflow:hidden;touch-action:none}
        .leaf{position:absolute;top:0;right:0;bottom:0;left:0;box-sizing:border-box;padding:5px;background:var(--paper);display:flex;align-items:center;justify-content:center;will-change:transform;backface-visibility:hidden}
        #previous{transform:translateX(100%)}#next{transform:translateX(-100%)}svg{width:100%;height:100%;max-height:100%;touch-action:none}
        .leaf{padding-top:28px;padding-bottom:26px}
        .folio-heading,.folio-number{position:absolute;color:var(--ink);font:12px sans-serif;opacity:.8;pointer-events:none;user-select:none;-webkit-user-select:none}
        #deck{user-select:none;-webkit-user-select:none;-webkit-touch-callout:none}
        .folio-heading{top:8px;left:14px;right:14px;display:flex;justify-content:space-between;gap:8px;direction:ltr}
        .folio-heading span{direction:rtl}.folio-number{bottom:6px;left:0;right:0;text-align:center}
        body[data-frame="1"]{--surah-ornament:var(--ink);--surah-gold:var(--ink)}
        body[data-frame="1"] .surah-frame rect:first-child{stroke-width:.75}
        .surah-classic,body[data-frame="1"] .surah-spray,body[data-frame="2"] .surah-spray,body[data-frame="3"] .surah-spray,body[data-frame="2"] .surah-frame>path,body[data-frame="2"] .surah-frame>rect:nth-child(2){display:none}
        body[data-frame="1"] .surah-classic,body[data-frame="3"] .surah-classic{display:inline}
        body[data-frame="2"] .surah-frame>rect:first-child{rx:3;stroke-width:.36}
        body[data-frame="3"] .leaf{padding:42px 17px 39px}
        body[data-frame="3"] .leaf:before{content:'';position:absolute;top:25px;right:5px;bottom:24px;left:5px;pointer-events:none;border:8px solid #547f83;outline:1px solid #a48c56;outline-offset:-1px;border-image:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 36 36'%3E%3Cdefs%3E%3Cpattern id='p' width='12' height='12' patternUnits='userSpaceOnUse'%3E%3Crect width='12' height='12' fill='%231b505b'/%3E%3Cpath d='M0 6Q3 6 6 1Q9 6 12 6Q9 6 6 11Q3 6 0 6Z M0 1Q3 1 3 4M12 11Q9 11 9 8' fill='none' stroke='%23c5ad76' stroke-width='.8'/%3E%3Ccircle cx='6' cy='6' r='1.4' fill='%237ebfc1'/%3E%3C/pattern%3E%3C/defs%3E%3Crect width='36' height='36' fill='url(%23p)'/%3E%3C/svg%3E") 12 round}
        body[data-frame="3"] .leaf[data-opening="true"]:before{outline:2px double #c5ad76;box-shadow:inset 0 0 0 2px #48767b,inset 0 0 0 4px #ad9259}
        body[data-frame="3"][data-theme="2"] .leaf:before{outline-color:#c9af73}
        [id$="_alm_art"]{fill:var(--ink)}svg path:not(.ayahPolygon){pointer-events:none}
        .leaf{--gutter-edge:rgba(39,27,15,.19);--gutter-mid:rgba(39,27,15,.06);--gutter-soft:rgba(39,27,15,.025)}
        body[data-theme="2"] .leaf{--gutter-edge:#374b41;--gutter-mid:#20342b;--gutter-soft:rgba(73,99,85,.08)}
        .leaf:after{content:'';position:absolute;top:0;bottom:0;width:28px;pointer-events:none}
        .leaf[data-binding="right"]:after{right:0;background:linear-gradient(to left,var(--gutter-edge),var(--gutter-mid) 22%,var(--gutter-soft) 65%,transparent)}
        .leaf[data-binding="left"]:after{left:0;background:linear-gradient(to right,var(--gutter-edge),var(--gutter-mid) 22%,var(--gutter-soft) 65%,transparent)}
        body[data-theme="2"] svg [fill="#231f20"],body[data-theme="2"] svg [fill="#000000"],body[data-theme="2"] svg [fill="#000"],body[data-theme="2"] svg [fill="black"]{fill:var(--ink)}
        .ayahPolygon{fill:#c6a45c;fill-opacity:0;pointer-events:all;transition:fill-opacity 180ms ease}.ayahPolygon.selected{fill-opacity:.16}
        </style></head><body data-theme="{{{theme}}}" data-frame="{{{frameStyle}}}"><main id="deck"><article class="leaf" id="previous">{{{previous}}}</article><article class="leaf" id="current">{{{current}}}</article><article class="leaf" id="next">{{{next}}}</article></main>
        <script>
        const deck=document.getElementById('deck'),current=document.getElementById('current'),previous=document.getElementById('previous'),next=document.getElementById('next');
        let svg,box,zoom=1,cx=0,cy=0,sx=0,sy=0,lx=0,ly=0,dist=0,startZoom=1,moved=false,multi=false,pending=false,longPressed=false,press=0,pressTarget=null,drag=0,lastTouch=0;
        window.mushafPageNumber={{{page}}};
        function setBinding(page){
          current.dataset.binding=page%2?'left':'right';
          previous.dataset.binding=next.dataset.binding=page%2?'right':'left';
          current.dataset.opening=page<=2;
          previous.dataset.opening=page>1&&page-1<=2;
          next.dataset.opening=page+1<=2;
        }
        setBinding(window.mushafPageNumber);
        const bridge=url=>{location.href='almithaq://'+url;};
        function cancelPress(){clearTimeout(press);press=0;}
        function initBox(){svg=current.querySelector('svg');if(!svg)return;const b=svg.viewBox.baseVal;box={x:b.x,y:b.y,w:b.width,h:b.height};cx=box.x+box.w/2;cy=box.y+box.h/2;}
        function render(){if(!svg||!box)return;const w=box.w/zoom,h=box.h/zoom;cx=Math.max(box.x+w/2,Math.min(box.x+box.w-w/2,cx));cy=Math.max(box.y+h/2,Math.min(box.y+box.h-h/2,cy));svg.setAttribute('viewBox',[cx-w/2,cy-h/2,w,h].join(' '));}
        window.setZoom=function(z){zoom=Math.max(1,Math.min(3.5,z));render();};
        window.setPalette=function(theme){
          document.body.dataset.theme=String(theme);const dark=theme===2;const bg=dark?'#101f1c':theme===0?'#f0e5d0':'#fffcf2';document.documentElement.style.background=bg;document.body.style.background=bg;
          document.querySelectorAll('[id$="_alm_ink_matrix"]').forEach(e=>e.setAttribute('values',dark?'0 0 0 0 .52 0 0 0 0 .85 0 0 0 0 .68 0 0 0 1 0':'0 0 0 0 .06 0 0 0 0 .43 0 0 0 0 .31 0 0 0 1 0'));
        };
        function position(dx,animated=false){
          const width=deck.clientWidth;drag=dx;[current,previous,next].forEach(e=>e.style.transition=animated?'transform 190ms cubic-bezier(.2,.7,.2,1)':'none');
          current.style.transform='translate3d('+dx+'px,0,0)';previous.style.transform='translate3d('+(dx+width)+'px,0,0)';next.style.transform='translate3d('+(dx-width)+'px,0,0)';
        }
        window.cancelTurn=function(){pending=false;cancelPress();position(0,true);};
        window.installPages=function(page,center,before,after,z,theme){
          cancelPress();current.innerHTML=center;previous.innerHTML=before||'';next.innerHTML=after||'';previous.style.visibility=before?'visible':'hidden';next.style.visibility=after?'visible':'hidden';
          window.mushafPageNumber=page;setBinding(page);pending=false;moved=false;multi=false;longPressed=false;position(0);initBox();window.setPalette(theme);window.setZoom(z);
        };
        window.markAyah=function(s,a){
          if(!svg)return;svg.querySelectorAll('.selected').forEach(x=>x.classList.remove('selected'));
          const p=[...svg.querySelectorAll('.ayahPolygon')].filter(x=>+x.getAttribute('surah')===s&&+x.getAttribute('ayah')===a);
          p.forEach(x=>x.classList.add('selected'));svg.querySelector('[id$="_alm_clip_path"]').setAttribute('d',p.map(x=>x.getAttribute('d')).join(' '));
        };
        function distance(t){return Math.hypot(t[0].clientX-t[1].clientX,t[0].clientY-t[1].clientY);}
        deck.addEventListener('touchstart',e=>{
          if(pending)return;cancelPress();longPressed=false;
          if(e.touches.length===2){multi=true;moved=true;dist=distance(e.touches);startZoom=zoom;position(0);}
          else{const t=e.touches[0];sx=lx=t.clientX;sy=ly=t.clientY;moved=false;multi=false;pressTarget=e.target.closest('.ayahPolygon');
            if(pressTarget)press=setTimeout(()=>{if(moved||multi||pending)return;longPressed=true;bridge('verse/'+Number(pressTarget.getAttribute('surah'))+'/'+Number(pressTarget.getAttribute('ayah'))+'#'+Math.max(0,Math.min(1,sy/window.innerHeight)).toFixed(4));},480);
          }
        },{passive:true});
        deck.addEventListener('touchmove',e=>{
          e.preventDefault();if(pending||longPressed)return;
          if(e.touches.length===2){cancelPress();multi=true;moved=true;window.setZoom(startZoom*distance(e.touches)/Math.max(1,dist));}
          else if(e.touches.length===1){const t=e.touches[0],dx=t.clientX-sx,dy=t.clientY-sy;if(Math.hypot(dx,dy)>8){moved=true;cancelPress();}
            if(zoom>1.02){cx-=(t.clientX-lx)*box.w/zoom/deck.clientWidth;cy-=(t.clientY-ly)*box.h/zoom/deck.clientHeight;render();}
            else if(Math.abs(dx)>Math.abs(dy)*1.2){const hasPage=dx>0?next.childElementCount:previous.childElementCount;position(hasPage?dx:dx*.18);}lx=t.clientX;ly=t.clientY;
          }
        },{passive:false});
        deck.addEventListener('touchend',e=>{
          e.preventDefault();lastTouch=Date.now();cancelPress();if(e.touches.length||pending)return;
          if(longPressed){position(0,true);return;}if(multi){bridge('zoom/'+zoom.toFixed(3));return;}if(zoom>1.02){if(!moved)bridge('chrome/toggle');return;}
          const t=e.changedTouches[0],dx=t.clientX-sx,dy=t.clientY-sy,hasPage=dx>0?next.childElementCount:previous.childElementCount;
          if(hasPage&&Math.abs(dx)>Math.max(48,deck.clientWidth*.18)&&Math.abs(dx)>Math.abs(dy)*1.5){pending=true;position(dx>0?deck.clientWidth:-deck.clientWidth,true);setTimeout(()=>{if(pending)bridge('page/'+(dx>0?'next':'previous'));},195);}
          else{position(0,true);if(!moved)bridge('chrome/toggle');}
        },{passive:false});
        deck.addEventListener('touchcancel',()=>{cancelPress();position(0,true);multi=false;moved=true;});
        deck.addEventListener('contextmenu',e=>e.preventDefault());
        deck.addEventListener('click',e=>{if(Date.now()-lastTouch<650||pending)return;e.preventDefault();bridge('chrome/toggle');});
        window.addEventListener('blur',()=>{cancelPress();position(0);});
        initBox();previous.style.visibility=previous.childElementCount?'visible':'hidden';next.style.visibility=next.childElementCount?'visible':'hidden';window.setPalette({{{theme}}});
        </script></body></html>
        """;
    }
}
