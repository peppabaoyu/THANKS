using System;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

sealed class ClockState {
 public double Interval,Duration,Left;public int Phase;public bool Enabled=true;public string Name="";
 public ClockState(double interval,double duration){Interval=interval;Duration=duration;Reset();}
 public void Reset(){Phase=0;Left=Interval;}
 public bool Tick(double seconds){if(!Enabled||(Phase!=0&&Phase!=2))return false;Left=Math.Max(0,Left-seconds);if(Left>0)return false;Phase=Phase==0?1:3;return true;}
 public void Rest(){Phase=2;Left=Duration;}
}
sealed partial class Reminder : Form {
 internal static EventWaitHandle WakeEvent,ExitEvent;
 readonly SessionStore store;readonly bool syncDesktop;
 internal readonly ClockState[] States;
 readonly Label[] counts=new Label[3],statuses=new Label[3];readonly Button[] actions=new Button[3];
 readonly NumericUpDown[] intervals=new NumericUpDown[3],durations=new NumericUpDown[3];
 readonly Button[] switches=new Button[3];TextBox customName;Label global,today,sessionLabel,themeName;PictureBox avatar;Button pause,end,themeButton;CheckBox sound;
 internal FloatingReminder Floating;NotifyIcon tray;System.Windows.Forms.Timer timer;StatisticsForm statistics;
 PonyTheme theme;bool running,floatingMode,initializing=true,closing;string statusNote="";
 readonly Stopwatch elapsed=Stopwatch.StartNew();double last,lastSave,lastStats;
 public Reminder(string directory,bool desktopSync){
 syncDesktop=desktopSync;store=new SessionStore(directory);
 store.MigrateLegacy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"提醒设置.txt"));store.MigrateLegacy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"提醒设置.txt"));
 theme=CustomThemes.Load(directory).Find(t=>t.Id==store.Theme)??Themes.Find(store.Theme);if(theme.Icon==null)theme=Themes.All[0];States=new[]{new ClockState(store.EyeMinutes*60,store.EyeSeconds),new ClockState(store.MoveMinutes*60,store.MoveSeconds),new ClockState(store.CustomMinutes*60,store.CustomSeconds)};foreach(var state in States)state.Enabled=false;States[0].Name="远眺";States[1].Name="起身";States[2].Name=store.CustomName;
 BuildResponsiveUi();
 Floating=new FloatingReminder(States,Action,ToggleOne,Toggle,()=>Reveal(false),()=>Close(),theme);
 tray=new NotifyIcon{Icon=theme.Icon,Text="歇歇 · 尚未开始",Visible=true};tray.DoubleClick+=(s,e)=>Reveal(false);var menu=new ContextMenuStrip();menu.Items.Add("打开歇歇",null,(s,e)=>Reveal(false));menu.Items.Add("缩小为悬浮球",null,(s,e)=>FloatMode());menu.Items.Add("全部开启 / 暂停",null,(s,e)=>Toggle());menu.Items.Add("结束本次工作",null,(s,e)=>EndSession());menu.Items.Add("退出",null,(s,e)=>Close());tray.ContextMenuStrip=menu;
 Resize+=(s,e)=>{if(WindowState==FormWindowState.Minimized){Hide();WindowState=FormWindowState.Normal;}};
 FormClosing+=(s,e)=>{if(closing)return;closing=true;store.End(DateTime.UtcNow);};FormClosed+=(s,e)=>{timer.Stop();timer.Dispose();tray.Dispose();Floating.Close();Floating.Dispose();if(statistics!=null)statistics.Close();};
 timer=new System.Windows.Forms.Timer{Interval=250};timer.Tick+=(s,e)=>Tick();initializing=false;SetTheme(theme,false);Render();timer.Start();Shown+=(s,e)=>{var area=Screen.FromControl(this).WorkingArea;if(Height>area.Height-40)Height=Math.Max(500,area.Height-40);Activate();ArrangeCards();if(syncDesktop)Updates.Automatic(this,store.DirectoryPath);};
 }
 Button MakeButton(string text,int width){var b=new Button{Text=text,Width=width,MinimumSize=new Size(width,36),AutoSize=true,Padding=new Padding(8,4,8,4),FlatStyle=FlatStyle.Flat,BackColor=theme.Glyph,ForeColor=theme.Ink,Margin=new Padding(0,3,8,3)};b.FlatAppearance.BorderSize=0;return b;}
 void SettingsChanged(int index){if(initializing)return;Tick();var c=States[index];c.Interval=(int)intervals[index].Value*60;c.Duration=(int)durations[index].Value*(index==0?1:60);c.Reset();store.EyeMinutes=(int)intervals[0].Value;store.EyeSeconds=(int)durations[0].Value;store.MoveMinutes=(int)intervals[1].Value;store.MoveSeconds=(int)durations[1].Value*60;store.CustomMinutes=(int)intervals[2].Value;store.CustomSeconds=(int)durations[2].Value*60;store.Changed();store.Save();statusNote="已更新此项间隔，其他提醒不受影响。";Render();}
 void ToggleOne(int index){Tick();States[index].Enabled=!States[index].Enabled;running=Array.Exists(States,c=>c.Enabled);if(States[index].Enabled)store.Start(DateTime.UtcNow);TopMost=false;statusNote="";store.Save();Render();}
 void Toggle(){Tick();bool enable=!Array.Exists(States,c=>c.Enabled);foreach(var c in States)c.Enabled=enable;running=enable;if(enable)store.Start(DateTime.UtcNow);TopMost=false;statusNote="";store.Save();Render();}
 void Action(int index){Tick();var c=States[index];c.Enabled=true;running=true;store.Start(DateTime.UtcNow);if(c.Phase==3)c.Reset();else if(c.Phase!=2)c.Rest();TopMost=false;statusNote="";store.Save();Render();}
 void EndSession(){store.End(DateTime.UtcNow);running=false;foreach(var c in States){c.Enabled=false;c.Reset();}last=elapsed.Elapsed.TotalSeconds;statusNote="本次工作已结束，时长已保存。";Floating.Expand(false);Render();}
 void FloatMode(){floatingMode=true;TopMost=false;Hide();Floating.UpdateState(running);Floating.Present(Array.Exists(States,c=>c.Enabled&&c.Phase!=0));}
 internal void Reveal(bool top){floatingMode=false;Floating.Hide();Show();WindowState=FormWindowState.Normal;TopMost=top;BringToFront();if(!top)Activate();}
 void Tick(){store.Advance(DateTime.UtcNow);if(ExitEvent!=null&&ExitEvent.WaitOne(0)){Close();return;}if(WakeEvent!=null&&WakeEvent.WaitOne(0))Reveal(false);double now=elapsed.Elapsed.TotalSeconds,delta=now-last;last=now;
 if(running){if(delta>15){foreach(var c in States)c.Enabled=false;running=false;TopMost=false;statusNote="长时间中断，提醒已暂停；工作时长按你的设置继续累计。";}else{bool notify=false;foreach(var state in States)notify=state.Tick(delta)||notify;if(notify){if(floatingMode)Floating.Present(true);else Reveal(true);if(sound.Checked)System.Media.SystemSounds.Asterisk.Play();}}}
 if(now-lastSave>=10){lastSave=now;store.Save();}if(statistics!=null&&now-lastStats>=10){lastStats=now;statistics.RefreshData();}Render();}
 void Render(){Floating.UpdateState(running);pause.Text=running?"全部暂停":"全部开启";end.Enabled=store.Active;
 global.Text=store.Error.Length>0?store.Error:statusNote.Length>0?statusNote:running?"提醒已开启 · 到点后确认开始休息":store.Active?"提醒已暂停 · 本次工作时长仍在累计":"准备好后点击开始，记录今天的工作时光。";
 today.Text=SessionStore.Duration(store.Total(DateTime.Today));sessionLabel.Text=store.Active?"本次 "+SessionStore.Duration(store.SessionSeconds)+" · 含暂停、休息与等待":"包含暂停、起身休息与等待确认时间";tray.Text=running?"歇歇 · 提醒中":store.Active?"歇歇 · 提醒暂停，工作时长累计中":"歇歇 · 尚未开始";
 for(int i=0;i<3;i++){var c=States[i];int left=(int)Math.Ceiling(c.Left);counts[i].Text=c.Phase==1?"该休息啦":c.Phase==3?"休息结束":(left/60).ToString("00")+":"+(left%60).ToString("00");actions[i].Text=c.Phase==3?"返回工作":c.Phase==2?"休息中…":c.Phase==1?"开始休息":"现在休息";actions[i].Enabled=c.Phase!=2;intervals[i].Enabled=durations[i].Enabled=!c.Enabled&&c.Phase==0;switches[i].Text=c.Enabled?"暂停此项":"开启 / 继续";statuses[i].Text=!c.Enabled?"此项已关闭 / 暂停，倒计时保留；其他提醒照常运行。":c.Phase==3?"点击返回工作，开始下一轮提醒。":i==0?"看约 6 米外，轻柔、完整地眨眼，不换成看手机。":i==1?"舒适地站起慢走；疼痛加重就停止，遵循康复医嘱。":"按你设置的事项放松，完成后返回工作。";}}
 void ChooseTheme(){using(var picker=new ImageThemePicker(theme,store.DirectoryPath)){if(picker.ShowDialog(this)==DialogResult.OK){SetTheme(picker.Selected,true);try{CustomThemes.Prune(store.DirectoryPath,picker.Selected.Id);}catch(IOException){statusNote="图片已切换，旧记录将稍后清理。";}}}}
 void SetTheme(PonyTheme value,bool save){theme=value;BackColor=value.Page;ForeColor=value.Ink;Icon=value.Icon;avatar.Image=value.Picture;themeName.Text=value.Name+"陪你，慢慢来";Floating.SetTheme(value);tray.Icon=value.Icon;foreach(var b in actions){b.BackColor=value.Glyph;b.ForeColor=value.Ink;}pause.BackColor=value.Accent;pause.ForeColor=Color.White;if(save){store.Theme=value.Id;store.Changed();store.Save();}if(syncDesktop)statusNote=Themes.UpdateDesktopIcon(value,store.DirectoryPath);Render();}
 void ShowStatistics(){store.Advance(DateTime.UtcNow);store.Save();if(statistics!=null){statistics.Activate();statistics.RefreshData();return;}statistics=new StatisticsForm(store,theme);statistics.FormClosed+=(s,e)=>statistics=null;statistics.Show(this);}
 void ShowLicense(){using(var stream=Themes.Resource("FONT-LICENSE.txt"))using(var reader=new StreamReader(stream)){var form=new Form{Text="开源字体许可 · jf open 粉圆",Size=new Size(670,500),StartPosition=FormStartPosition.CenterParent,Icon=theme.Icon};form.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=reader.ReadToEnd()});form.Show(this);}}
 void ShowInfo(){MessageBox.Show("开始提醒后，今日工作时长持续累计，包括暂停、起身休息、等待确认和未满一轮的时间。结束本次或关闭软件才停止；软件关闭期间不计入。软件仍开着时，锁屏和电脑睡眠时间也属于本次时段。\n\n到点提醒后，点击开始休息才计算休息时间；结束后点击返回工作。三项提醒可分别开启和暂停，暂停保留倒计时；全部开启 / 暂停为快捷操作。03 可修改名称、间隔和休息时长。暂停后可以修改间隔，修改会重置提醒倒计时，不清除统计。\n\n悬浮球可拖动，点击展开。横条支持暂停、开始休息、返回工作、收起和打开主窗口。\n\n远眺默认每 20 分钟至少 20 秒，是常见护眼建议，不是已证实最优频次。起身默认每 30 分钟、活动 3 分钟，是一般久坐休息起点，不是严重脊柱侧弯的治疗处方。具体矫正动作请由脊柱专科或康复治疗师制定。持续眼干或腰痛应就医；新发无力、会阴麻木、大小便控制异常或突发视力下降需及时就医。\n\n记录保存在本机："+store.DirectoryPath+"\n请勿删除该文件夹，以保留历史统计。资料与链接见使用说明。","使用说明",MessageBoxButtons.OK,MessageBoxIcon.Information);}
 [STAThread] static void Main(string[] args){
 Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Themes.Load();
 if(args.Length==2&&args[0]=="--self-test"){try{SelfTests.Run(args[1]);}catch(Exception ex){Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"FAILED.txt"),ex.ToString());Environment.Exit(1);}return;}
 WakeEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\SimpleEyeAndMovementReminderWake");bool created;using(var mutex=new Mutex(true,"Local\\SimpleEyeAndMovementReminder",out created)){if(!created){WakeEvent.Set();WakeEvent.Dispose();return;}ExitEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\XieXieExit");string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XieXie");Application.Run(new Reminder(data,true));ExitEvent.Dispose();WakeEvent.Dispose();}
 }
}
