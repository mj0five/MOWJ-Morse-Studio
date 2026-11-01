using System;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;

[assembly: AssemblyTitle("MOWJ Morse Studio Setup")]
[assembly: AssemblyProduct("MOWJ Morse Studio")]
[assembly: AssemblyVersion("0.4.1.0")]
[assembly: AssemblyFileVersion("0.4.1.0")]


class ChromeButton:Button {
 readonly bool close;
 public ChromeButton(bool closing){close=closing;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  float cx=ClientSize.Width/2f,cy=ClientSize.Height/2f,r=Math.Min(ClientSize.Width,ClientSize.Height)*.17f;
  using(var pen=new Pen(Enabled?ForeColor:Color.Gray,Math.Max(1.2f,ClientSize.Width/38f))){
   if(close){e.Graphics.DrawLine(pen,cx-r,cy-r,cx+r,cy+r);e.Graphics.DrawLine(pen,cx-r,cy+r,cx+r,cy-r);}
   else e.Graphics.DrawLine(pen,cx-r,cy,cx+r,cy);
  }
  using(var pen=new Pen(FlatAppearance.BorderColor,1))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
  if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-4,-4));
 }
}

class InstallRecord {
 public string Product="MOWJ.MorseStudio"; public string Root; public string Version="0.4.1";
 public bool Qa; public bool Desktop; public List<string> Files=new List<string>();
}
class SetupForm:Form {
 const string Version="0.4.1", Marker=".mowj-install.json";
 Color Navy=Color.FromArgb(12,23,31),Lime=Color.FromArgb(211,247,128),Muted=Color.FromArgb(156,177,188);
 Image logo; Button primary,back,browse,close,min; Label heading,description,status,pathLabel;TextBox location;CheckBox desktop;
 readonly Dictionary<Control,Rectangle> designBounds=new Dictionary<Control,Rectangle>();
 readonly Dictionary<Control,Font> designFonts=new Dictionary<Control,Font>();
 bool layoutCaptured=false;
 Panel bar; int progress=0,state=0;bool busy=false,remove=false,qa=false;string root,qaRoot;InstallRecord record;
 JavaScriptSerializer json=new JavaScriptSerializer();BackgroundWorker worker;
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,int w,int l);
 public SetupForm(string[] args){
  for(int i=0;i<args.Length;i++){if(args[i]=="--qa-root"&&i+1<args.Length){qa=true;qaRoot=Path.GetFullPath(args[++i]);}if(args[i]=="--remove-root"&&i+1<args.Length){remove=true;root=Path.GetFullPath(args[++i]);}}
  Text=remove?"MOWJ — Uninstall":"MOWJ — Setup";ClientSize=new Size(1040,620);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.None;
  BackColor=Navy;ForeColor=Color.FromArgb(239,245,242);Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;
  using(var st=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))logo=Image.FromStream(st);
  using(var st=Assembly.GetExecutingAssembly().GetManifestResourceStream("mowj.ico"))Icon=new Icon(st);
  close=ButtonAt("×",985,5,44,32,false);close.Font=new Font("Segoe UI",17);close.Click+=(s,e)=>Close();
  min=ButtonAt("—",938,5,42,32,false);min.Click+=(s,e)=>WindowState=FormWindowState.Minimized;
  heading=LabelAt("",404,135,590,125,31,FontStyle.Regular);
  description=LabelAt("",408,270,552,80,12,FontStyle.Regular);description.ForeColor=Muted;
  pathLabel=LabelAt("INSTALL LOCATION",408,280,500,22,9,FontStyle.Regular);pathLabel.ForeColor=Muted;
  location=new TextBox{Left=408,Top=311,Width=440,Height=32,BackColor=Color.FromArgb(25,42,53),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10)};
  Controls.Add(location);
  browse=ButtonAt("Browse",856,308,110,34,false);browse.Click+=(s,e)=>{using(var f=new FolderBrowserDialog{Description="Choose the MOWJ installation folder",SelectedPath=location.Text})if(f.ShowDialog(this)==DialogResult.OK)location.Text=Path.Combine(f.SelectedPath,"MOWJ");};
  desktop=new CheckBox{Left=408,Top=368,Width=510,Height=28,Text="Create a desktop shortcut",Checked=true,ForeColor=ForeColor,BackColor=Navy};Controls.Add(desktop);
  status=LabelAt("",408,395,555,55,11,FontStyle.Regular);status.ForeColor=Muted;
  bar=new Panel{Left=408,Top=344,Width=550,Height=12,BackColor=Color.FromArgb(39,57,68)};bar.Paint+=(s,e)=>{e.Graphics.FillRectangle(new SolidBrush(Lime),0,0,bar.Width*progress/100,bar.Height);};Controls.Add(bar);
  primary=ButtonAt("",408,470,310,52,true);primary.Font=new Font("Segoe UI",13,FontStyle.Bold);primary.Click+=Primary;
  back=ButtonAt("Back",735,477,125,40,false);back.Click+=(s,e)=>{if(state==1)ShowState(0);else Close();};
  root=root??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MOWJ");
  if(qa&&!remove)root=Path.Combine(qaRoot,"MOWJ");
  location.Text=root;
  FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};
  MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left&&e.Y<44*ClientSize.Width/1040f){ReleaseCapture();SendMessage(Handle,0xA1,2,0);}};
  if(remove){try{record=ReadRecord(root);qa=record.Qa;if(qa)qaRoot=Path.GetDirectoryName(root);}catch(Exception ex){Shown+=(s,e)=>{MessageBox.Show(this,ex.Message,"MOWJ",MessageBoxButtons.OK,MessageBoxIcon.Error);Close();};}}
  ShowState(0);
  CaptureDesignLayout();
  float scale;
  using(var screenGraphics=Graphics.FromHwnd(IntPtr.Zero))scale=screenGraphics.DpiX/96f;
  for(int i=0;i<args.Length-1;i++)if(args[i]=="--qa-scale")float.TryParse(args[i+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out scale);
  var area=Screen.FromPoint(Cursor.Position).WorkingArea;
  scale=Math.Max(.75f,Math.Min(Math.Max(1f,scale),Math.Min((area.Width-32)/1040f,(area.Height-32)/620f)));
  ApplyDesignScale(scale);
 }
 void CaptureDesignLayout(){
  location.AutoSize=false;location.SetBounds(408,311,440,32);
  foreach(Control c in Controls){designBounds[c]=c.Bounds;designFonts[c]=(Font)c.Font.Clone();}
  layoutCaptured=true;
 }
 internal void ApplyDesignScale(float scale){
  if(!layoutCaptured)return;
  var area=Screen.FromPoint(Cursor.Position).WorkingArea;
  scale=Math.Max(.75f,Math.Min(scale,Math.Min((area.Width-32)/1040f,(area.Height-32)/620f)));
  SuspendLayout();
  ClientSize=new Size((int)Math.Round(1040*scale),(int)Math.Round(620*scale));
  var nativeHandle=Handle;
  float actual=Math.Min(ClientSize.Width/1040f,ClientSize.Height/620f);
  foreach(var entry in designBounds){
   var c=entry.Key;var b=entry.Value;var f=designFonts[c];
   Font previous=c.Font;
   c.Font=new Font(f.FontFamily,f.SizeInPoints*96f/72f*actual,f.Style,GraphicsUnit.Pixel);
   c.SetBounds((int)Math.Round(b.X*actual),(int)Math.Round(b.Y*actual),(int)Math.Round(b.Width*actual),(int)Math.Round(b.Height*actual));
  }
  ResumeLayout(false);Invalidate(true);
 }

 Label LabelAt(string t,int x,int y,int w,int h,int sz,FontStyle style){var l=new Label{Text=t,Left=x,Top=y,Width=w,Height=h,Font=new Font("Segoe UI",sz,style),BackColor=Color.Transparent,ForeColor=ForeColor};Controls.Add(l);return l;}
 Button ButtonAt(string t,int x,int y,int w,int h,bool accent){
  Button b=(t=="×"||t=="—")?new ChromeButton(t=="×"):new Button();
  b.Text=t;b.SetBounds(x,y,w,h);b.FlatStyle=FlatStyle.Flat;
  b.BackColor=accent?Lime:Color.FromArgb(21,35,45);
  b.ForeColor=accent?Color.FromArgb(15,28,24):Color.FromArgb(228,239,237);b.Cursor=Cursors.Hand;
  b.AccessibleName=t=="×"?"Close installer":t=="—"?"Minimize installer":t;
  b.FlatAppearance.BorderColor=accent?Lime:Color.FromArgb(53,74,86);Controls.Add(b);return b;
 }
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float k=ClientSize.Width/1040f;g.ScaleTransform(k,k);
  g.FillRectangle(new SolidBrush(Color.FromArgb(15,28,37)),0,0,1040,44);g.DrawImage(logo,14,7,30,30);
  g.DrawString("MOWJ   Morse Studio "+(remove?"Uninstaller":"Installer"),new Font("Segoe UI",13.333f,FontStyle.Regular,GraphicsUnit.Pixel),Brushes.White,53,12);
  var rect=new Rectangle(0,45,365,575);using(var b=new LinearGradientBrush(rect,Color.FromArgb(29,49,48),Color.FromArgb(10,23,31),45))g.FillRectangle(b,rect);
  using(var gp=new GraphicsPath()){gp.AddEllipse(-40,125,445,390);using(var glow=new PathGradientBrush(gp)){glow.CenterColor=Color.FromArgb(60,179,213,91);glow.SurroundColors=new[]{Color.Transparent};g.FillPath(glow,gp);}}
  using(var pen=new Pen(Color.FromArgb(55,198,226,145),1)){for(int j=0;j<5;j++)g.DrawBezier(pen,-30,170+j*38,110,40+j*36,160,400+j*30,405,230+j*30);}
  g.DrawImage(logo,new Rectangle(61,191,245,245));
  g.DrawString("M O W J   /   M O R S E   S T U D I O",new Font("Segoe UI",10.667f,FontStyle.Regular,GraphicsUnit.Pixel),new SolidBrush(Muted),52,522);
  g.DrawString("OFFLINE EDITION  "+Version+(qa?"  /  TEST INSTALL":""),new Font("Consolas",10.667f,FontStyle.Regular,GraphicsUnit.Pixel),new SolidBrush(Muted),55,547);
  g.DrawLine(new Pen(Color.FromArgb(43,63,73)),365,45,365,620);
  if(state==2||state==3){int x=410,y=420;g.FillEllipse(new SolidBrush(Lime),x,y,10,10);for(int j=0;j<3;j++)g.FillRectangle(Brushes.Ivory,x+23+j*36,y+2,27,7);}
  g.DrawRectangle(new Pen(Color.FromArgb(54,75,85)),0,0,1039,619);
 }
 void ShowState(int value){
  state=value;location.Visible=pathLabel.Visible=browse.Visible=desktop.Visible=value==1;bar.Visible=value==2;status.Visible=value>0;description.Visible=value!=1;back.Visible=value==1||value==3;primary.Enabled=true;close.Enabled=true;
  if(value==0){heading.Text=remove?"Close this station?":"A new language.\nA new connection.";description.Text=remove?"Remove MOWJ Morse Studio from this computer.\nYour profile and practice records will be kept.":"MOWJ Morse Studio.\nLearn, listen and find your rhythm.";primary.Text=remove?"Uninstall MOWJ":"Install MOWJ   →";}
  if(value==1){heading.Text="Make it yours.";primary.Text="Begin installation   →";status.Text="Windows 10 / 11 · 64-bit\nOffline installation · No administrator account required";}
  if(value==2){heading.Text=remove?"Closing the station.":"Building your station.";description.Text=remove?"Removing application files…":"Installing MOWJ Morse Studio…";primary.Enabled=false;back.Visible=false;close.Enabled=false;}
  if(value==3){heading.Text=remove?"Station closed.":"Your station is ready.";description.Text=remove?"MOWJ has been removed.\nYour practice records remain on this computer.":"MOWJ Morse Studio is ready to use.\nYour first signal is waiting.";primary.Text=remove?"Done":"Launch MOWJ   →";back.Text="Finish";status.Text=remove?"You can reinstall later and continue your practice.":root;}
  Invalidate();
 }
 void Primary(object sender,EventArgs args){
  if(state==0){if(remove)StartWork();else ShowState(1);return;}
  if(state==1){try{root=ValidateTarget(location.Text);StartWork();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Choose another location",MessageBoxButtons.OK,MessageBoxIcon.Information);}return;}
  if(state==3){if(!remove){try{Process.Start(new ProcessStartInfo(Path.Combine(root,"MOWJ.exe")){WorkingDirectory=root,UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message);return;}}Close();}
 }
 static string Full(string p){return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar);}
 string ValidateTarget(string text){
  if(String.IsNullOrWhiteSpace(text))throw new Exception("Choose a folder for MOWJ.");
  var p=Full(text);if(p.StartsWith(@"\\")||p==Full(Path.GetPathRoot(p)))throw new Exception("Choose a local application folder, not a drive root.");
  foreach(var special in new[]{Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)})
   if(!String.IsNullOrWhiteSpace(special)&&p.Equals(Full(special),StringComparison.OrdinalIgnoreCase))throw new Exception("Choose a dedicated MOWJ folder.");
  if(Directory.Exists(p)&&Directory.GetFileSystemEntries(p).Length>0){ReadRecord(p);}
  return p;
 }
 static InstallRecord ReadRecord(string p){
  p=Full(p);var marker=Path.Combine(p,Marker);if(!File.Exists(marker))throw new Exception("This folder is not an existing MOWJ installation. Choose an empty folder.");
  var r=new JavaScriptSerializer().Deserialize<InstallRecord>(File.ReadAllText(marker));
  if(r==null||r.Product!="MOWJ.MorseStudio"||!String.Equals(Full(r.Root),p,StringComparison.OrdinalIgnoreCase)||r.Files==null)throw new Exception("The installation record is invalid.");
  foreach(var f in r.Files)SafePath(p,f);
  return r;
 }
 static string SafePath(string root,string relative){var p=Path.GetFullPath(Path.Combine(root,relative));if(!p.StartsWith(Full(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("An application file has an invalid path.");return p;}
 void StartWork(){
  foreach(var p in Process.GetProcessesByName("MOWJ")){try{if(String.Equals(Full(Path.GetDirectoryName(p.MainModule.FileName)),Full(root),StringComparison.OrdinalIgnoreCase)){MessageBox.Show(this,"Close MOWJ before continuing.","MOWJ");return;}}catch{}}
  busy=true;ShowState(2);worker=new BackgroundWorker{WorkerReportsProgress=true};worker.ProgressChanged+=(s,e)=>{progress=e.ProgressPercentage;status.Text=(string)e.UserState;bar.Invalidate();};
  worker.DoWork+=(s,e)=>{if(remove)Uninstall(worker);else Install(worker);};
  worker.RunWorkerCompleted+=(s,e)=>{busy=false;if(e.Error!=null){MessageBox.Show(this,e.Error.Message,"MOWJ setup could not finish",MessageBoxButtons.OK,MessageBoxIcon.Error);ShowState(remove?0:1);}else{progress=100;ShowState(3);}};
  worker.RunWorkerAsync();
 }
 void Install(BackgroundWorker w){
  string stage=root+".install-"+Guid.NewGuid().ToString("N"),backup=null;bool committed=false;
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(root));
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")){
    if(stream==null)throw new Exception("The installation payload is missing.");
    using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
     long total=0,completed=0;foreach(var entry in zip.Entries)total+=entry.Length;
     var drive=new DriveInfo(Path.GetPathRoot(root));if(drive.AvailableFreeSpace<total+100*1024*1024)throw new Exception("There is not enough free space for MOWJ.");
     Directory.CreateDirectory(stage);record=new InstallRecord{Root=root,Qa=qa,Desktop=desktop.Checked};
     foreach(var entry in zip.Entries){
      string dest=SafePath(stage,entry.FullName);if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(dest);continue;}
      Directory.CreateDirectory(Path.GetDirectoryName(dest));using(var source=entry.Open())using(var target=File.Create(dest)){var buf=new byte[131072];int n;while((n=source.Read(buf,0,buf.Length))>0){target.Write(buf,0,n);completed+=n;} }
      record.Files.Add(entry.FullName.Replace('/',Path.DirectorySeparatorChar));w.ReportProgress((int)Math.Min(94,completed*94/Math.Max(1,total)),"Installing "+Path.GetFileName(dest));
     }
    }
   }
   File.WriteAllText(Path.Combine(stage,Marker),json.Serialize(record));record.Files.Add(Marker);File.WriteAllText(Path.Combine(stage,Marker),json.Serialize(record));
   if(Directory.Exists(root)){if(Directory.GetFileSystemEntries(root).Length>0){ReadRecord(root);backup=root+".previous-"+DateTime.Now.ToString("yyyyMMddHHmmss");Directory.Move(root,backup);}else Directory.Delete(root);}
   Directory.Move(stage,root);committed=true;w.ReportProgress(96,"Creating shortcuts…");
   CreateShortcuts();Register();w.ReportProgress(100,"Installation complete.");
  }catch{
   if(committed)RemoveOwnedFiles(root,record);
   if(backup!=null&&Directory.Exists(backup)&&!Directory.Exists(root))Directory.Move(backup,root);
   throw;
  }finally{
   if(Directory.Exists(stage)){var verified=Full(stage);if(verified.StartsWith(Full(root)+".install-",StringComparison.OrdinalIgnoreCase))Directory.Delete(verified,true);}
  }
 }
 string RegistryName {get{return @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MOWJDesktop"+(qa?"-QA":"");}}
 string DesktopLink {get{return qa?Path.Combine(qaRoot,"Desktop","MOWJ Morse Studio.lnk"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"MOWJ Morse Studio.lnk");}}
 string StartFolder {get{return qa?Path.Combine(qaRoot,"StartMenu","MOWJ Morse Studio"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"MOWJ Morse Studio");}}
 static void Link(string file,string target,string icon,string arguments){
  Directory.CreateDirectory(Path.GetDirectoryName(file));var type=Type.GetTypeFromProgID("WScript.Shell");var shell=Activator.CreateInstance(type);
  var shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{file});var st=shortcut.GetType();
  st.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{target});st.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{Path.GetDirectoryName(target)});
  st.InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{icon});st.InvokeMember("Arguments",BindingFlags.SetProperty,null,shortcut,new object[]{arguments});st.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
  Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);
 }
 void CreateShortcuts(){string exe=Path.Combine(root,"MOWJ.exe"),ico=Path.Combine(root,"mowj.ico");if(record.Desktop)Link(DesktopLink,exe,ico,"");Link(Path.Combine(StartFolder,"MOWJ Morse Studio.lnk"),exe,ico,"");Link(Path.Combine(StartFolder,"Uninstall MOWJ.lnk"),Path.Combine(root,"Uninstall.exe"),ico,"");}
 void Register(){using(var key=Registry.CurrentUser.CreateSubKey(RegistryName)){key.SetValue("DisplayName","MOWJ Morse Studio"+(qa?" (QA test)":""));key.SetValue("DisplayVersion",Version);key.SetValue("Publisher","MOWJ");key.SetValue("InstallLocation",root);key.SetValue("DisplayIcon",Path.Combine(root,"mowj.ico"));key.SetValue("UninstallString","\""+Path.Combine(root,"Uninstall.exe")+"\"");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);key.SetValue("EstimatedSize",450000,RegistryValueKind.DWord);}}
 void Uninstall(BackgroundWorker w){
  record=ReadRecord(root);qa=record.Qa;if(qa)qaRoot=Path.GetDirectoryName(root);
  string exe=Path.Combine(root,"MOWJ.exe");
  RemoveLink(DesktopLink,exe);RemoveLink(Path.Combine(StartFolder,"MOWJ Morse Studio.lnk"),exe);RemoveLink(Path.Combine(StartFolder,"Uninstall MOWJ.lnk"),Path.Combine(root,"Uninstall.exe"));
  if(Directory.Exists(StartFolder)&&Directory.GetFileSystemEntries(StartFolder).Length==0)Directory.Delete(StartFolder);
  for(int i=0;i<record.Files.Count;i++){string f=SafePath(root,record.Files[i]);if(File.Exists(f))File.Delete(f);w.ReportProgress((i+1)*100/record.Files.Count,"Removing application files…");}
  RemoveEmpty(root);Registry.CurrentUser.DeleteSubKeyTree(RegistryName,false);
 }
 static void RemoveLink(string file,string expected){
  if(!File.Exists(file))return;var type=Type.GetTypeFromProgID("WScript.Shell");var sh=Activator.CreateInstance(type);var link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,sh,new object[]{file});
  string target=(string)link.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null);
  Marshal.FinalReleaseComObject(link);Marshal.FinalReleaseComObject(sh);
  if(String.Equals(Full(target),Full(expected),StringComparison.OrdinalIgnoreCase))File.Delete(file);
 }
 static void RemoveOwnedFiles(string root,InstallRecord r){if(r==null)return;foreach(var rel in r.Files){var p=SafePath(root,rel);if(File.Exists(p))File.Delete(p);}RemoveEmpty(root);}
 static void RemoveEmpty(string root){if(!Directory.Exists(root)||(File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)return;foreach(var d in Directory.GetDirectories(root))RemoveEmpty(d);if(Directory.GetFileSystemEntries(root).Length==0)Directory.Delete(root);}
 public void RunQa(bool deleting){
  if(!qa||String.IsNullOrEmpty(qaRoot))throw new Exception("QA mode needs a dedicated test folder.");
  Directory.CreateDirectory(qaRoot);string mark=Path.Combine(qaRoot,".mowj-qa");
  root=Path.Combine(qaRoot,"MOWJ");
  if(deleting){if(!File.Exists(mark))throw new Exception("Not a QA workspace.");record=ReadRecord(root);if(!record.Qa)throw new Exception("Not a QA installation.");}else File.WriteAllText(mark,"MOWJ installer QA workspace");
  var w=new BackgroundWorker{WorkerReportsProgress=true};if(deleting)Uninstall(w);else {root=ValidateTarget(root);Install(w);}
  File.WriteAllText(Path.Combine(qaRoot,deleting?"uninstall-result.txt":"install-result.txt"),"PASS "+DateTime.UtcNow.ToString("o"));
 }
 [STAThread]static void Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  if(Path.GetFileName(Application.ExecutablePath).Equals("Uninstall.exe",StringComparison.OrdinalIgnoreCase)){
   string original=Path.GetDirectoryName(Application.ExecutablePath),temp=Path.Combine(Path.GetTempPath(),"MOWJ-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");
   try{ReadRecord(original);File.Copy(Application.ExecutablePath,temp);Process.Start(new ProcessStartInfo(temp,"--remove-root \""+original+"\""){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"MOWJ");}return;
  }
  bool qi=Array.IndexOf(args,"--qa-install")>=0,qr=Array.IndexOf(args,"--qa-remove")>=0;
  if(qi||qr){try{using(var f=new SetupForm(args))f.RunQa(qr);}catch(Exception e){Console.Error.WriteLine(e.ToString());int a=Array.IndexOf(args,"--qa-root");if(a>=0&&a+1<args.Length){Directory.CreateDirectory(args[a+1]);File.WriteAllText(Path.Combine(args[a+1],"qa-error.txt"),e.ToString());}Environment.ExitCode=1;}return;}
  Application.Run(new SetupForm(args));
 }
}
