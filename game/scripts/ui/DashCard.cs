using Godot;

namespace RestaurantSimulator;

public partial class DashCard:PanelContainer{
 protected VBoxContainer Body=new();
 protected string CardTitle="";
 Label title=new();

 public DashCard(){CustomMinimumSize=new Vector2(330,170);SizeFlagsHorizontal=SizeFlags.ExpandFill;SizeFlagsVertical=SizeFlags.ShrinkBegin;}

 public override void _Ready(){
  AddThemeStyleboxOverride("panel",DashTheme.Box(DashTheme.Panel,DashTheme.Border,12,1));
  var margin=new MarginContainer();DashTheme.Pad(margin,10);AddChild(margin);
  Body.AddThemeConstantOverride("separation",6);margin.AddChild(Body);
  title.Text=string.IsNullOrEmpty(CardTitle)?Name.Replace("Panel",""):CardTitle;
  DashTheme.StyleLabel(title,15,DashTheme.Text,true);Body.AddChild(title);
 }

 protected Label StatusLabel(string text=""){
  var l=new Label{Text=text,CustomMinimumSize=new Vector2(0,42)};
  l.AutowrapMode=TextServer.AutowrapMode.WordSmart;
  l.ClipText=true;
  DashTheme.StyleLabel(l,12,DashTheme.Muted);
  Body.AddChild(l);
  return l;
 }

 protected HBoxContainer Row(){
  var r=new HBoxContainer();r.AddThemeConstantOverride("separation",5);Body.AddChild(r);return r;
 }

 protected Button AddButton(string text,System.Action action,bool primary=false){
  var b=DashTheme.Button(text,action,primary);Body.AddChild(b);return b;
 }

 protected void AddRowButton(HBoxContainer row,string text,System.Action action,bool primary=false){
  var b=DashTheme.Button(text,action,primary);b.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(b);
 }
}
