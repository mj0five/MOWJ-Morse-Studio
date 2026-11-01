using System;using System.IO;using System.Drawing;using System.Windows.Forms;using System.Reflection;using System.Linq;
class UninstallLayoutChecks{
 [STAThread]static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string dir=args[0];Directory.CreateDirectory(dir);
  foreach(float scale in new[]{1f,1.25f,1.5f,2f}){
   using(var f=new SetupForm(new string[0])){
    typeof(SetupForm).GetField("remove",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f,true);
    f.Text="MOWJ — Uninstall";
    f.ApplyDesignScale(scale);
    float actual=f.ClientSize.Width/1040f;
    var fields=BindingFlags.Instance|BindingFlags.NonPublic;
    var heading=(Label)typeof(SetupForm).GetField("heading",fields).GetValue(f);
    var close=(Button)typeof(SetupForm).GetField("close",fields).GetValue(f);
    var min=(Button)typeof(SetupForm).GetField("min",fields).GetValue(f);
    var primary=(Button)typeof(SetupForm).GetField("primary",fields).GetValue(f);
    if(heading.Left<365*actual+25*actual)throw new Exception("Heading crosses the panel boundary.");
    if(close.Right>f.ClientSize.Width-5*actual||min.Right>=close.Left)throw new Exception("Caption buttons clipped or overlap.");
    if(primary.Left<365*actual+25*actual||primary.Right>f.ClientSize.Width)throw new Exception("Primary button out of bounds.");
    if(Math.Abs(heading.Font.Size-31f*96f/72f*actual)>1)throw new Exception("Font scale is inconsistent.");
    foreach(int state in new[]{0,2,3}){
     typeof(SetupForm).GetMethod("ShowState",fields).Invoke(f,new object[]{state});
     using(var b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)){
      f.DrawToBitmap(b,new Rectangle(Point.Empty,f.ClientSize));
      string[] names=state==0?new[]{"close","min","heading","description","primary"}:state==1?new[]{"close","min","heading","pathLabel","location","browse","desktop","status","primary","back"}:state==2?new[]{"close","min","heading","description","bar","status","primary"}:new[]{"close","min","heading","description","status","primary","back"};
      using(var bg=Graphics.FromImage(b))foreach(string name in names){var c=(Control)typeof(SetupForm).GetField(name,fields).GetValue(f);using(var cb=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(cb,new Rectangle(Point.Empty,c.Size));bg.DrawImageUnscaled(cb,c.Left,c.Top);}}
      b.Save(Path.Combine(dir,"uninstaller-"+(int)(scale*100)+"-state-"+state+".png"));
     }
    }
    Console.WriteLine("PASS "+(int)(scale*100)+"%: actual "+f.ClientSize.Width+"x"+f.ClientSize.Height+", heading x="+heading.Left+", caption right="+close.Right);
   }
  }return 0;
 }
}
