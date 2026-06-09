using Godot;

namespace RestaurantSimulator;

public partial class MainDashboard:Control{
 public override void _Ready(){
  var st=new SimRunState();
  AddBackground();

  var center=new CenterContainer();
  center.SetAnchorsPreset(LayoutPreset.FullRect);
  AddChild(center);

  var shell=new MarginContainer{CustomMinimumSize=new Vector2(1160,900)};
  DashTheme.Pad(shell,18);
  center.AddChild(shell);

  var stack=new VBoxContainer();
  stack.AddThemeConstantOverride("separation",12);
  shell.AddChild(stack);

  var header=new Label{Text="Restaurant Simulator  •  Operations Command Dashboard",HorizontalAlignment=HorizontalAlignment.Center};
  DashTheme.StyleLabel(header,22,DashTheme.Text,true);
  stack.AddChild(header);

  var sub=new Label{Text="Standalone synthetic shift simulator | ASC optional | deterministic replay/export",HorizontalAlignment=HorizontalAlignment.Center};
  DashTheme.StyleLabel(sub,12,DashTheme.Muted);
  stack.AddChild(sub);

  var scroll=new ScrollContainer{CustomMinimumSize=new Vector2(1120,790),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
  stack.AddChild(scroll);

  var grid=new GridContainer{Columns=3};
  grid.AddThemeConstantOverride("h_separation",12);
  grid.AddThemeConstantOverride("v_separation",12);
  scroll.AddChild(grid);

  var sc=new ScenarioPanel();sc.Bind(st);var c=new ClockPanel();c.Bind(st);
  var a=new AlertPanel();a.Bind(st);var p=new PosPanel();p.Bind(st);var k=new KdsPanel();k.Bind(st);
  var sp=new StationPanel();sp.Bind(st);var cov=new CoveragePanel();cov.Bind(st);var lab=new LaborPanel();lab.Bind(st);var sl=new StaffingLedgerPanel();sl.Bind(st);
  var inv=new InventoryPanel();inv.Bind(st);var ic=new InventoryControlPanel();ic.Bind(st);var san=new SanitationPanel();san.Bind(st);
  var temp=new TemperaturePanel();temp.Bind(st);var so=new SosPanel();so.Bind(st);var e=new EventPanel();e.Bind(st);
  var j=new JsonlPanel();j.Bind(st);var x=new ExportPanel();x.Bind(st);

  grid.AddChild(sc);grid.AddChild(c);grid.AddChild(a);
  grid.AddChild(p);grid.AddChild(k);grid.AddChild(sp);
  grid.AddChild(cov);grid.AddChild(lab);grid.AddChild(sl);
  grid.AddChild(inv);grid.AddChild(ic);grid.AddChild(san);
  grid.AddChild(temp);grid.AddChild(so);grid.AddChild(e);
  grid.AddChild(j);grid.AddChild(x);
 }

 void AddBackground(){
  var bg=new ColorRect{Color=DashTheme.Background};
  bg.SetAnchorsPreset(LayoutPreset.FullRect);
  AddChild(bg);
 }
}
