using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

sealed class SessionStore {
 public readonly string DirectoryPath;
 public string Theme="fluttershy",CustomName="自定义";public int CustomMinutes=45,CustomSeconds=180;
 public int EyeMinutes=20, EyeSeconds=20, MoveMinutes=30, MoveSeconds=180;
 public bool Sound=true;
 public string Error="";
 public bool Active { get; private set; }
 public double SessionSeconds { get; private set; }
 public readonly SortedDictionary<string,double> Days=new SortedDictionary<string,double>();
 DateTime lastUtc;
 bool dirty,readOnly;
 string FilePath { get { return Path.Combine(DirectoryPath,"history.xml"); } }
 public SessionStore(string directory){DirectoryPath=directory;Load();}
 public void Start(DateTime utc){if(Active)return;Active=true;SessionSeconds=0;lastUtc=utc;}
 public void Advance(DateTime utc){if(!Active)return;if(utc>lastUtc){SessionSeconds+=(utc-lastUtc).TotalSeconds;AddRange(lastUtc,utc);}lastUtc=utc;}
 public void End(DateTime utc){Advance(utc);Active=false;Save();}
 public void AddRange(DateTime fromUtc,DateTime toUtc){
 while(fromUtc<toUtc){DateTime local=fromUtc.ToLocalTime();DateTime boundary=DateTime.SpecifyKind(local.Date.AddDays(1),DateTimeKind.Local).ToUniversalTime();DateTime end=boundary<toUtc?boundary:toUtc;if(end<=fromUtc)end=toUtc;
 string key=local.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);double current;Days.TryGetValue(key,out current);Days[key]=current+(end-fromUtc).TotalSeconds;dirty=true;fromUtc=end;}
 }
 public double Total(DateTime date){double s;return Days.TryGetValue(date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),out s)?s:0;}
 public double Total(DateTime from,DateTime until){double s=0;for(DateTime d=from.Date;d<until.Date;d=d.AddDays(1))s+=Total(d);return s;}
 public void Changed(){dirty=true;}
 public void Save(){if(!dirty||readOnly)return;try{
 Directory.CreateDirectory(DirectoryPath);
 var root=new XElement("xiexie",new XAttribute("version",2),new XElement("settings",new XAttribute("theme",Theme),new XAttribute("eyeMinutes",EyeMinutes),new XAttribute("eyeSeconds",EyeSeconds),new XAttribute("moveMinutes",MoveMinutes),new XAttribute("moveSeconds",MoveSeconds),new XAttribute("customName",CustomName),new XAttribute("customMinutes",CustomMinutes),new XAttribute("customSeconds",CustomSeconds),new XAttribute("sound",Sound)));
 var daily=new XElement("days");foreach(var pair in Days)daily.Add(new XElement("day",new XAttribute("date",pair.Key),new XAttribute("seconds",pair.Value.ToString("R",CultureInfo.InvariantCulture))));root.Add(daily);
 string temporary=FilePath+".tmp";new XDocument(root).Save(temporary);if(File.Exists(FilePath))File.Replace(temporary,FilePath,FilePath+".bak");else File.Move(temporary,FilePath);dirty=false;Error="";
 }catch(Exception ex){Error="记录暂未保存："+ex.Message;}}
 void Read(string path){
 var root=XDocument.Load(path).Root;if(root==null || root.Name!="xiexie")throw new InvalidDataException("记录格式不正确");
 var s=root.Element("settings");if(s!=null){Theme=(string)s.Attribute("theme")??"fluttershy";EyeMinutes=Clamp((int?)s.Attribute("eyeMinutes")??20,1,120);EyeSeconds=Clamp((int?)s.Attribute("eyeSeconds")??20,20,300);MoveMinutes=Clamp((int?)s.Attribute("moveMinutes")??30,1,120);MoveSeconds=Clamp((int?)s.Attribute("moveSeconds")??180,60,900);Sound=(bool?)s.Attribute("sound")??true;CustomName=(string)s.Attribute("customName")??"自定义";CustomMinutes=Clamp((int?)s.Attribute("customMinutes")??45,1,120);CustomSeconds=Clamp((int?)s.Attribute("customSeconds")??180,60,900);}
 var loaded=new SortedDictionary<string,double>();var days=root.Element("days");if(days!=null)foreach(var item in days.Elements("day")){DateTime date;double seconds;string key=(string)item.Attribute("date");if(!DateTime.TryParseExact(key,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date) || !double.TryParse((string)item.Attribute("seconds"),NumberStyles.Float,CultureInfo.InvariantCulture,out seconds)||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new InvalidDataException("记录内容不正确");loaded[key]=seconds;}Days.Clear();foreach(var item in loaded)Days[item.Key]=item.Value;
 }
 void Load(){if(!File.Exists(FilePath))return;try{Read(FilePath);}catch{try{Read(FilePath+".bak");Error="主记录损坏，已从备份恢复。";}catch{readOnly=true;Error="历史记录未能读取，原文件已保留，暂不写入新记录。";}}}
 public void MigrateLegacy(string path){if(File.Exists(FilePath)||!File.Exists(path))return;try{var lines=File.ReadAllLines(path);if(lines.Length<4)return;EyeMinutes=Clamp(int.Parse(lines[0]),1,120);EyeSeconds=Clamp(int.Parse(lines[1]),20,300);MoveMinutes=Clamp(int.Parse(lines[2]),1,120);MoveSeconds=Clamp(int.Parse(lines[3])*60,60,900);Changed();Save();}catch{}}
 static int Clamp(int v,int min,int max){return Math.Max(min,Math.Min(max,v));}
 public static string Duration(double seconds){long n=(long)Math.Floor(seconds);return n>=3600?(n/3600)+" 小时 "+(n/60%60)+" 分钟":(n/60)+" 分钟 "+(n%60)+" 秒";}
 public static DateTime WeekStart(DateTime day){return day.Date.AddDays(-((7+(int)day.DayOfWeek-1)%7));}
}
