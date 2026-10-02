using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

sealed class FloatingReminder : Form {
 readonly ClockState[] clocks; readonly Action<int> act,toggleOne; readonly Action toggle,open;
 readonly Timer animation=new Timer(); readonly ToolTip tip=new ToolTip();
 bool expanded,active,dragging;Point pressed,origin;float scale=1;int targetWidth,targetHeight;PonyTheme theme;
 public FloatingReminder(ClockState[] states,Action<int> action,Action<int> single,Action pause,Action show,Action quit,PonyTheme current){
 clocks=states;act=action;toggleOne=single;toggle=pause;open=show;theme=current;
 FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;BackColor=theme.Bubble;
 using(var g=CreateGraphics())scale=Math.Max(1,g.DpiX/96f);Size=new Size(Px(72),Px(72));var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-Px(24),area.Top+Px(140));SetShape();
 var menu=new ContextMenuStrip();menu.Items.Add("打开主窗口",null,(s,e)=>open());menu.Items.Add("全部开启 / 暂停",null,(s,e)=>toggle());menu.Items.Add("展开 / 收起",null,(s,e)=>Expand(!expanded));menu.Items.Add("退出",null,(s,e)=>quit());ContextMenuStrip=menu;
 tip.SetToolTip(this,"拖动移动 · 单击展开 · 右键打开主窗口");animation.Interval=16;animation.Tick+=(s,e)=>Animate();
 MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;pressed=Cursor.Position;origin=Location;dragging=false;Capture=true;};
 MouseMove+=(s,e)=>{if(!Capture||e.Button!=MouseButtons.Left)return;Point p=Cursor.Position;int dx=p.X-pressed.X,dy=p.Y-pressed.Y;if(Math.Abs(dx)+Math.Abs(dy)>Px(5))dragging=true;if(dragging)Location=new Point(origin.X+dx,origin.Y+dy);};
 MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;Capture=false;if(dragging){Clamp();return;}ClickAt(e.X/scale,e.Y/scale);};
 FormClosed+=(s,e)=>{animation.Stop();animation.Dispose();tip.Dispose();};
 }
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x02E0){scale=Math.Max(1,((long)m.WParam&0xffff)/96f);if(IsHandleCreated)BeginInvoke((Action)(()=>{Size=new Size(Px(expanded?440:72),Px(expanded?148:72));Clamp();SetShape();Invalidate();}));}}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 int Px(float n){return (int)Math.Round(n*scale);}
 public void SetTheme(PonyTheme value){theme=value;BackColor=value.Bubble;Invalidate();}
 public void UpdateState(bool running){active=running;Invalidate();}
 public void Present(bool notify){if(!Visible)Show();if(notify)Expand(true);else Clamp();}
 public void Expand(bool value){expanded=value;var a=Screen.FromControl(this).WorkingArea;targetWidth=Math.Min(Px(value?440:72),a.Width);targetHeight=Px(value?148:72);animation.Start();}
 void Animate(){int right=Right,dw=targetWidth-Width,dh=targetHeight-Height;int w=Math.Abs(dw)<3?targetWidth:Width+(int)Math.Ceiling(Math.Abs(dw)*.28)*Math.Sign(dw);int h=Math.Abs(dh)<3?targetHeight:Height+(int)Math.Ceiling(Math.Abs(dh)*.28)*Math.Sign(dh);Bounds=new Rectangle(right-w,Top,w,h);Clamp();SetShape();Invalidate();if(w==targetWidth&&h==targetHeight)animation.Stop();}
 void Clamp(){var a=Screen.FromControl(this).WorkingArea;Location=new Point(Math.Max(a.Left,Math.Min(Left,a.Right-Width)),Math.Max(a.Top,Math.Min(Top,a.Bottom-Height)));}
 static GraphicsPath Rounded(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 void SetShape(){using(var p=Rounded(new RectangleF(0,0,Width-1,Height-1),Width<=Px(74)?(Math.Min(Width,Height)-1)/2f:Px(20))){var old=Region;Region=new Region(p);if(old!=null)old.Dispose();}}
 void TextAt(Graphics g,string text,float size,Color color,RectangleF bounds,bool bold,bool centered){using(var f=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(color))using(var sf=new StringFormat{Alignment=centered?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(text,f,b,bounds,sf);}
 internal GraphicsPath CenteredGlyph(float width){var glyph=new GraphicsPath();glyph.AddString("歇",Themes.CuteFont,(int)FontStyle.Regular,33,new PointF(0,0),StringFormat.GenericTypographic);using(var weight=new Pen(Color.Black,1.45f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round}){var outline=(GraphicsPath)glyph.Clone();outline.Widen(weight);glyph.AddPath(outline,false);outline.Dispose();glyph.FillMode=FillMode.Winding;}RectangleF b=glyph.GetBounds();using(var move=new Matrix()){move.Translate(width/2-b.X-b.Width/2,29-b.Y-b.Height/2);glyph.Transform(move);}return glyph;}
 static string Time(double seconds){int n=(int)Math.Ceiling(seconds);return (n/60).ToString("00")+":"+(n%60).ToString("00");}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(scale,scale);float w=Width/scale;
 if(w<145){using(var ring=new Pen(Color.FromArgb(105,Color.White),1.3f))g.DrawEllipse(ring,3,3,65,65);using(var glyph=CenteredGlyph(w))using(var stroke=new Pen(Color.FromArgb(70,theme.Accent),1f){LineJoin=LineJoin.Round})using(var fill=new SolidBrush(theme.Glyph)){g.DrawPath(stroke,glyph);g.FillPath(fill,glyph);}bool due=Array.Exists(clocks,c=>c.Enabled&&(c.Phase==1||c.Phase==3));double nearest=double.MaxValue;foreach(var c in clocks)if(c.Enabled)nearest=Math.Min(nearest,c.Left);
 string caption=!active?"暂停中":due?"该休息啦":Time(nearest==double.MaxValue?0:nearest);TextAt(g,caption,10.5f,theme.Ink,new RectangleF(3,46,w-6,19),false,true);if(due)using(var dot=new SolidBrush(theme.Glyph))g.FillEllipse(dot,w-17,11,6,6);return;}
 if(animation.Enabled)return;
 TextAt(g,"歇歇",14,theme.Ink,new RectangleF(16,4,48,24),true,false);TextAt(g,active?"提醒中":"已暂停",11,theme.Ink,new RectangleF(65,4,65,24),false,false);
 TextAt(g,active?"全停":"全开",12,theme.Ink,new RectangleF(w-160,4,43,24),false,true);TextAt(g,"主窗口",12,theme.Ink,new RectangleF(w-112,4,56,24),false,true);TextAt(g,"收起",12,theme.Ink,new RectangleF(w-52,4,40,24),false,true);
 for(int i=0;i<clocks.Length;i++){float y=33+i*36;var c=clocks[i];using(var row=Rounded(new RectangleF(9,y-1,w-18,34),11))using(var fill=new SolidBrush(Color.FromArgb(100,Color.White)))g.FillPath(fill,row);
 string info=!c.Enabled?"已暂停":c.Phase==1?(i==0?"看约 6 米外 · "+(int)c.Duration+" 秒":i==1?"舒展 · "+(int)(c.Duration/60)+" 分钟":"休息 · "+(int)(c.Duration/60)+" 分钟"):c.Phase==2?"休息中  "+Time(c.Left):c.Phase==3?"休息结束":"下一次  "+Time(c.Left);
 TextAt(g,c.Name,12,theme.Ink,new RectangleF(17,y,65,31),true,false);TextAt(g,info,12,theme.Ink,new RectangleF(86,y,w-254,31),false,false);
 TextAt(g,c.Enabled?"暂停":"开启",12,theme.Ink,new RectangleF(w-162,y,53,30),false,true);
 using(var button=Rounded(new RectangleF(w-103,y,86,30),10))using(var fill=new SolidBrush(c.Phase==2?Color.FromArgb(90,Color.White):theme.Glyph))g.FillPath(fill,button);
 TextAt(g,c.Phase==3?"返回工作":c.Phase==2?"休息中":c.Phase==1?"开始休息":"现在休息",12,theme.Ink,new RectangleF(w-103,y,86,30),true,true);
 }}
 void ClickAt(float x,float y){if(animation.Enabled)return;if(!expanded){Expand(true);return;}float w=Width/scale;if(y<30){if(x>=w-52)Expand(false);else if(x>=w-112)open();else if(x>=w-160)toggle();return;}for(int i=0;i<clocks.Length;i++){float row=33+i*36;if(x>=w-162&&x<w-109&&y>=row&&y<=row+30){toggleOne(i);return;}if(x>=w-103&&x<=w-17&&y>=row&&y<=row+30&&clocks[i].Phase!=2){act(i);if(!Array.Exists(clocks,c=>c.Enabled&&c.Phase!=0))Expand(false);return;}}}
}
