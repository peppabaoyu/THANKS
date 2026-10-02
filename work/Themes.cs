using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

sealed class PonyTheme {
 public string Id,Name,IconFile; public Color Bubble,Glyph,Ink,Accent,Page; public Image Picture; public Icon Icon;
 public PonyTheme(string id,string name,string bubble,string glyph,string ink,string accent,string page){Id=id;Name=name;Bubble=ColorTranslator.FromHtml(bubble);Glyph=ColorTranslator.FromHtml(glyph);Ink=ColorTranslator.FromHtml(ink);Accent=ColorTranslator.FromHtml(accent);Page=ColorTranslator.FromHtml(page);}
}
static class Themes {
 public static readonly PonyTheme[] All={
 new PonyTheme("fluttershy","柔柔","#F5AECA","#FFF4AA","#694252","#A64C76","#FFF8FB"),
 new PonyTheme("twilight","紫悦","#B393D5","#FAD0EB","#40245F","#7B459F","#FAF7FE"),
 new PonyTheme("rainbow","云宝","#94D9F0","#FFF2A6","#244C6B","#2779AB","#F4FCFF"),
 new PonyTheme("pinkie","碧琪","#F591C1","#FFF0B9","#682243","#B2397B","#FFF6FB"),
 new PonyTheme("rarity","珍奇","#B79CD8","#FFFAFF","#493466","#77529C","#FBF9FF"),
 new PonyTheme("spike","穗龙","#B38BD6","#D9F6AB","#49315B","#784BA0","#FAF8FC"),
 new PonyTheme("celestia","宇宙公主","#BDE5DC","#FFF4BD","#3F5B58","#967032","#FFFCF4")};
 static PrivateFontCollection rounded=new PrivateFontCollection(); static IntPtr fontMemory; public static FontFamily CuteFont;
 public static bool Ready {get{foreach(var theme in All)if(theme.Picture==null || theme.Icon==null)return false;return true;}}
 public static void Load(){foreach(var theme in All){using(var stream=Resource(theme.Id+".png")){if(stream!=null)using(var image=Image.FromStream(stream))theme.Picture=new Bitmap(image);}using(var stream=Resource(theme.Id+".ico")){if(stream!=null)theme.Icon=new Icon(stream);}}
 using(var font=Resource("rounded.ttf")){if(font!=null){byte[] bytes=new byte[font.Length];font.Read(bytes,0,bytes.Length);fontMemory=Marshal.AllocHGlobal(bytes.Length);Marshal.Copy(bytes,0,fontMemory,bytes.Length);rounded.AddMemoryFont(fontMemory,bytes.Length);CuteFont=rounded.Families[0];}}
 if(CuteFont==null)CuteFont=new FontFamily("Microsoft YaHei UI");}
 public static Stream Resource(string name){return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);}
 public static PonyTheme Find(string id){foreach(var t in All)if(t.Id==id)return t;return All[0];}
 public static string UpdateDesktopIcon(PonyTheme theme,string dataDirectory){
 object shell=null,shortcut=null;
 try{string icons=Path.Combine(dataDirectory,"icons");Directory.CreateDirectory(icons);string iconFile=theme.IconFile??Path.Combine(icons,theme.Id+"-round-20260919.ico");if(!File.Exists(iconFile))using(var input=Resource(theme.Id+".ico"))using(var output=File.Create(iconFile))input.CopyTo(output);
 string desktopLink=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"歇歇.lnk");if(!File.Exists(desktopLink))return "";
 Type type=Type.GetTypeFromProgID("WScript.Shell");shell=Activator.CreateInstance(type);shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{desktopLink});
 string target=(string)shortcut.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,shortcut,null);
 if(!string.Equals(Path.GetFullPath(target),Path.GetFullPath(Application.ExecutablePath),StringComparison.OrdinalIgnoreCase))return "";
 shortcut.GetType().InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{iconFile+",0"});shortcut.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
 SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);return "";
 }catch(Exception){return "角色已切换；桌面图标暂未同步，可稍后重试。";}
 finally{if(shortcut!=null && Marshal.IsComObject(shortcut))Marshal.FinalReleaseComObject(shortcut);if(shell!=null && Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);}}
 [DllImport("shell32.dll")] static extern void SHChangeNotify(uint eventId,uint flags,IntPtr item1,IntPtr item2);
}

sealed class ThemePicker : Form {
 public PonyTheme Selected;
 public ThemePicker(PonyTheme current){SuspendLayout();AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;Text="选一位今天的伙伴";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(620,375);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=current.Page;Icon=current.Icon;
 var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=4,RowCount=3};for(int i=0;i<4;i++)layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));layout.RowStyles.Add(new RowStyle(SizeType.Percent,50));layout.RowStyles.Add(new RowStyle(SizeType.Percent,50));
 var hint=new Label{Text="图标与悬浮球配色一起切换",Dock=DockStyle.Fill,ForeColor=current.Ink};layout.Controls.Add(hint,0,0);layout.SetColumnSpan(hint,4);
 for(int i=0;i<Themes.All.Length;i++){PonyTheme chosen=Themes.All[i];var cell=new Button{Text=chosen.Name+(chosen==current?"  ✓":""),Image=Thumbnail(chosen.Picture??Themes.All[0].Picture,72),TextImageRelation=TextImageRelation.ImageAboveText,ImageAlign=ContentAlignment.MiddleCenter,TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,BackColor=chosen==current?chosen.Bubble:Color.White,ForeColor=chosen.Ink,Margin=new Padding(4)};cell.FlatAppearance.BorderSize=chosen==current?2:0;cell.FlatAppearance.BorderColor=chosen.Accent;cell.Click+=(s,e)=>{Selected=chosen;DialogResult=DialogResult.OK;};layout.Controls.Add(cell,i%4,1+i/4);}
 Controls.Add(layout);ResumeLayout(true);}
 static Image Thumbnail(Image image,int size){var output=new Bitmap(size,size);using(var g=Graphics.FromImage(output)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,size,size);}return output;}
}

