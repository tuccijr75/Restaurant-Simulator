using Godot;
using System;

namespace RestaurantSimulator;

public static class DashTheme{
 public static readonly Color Background=new Color(0.05f,0.06f,0.075f);
 public static readonly Color Panel=new Color(0.10f,0.12f,0.145f);
 public static readonly Color PanelSoft=new Color(0.13f,0.15f,0.18f);
 public static readonly Color Border=new Color(0.25f,0.31f,0.38f);
 public static readonly Color Accent=new Color(0.12f,0.48f,0.78f);
 public static readonly Color AccentDark=new Color(0.08f,0.32f,0.52f);
 public static readonly Color Text=new Color(0.93f,0.96f,0.98f);
 public static readonly Color Muted=new Color(0.68f,0.75f,0.80f);
 public static readonly Color Warn=new Color(1.0f,0.73f,0.28f);
 public static readonly Color Danger=new Color(1.0f,0.32f,0.26f);

 public static StyleBoxFlat Box(Color bg,Color border,int radius=10,int width=1){
  var b=new StyleBoxFlat{BgColor=bg,BorderColor=border};
  b.BorderWidthLeft=width;b.BorderWidthRight=width;b.BorderWidthTop=width;b.BorderWidthBottom=width;
  b.CornerRadiusTopLeft=radius;b.CornerRadiusTopRight=radius;b.CornerRadiusBottomLeft=radius;b.CornerRadiusBottomRight=radius;
  return b;
 }

 public static void Pad(MarginContainer m,int v){
  m.AddThemeConstantOverride("margin_left",v);m.AddThemeConstantOverride("margin_right",v);
  m.AddThemeConstantOverride("margin_top",v);m.AddThemeConstantOverride("margin_bottom",v);
 }

 public static void StyleLabel(Label l,int size=12,Color? color=null,bool strong=false){
  l.AddThemeFontSizeOverride("font_size",size);
  l.AddThemeColorOverride("font_color",color??Text);
  if(strong)l.AddThemeColorOverride("font_outline_color",new Color(0,0,0,0.35f));
 }

 public static Button Button(string text,Action pressed,bool primary=false){
  var b=new Button{Text=text,CustomMinimumSize=new Vector2(48,28)};
  b.Pressed+=pressed;
  StyleButton(b,primary);
  return b;
 }

 public static void StyleButton(Button b,bool primary=false){
  var normal=primary?Accent:PanelSoft;
  var hover=primary?new Color(0.16f,0.56f,0.90f):new Color(0.18f,0.21f,0.25f);
  var pressed=primary?AccentDark:new Color(0.08f,0.10f,0.12f);
  b.AddThemeFontSizeOverride("font_size",12);
  b.AddThemeColorOverride("font_color",Text);
  b.AddThemeStyleboxOverride("normal",Box(normal,primary?Accent:Border,8));
  b.AddThemeStyleboxOverride("hover",Box(hover,primary?Accent:Border,8));
  b.AddThemeStyleboxOverride("pressed",Box(pressed,Border,8));
 }

 public static string Preview(string text,int maxLines=5,int maxChars=360){
  if(string.IsNullOrEmpty(text))return "—";
  var clean=text.Replace("\r","").Trim();
  var lines=clean.Split('\n',StringSplitOptions.RemoveEmptyEntries);
  var output="";
  for(var i=0;i<lines.Length&&i<maxLines;i++)output+=lines[i]+"\n";
  output=output.TrimEnd();
  if(output.Length>maxChars)output=output[..maxChars]+"…";
  return output;
 }
}
