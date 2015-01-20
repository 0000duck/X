﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>菜单</summary>
    public partial class Menu<TEntity> : EntityTree<TEntity>, IMenu where TEntity : Menu<TEntity>, new()
    {
        #region 对象操作
        static Menu()
        {
            var entity = new TEntity();

            //EntityFactory.Register(typeof(TEntity), new MenuFactory<TEntity>());
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}菜单数据……", typeof(TEntity).Name);

            using (var trans = new EntityTransaction<TEntity>())
            {
                // 准备增加Admin目录下的所有页面
                //ScanAndAdd(top);
                ScanAndAdd();

                trans.Commit();
                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}菜单数据！", typeof(TEntity).Name);
            }
        }

        /// <summary>已重载。调用Save时写日志，而调用Insert和Update时不写日志</summary>
        /// <returns></returns>
        public override int Save()
        {
            if (!String.IsNullOrEmpty(Url))
            {
                // 删除两端空白
                if (Url != Url.Trim()) Url = Url.Trim();
            }

            if (ID == 0)
                WriteLog("添加", Name);
            else if (HasDirty)
                WriteLog("修改", Name);

            return base.Save();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {
            String name = Name;
            if (String.IsNullOrEmpty(name))
            {
                TEntity entity = FindByID(ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name);

            return base.Delete();
        }

        #endregion

        #region 扩展属性
        /// <summary>父菜单名</summary>
        [XmlIgnore]
        public virtual String ParentMenuName { get { return Parent == null ? null : Parent.Name; } set { } }

        /// <summary>当前页所对应的菜单项</summary>
        public static TEntity Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null || context.Request == null) return null;

                var key = "CurrentMenu";
                var entity = context.Items[key] as TEntity;
                if (entity == null)
                {
                    entity = GetCurrentMenu();
                    context.Items[key] = entity;
                }

                return entity;
            }
        }

        static TEntity GetCurrentMenu()
        {
            var context = HttpContext.Current;
            if (context == null || context.Request == null) return null;

            // 计算当前文件路径
            var p = context.Request.PhysicalPath;
            var di = new DirectoryInfo(Path.GetDirectoryName(p));
            var fileName = Path.GetFileName(p);

            // 查找所有以该文件名结尾的菜单
            var list = Meta.Cache.Entities;
            list = list.FindAll(item => !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWithIgnoreCase(fileName));
            if ((list == null || list.Count < 1) && Path.GetFileNameWithoutExtension(p).EndsWithIgnoreCase("Form"))
            {
                fileName = Path.GetFileNameWithoutExtension(p);
                fileName = fileName.Substring(0, fileName.Length - "Form".Length);
                fileName += Path.GetExtension(p);

                // 有可能是表单
                list = Meta.Cache.Entities.FindAll(item => !String.IsNullOrEmpty(item.Url) && item.Url.Trim().EndsWithIgnoreCase(fileName));
            }
            if (list == null || list.Count < 1) return null;
            if (list.Count == 1) return list[0];

            // 查找所有以该文件名结尾的菜单
            var list2 = list.FindAll(e => !String.IsNullOrEmpty(e.Url) && e.Url.Trim().EndsWithIgnoreCase(@"/" + fileName));
            if (list2 == null || list2.Count < 1) return list[0];
            if (list2.Count == 1) return list2[0];

            // 优先全路径
            var url = String.Format(@"../../{0}/{1}/{2}", di.Parent.Name, di.Name, fileName);
            var entity = FindByUrl(url);
            if (entity != null) return entity;

            // 兼容旧版本
            url = String.Format(@"../{0}/{1}", di.Name, fileName);
            return FindByUrl(url);
        }

        /// <summary>必要的菜单。必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        internal static Int32[] Necessaries
        {
            get
            {
                // 找出所有的必要菜单，如果没有，则表示全部都是必要
                var list = FindAllWithCache(__.Necessary, true);
                if (list.Count <= 0) list = Meta.Cache.Entities;

                return list.GetItem<Int32>(__.ID).ToArray();
            }
        }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(__.ID, id);
        }

        /// <summary>根据名字查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name) { return Meta.Cache.Entities.Find(__.Name, name); }

        /// <summary>根据Url查找</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static TEntity FindByUrl(String url) { return Meta.Cache.Entities.FindIgnoreCase(__.Url, url); }

        /// <summary>根据名字查找，支持路径查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindForName(String name)
        {
            TEntity entity = FindByName(name);
            if (entity != null) return entity;

            //return FindByPath(Meta.Cache.Entities, name, _.Name);
            return Root.FindByPath(name, _.Name, _.Permission, _.Remark);
        }

        /// <summary>根据权限名查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByPerssion(String name) { return Meta.Cache.Entities.Find(__.Permission, name); }

        /// <summary>为了权限而查找，支持路径查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindForPerssion(String name)
        {
            // 计算集合，为了处理同名的菜单
            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(__.Permission, name);
            if (list != null && list.Count == 1) return list[0];

            // 如果菜单同名，则使用当前页
            TEntity current = null;
            // 看来以后要把list != null && list.Count > 0判断作为常态，养成好习惯呀
            if (list != null && list.Count > 0)
            {
                if (current == null) current = Current;
                if (current != null)
                {
                    foreach (TEntity item in list)
                    {
                        if (current.ID == item.ID) return item;
                    }
                }

                if (XTrace.Debug) XTrace.WriteLine("存在多个名为" + name + "的菜单，系统无法区分，请修改为不同的权限名，以免发生授权干扰！");

                return list[0];
            }

            return Root.FindByPath(name, _.Permission);
        }

        ///// <summary>
        ///// 路径查找
        ///// </summary>
        ///// <param name="list"></param>
        ///// <param name="path"></param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //public static TEntity FindByPath(EntityList<TEntity> list, String path, String name)
        //{
        //    if (list == null || list.Count < 1) return null;
        //    if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name)) return null;

        //    // 尝试一次性查找
        //    TEntity entity = list.Find(name, path);
        //    if (entity != null) return entity;

        //    String[] ss = path.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (ss == null || ss.Length < 1) return null;

        //    // 找第一级
        //    entity = list.Find(name, ss[0]);
        //    if (entity == null) entity = list.Find(__.Remark, ss[0]);
        //    if (entity == null) return null;

        //    // 是否还有下级
        //    if (ss.Length == 1) return entity;

        //    // 递归找下级
        //    return FindByPath(entity.Childs, String.Join("\\", ss, 1, ss.Length - 1), name);

        //    //EntityList<TEntity> list3 = new EntityList<TEntity>();
        //    //for (int i = 0; i < ss.Length; i++)
        //    //{
        //    //    // 找到符合当前级别的所有节点
        //    //    EntityList<TEntity> list2 = list.FindAll(name, ss[i]);
        //    //    if (list2 == null || list2.Count < 1) return null;

        //    //    // 是否到了最后
        //    //    if (i == ss.Length - 1)
        //    //    {
        //    //        list3 = list2;
        //    //        break;
        //    //    }

        //    //    // 找到它们的子节点
        //    //    list3.Clear();
        //    //    foreach (TEntity item in list2)
        //    //    {
        //    //        if (item.Childs != null && item.Childs.Count > 0) list3.AddRange(item.Childs);
        //    //    }
        //    //    if (list3 == null || list3.Count < 1) return null;
        //    //}
        //    //if (list3 != null && list3.Count > 0)
        //    //    return list[0];
        //    //else
        //    //    return null;
        //}

        /// <summary>查找指定菜单的子菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllByParentID(Int32 id)
        {
            EntityList<TEntity> list = Meta.Cache.Entities.FindAll(__.ParentID, id);
            if (list != null && list.Count > 0) list.Sort(new String[] { _.Sort, _.ID }, new Boolean[] { true, false });
            return list;
        }

        /// <summary>取得当前角色的子菜单，有权限、可显示、排序</summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public IList<IMenu> GetMySubMenus(Int32[] filters)
        {
            var list = Childs;
            if (list == null || list.Count < 1) return null;

            //list = list.FindAll(Menu<TEntity>._.ParentID, parentID);
            //if (list == null || list.Count < 1) return null;
            list = list.FindAll(Menu<TEntity>._.IsShow, true);
            if (list == null || list.Count < 1) return null;

            return list.ToList().Where(e => filters.Contains(e.ID)).Cast<IMenu>().ToList();
        }
        #endregion

        #region 扩展操作
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var path = FullPath;
            if (!String.IsNullOrEmpty(path))
                return path;
            else
                return base.ToString();
        }
        #endregion

        #region 业务
        /// <summary>导入</summary>
        public virtual void Import()
        {
            using (var trans = new EntityTransaction<TEntity>())
            {
                //顶级节点根据名字合并
                if (ParentID == 0)
                {
                    var m = Find(__.Name, Name);
                    if (m != null)
                    {
                        this.ID = m.ID;
                        this.Name = m.Name;
                        this.ParentID = 0;
                        this.Url = m.Url;
                        this.Remark = m.Remark;

                        this.Update();
                    }
                    else
                        this.Insert();
                }
                else
                {
                    this.Insert();
                }

                //更新编号
                var list = Childs;
                if (list != null && list.Count > 0)
                {
                    foreach (TEntity item in list)
                    {
                        item.ParentID = ID;

                        item.Import();
                    }
                }

                trans.Commit();
            }
        }

        /// <summary>添加子菜单</summary>
        /// <param name="name">名称</param>
        /// <param name="url"></param>
        /// <param name="sort"></param>
        /// <param name="reamark"></param>
        /// <returns></returns>
        public virtual TEntity Create(String name, String url, Int32 sort = 0, String reamark = null)
        {
            var entity = new TEntity();
            entity.ParentID = ID;
            entity.Name = name;
            entity.Permission = name;
            entity.Url = url;
            entity.Sort = sort;
            entity.IsShow = true;
            entity.Remark = reamark ?? name;
            //entity.Save();

            return entity;
        }

        /// <summary>扫描配置文件中指定的目录</summary>
        /// <returns></returns>
        public static Int32 ScanAndAdd()
        {
            // 扫描目录
            var appDirs = new List<String>(Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirs", null));
            // 过滤文件
            var appDirsFileFilter = Config.GetConfigSplit<String>("NewLife.CommonEntity.AppDirsFileFilter", null);
            // 是否在子目中过滤
            var appDirsIsAllFilter = Config.GetConfig<Boolean>("NewLife.CommonEntity.AppDirsIsAllDirs", false);

            var filters = new HashSet<String>(appDirsFileFilter, StringComparer.OrdinalIgnoreCase);

            // 如果不包含Admin，以它开头
            if (!appDirs.Contains("Admin")) appDirs.Insert(0, "Admin");

            Int32 total = 0;
            foreach (var item in appDirs)
            {
                // 如果目录不存在，就没必要扫描了
                var p = item.GetFullPath();
                if (!Directory.Exists(p))
                {
                    // 有些旧版本系统，会把目录放到Admin目录之下
                    p = "Admin".CombinePath(item);
                    if (!Directory.Exists(p)) continue;
                }

                XTrace.WriteLine("扫描目录生成菜单 {0}", p);

                // 根据目录找菜单，它将作为顶级菜单
                var top = FindForName(item);
                if (top == null) top = Meta.Cache.Entities.Find(__.Remark, item);
                if (top == null)
                {
                    top = Root.Create(item, null, 0, item);
                    top.Save();
                }
                total += ScanAndAdd(item, top, filters, appDirsIsAllFilter);
            }

            return total;
        }

        //static TEntity GetTopForDir(String dir)
        //{
        //    // 根据目录找菜单，它将作为顶级菜单
        //    var top = FindForName(dir);
        //    if (top == null) top = Meta.Cache.Entities.Find(__.Remark, dir);

        //    // 如果找不到，就取第一个作为顶级
        //    if (top == null)
        //    {
        //        var childs = Root.Childs;
        //        if (childs != null && childs.Count > 0)
        //            top = childs[0];
        //        else
        //        {
        //            //var list = FindAllByName(__.ParentID, 0, _.ID.Desc(), 0, 1);
        //            //if (list != null && list.Count > 1) top = list[0];
        //            return Meta.Cache.Entities.ToList().OrderByDescending(e => e.Sort).FirstOrDefault(e => e.ParentID == 0);
        //        }
        //    }
        //    return top;
        //}

        ///// <summary>扫描指定目录并添加文件到第一个顶级菜单之下</summary>
        ///// <param name="dir"></param>
        ///// <returns></returns>
        //public static Int32 ScanAndAdd(String dir) { return ScanAndAdd(dir, GetTopForDir(dir)); }

        /// <summary>扫描指定目录并添加文件到顶级菜单之下</summary>
        /// <param name="dir">扫描目录</param>
        /// <param name="parent">父级</param>
        /// <param name="fileFilter">过滤文件名</param>
        /// <param name="isFilterChildDir">是否在子目录中过滤</param>
        /// <returns></returns>
        static Int32 ScanAndAdd(String dir, TEntity parent, ICollection<String> fileFilter = null, Boolean isFilterChildDir = false)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");
            //if (top == null) throw new ArgumentNullException("top");

            // 要扫描的目录
            var p = dir.GetFullPath();
            if (!Directory.Exists(p)) return 0;

            //本意，获取目录名
            var dirName = new DirectoryInfo(p).Name;

            if (dirName.EqualIgnoreCase("Frame", "Asc", "images")) return 0;
            if (dirName.StartsWithIgnoreCase("img")) return 0;

            //本目录aspx页面
            var fs = Directory.GetFiles(p, "*.aspx", SearchOption.TopDirectoryOnly);
            //本目录子子录
            var dis = Directory.GetDirectories(p);
            //如没有页面和子目录
            if ((fs == null || fs.Length < 1) && (dis == null || dis.Length < 1)) return 0;

            // 添加
            var num = 0;

            ////本目录菜单
            //var parent = FindByName(dirName);
            //if (parent == null) parent = Meta.Cache.Entities.Find(__.Remark, dirName);
            ////目录是否做为新菜单
            //var isAddDir = false;
            //if (parent == null)
            //{
            //    parent = top.Create(dirName, null, 0, dirName);
            //    parent.Save();
            //    num++;
            //    //目录为新增菜单
            //    isAddDir = true;
            //}

            XTrace.WriteLine("分析菜单下的页面 {0} 共有文件{1}个 子目录{2}个", parent.Name, fs.Length, dis.Length);

            //aspx
            if (fs != null && fs.Length > 0)
            {
                var currentPath = GetPathForScan(p, !dir.Contains("/") && !dir.Contains("\\"));
                foreach (var elm in fs)
                {
                    var file = Path.GetFileName(elm);
                    if (file.EqualIgnoreCase("Default.aspx"))
                    {
                        parent.Url = currentPath.CombinePath("Default.aspx");
                        String title = GetPageTitle(elm);
                        if (!String.IsNullOrEmpty(title)) parent.Name = parent.Permission = title;
                        parent.Save();
                    }

                    // 过滤特定文件名文件
                    // 采用哈希集合查询字符串更快
                    if (fileFilter != null && fileFilter.Contains(file)) continue;

                    // 过滤掉表单页面
                    if (Path.GetFileNameWithoutExtension(elm).EndsWithIgnoreCase("Form")) continue;
                    // 过滤掉选择页面
                    if (Path.GetFileNameWithoutExtension(elm).StartsWithIgnoreCase("Select")) continue;
                    //if (elm.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase)) continue;

                    // 全部使用全路径
                    var url = currentPath.CombinePath(Path.GetFileName(elm));
                    var entity = FindByUrl(url);
                    if (entity != null) continue;

                    entity = parent.Create(Path.GetFileNameWithoutExtension(elm), url);
                    String elmTitle = GetPageTitle(elm);
                    if (!String.IsNullOrEmpty(elmTitle)) entity.Name = entity.Permission = elmTitle;
                    entity.Save();

                    num++;
                }
            }

            // 子级目录
            if (dis == null || dis.Length > 0)
            {
                foreach (String item in dis)
                {
                    //num += isFilterChildDir ? ScanAndAdd(item, parent, fileFilter, true) : ScanAndAdd(item, parent, null, false);
                    var dirname = Path.GetFileName(item);
                    var menu = parent.Create(dirname, null, 0, dirname);
                    menu.Save();
                    num++;

                    var count = 0;
                    if (isFilterChildDir)
                        count = ScanAndAdd(item, menu, fileFilter, true);
                    else
                        count = ScanAndAdd(item, menu, null, false);

                    if (count == 0)
                    {
                        menu.Delete();
                        num--;
                    }
                    else
                        num += count;
                }
            }

            ////如果目录中没有菜单，移除目录
            ////目录为新增加菜单且本级以下num为1则认为只增加了目录，并无子级
            //if (num == 1)
            //{
            //    if (parent.Parent != null) parent.Parent.Childs.Remove(parent);
            //    parent.Delete();
            //    num--;
            //}

            return num;
        }

        /// <summary>获取目录层级</summary>
        /// <param name="dir"></param>
        /// <param name="isTop">是否顶级目录</param>
        /// <returns></returns>
        static String GetPathForScan(String dir, Boolean isTop)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("dir");

            // 要扫描的目录
            var p = dir.GetFullPath();
            if (!Directory.Exists(dir)) return "";

            //获取层级
            var currentPath = isTop ? "../" : "../../";
            currentPath = currentPath.CombinePath(p.Replace(AppDomain.CurrentDomain.BaseDirectory, null)).Replace("\\", "/").EnsureEnd("/");

            return currentPath;
        }

        static Regex reg_PageTitle = new Regex("\\bTitle=\"([^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex reg_PageTitle2 = new Regex("<title>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static String GetPageTitle(String pagefile)
        {
            if (String.IsNullOrEmpty(pagefile) || !".aspx".EqualIgnoreCase(Path.GetExtension(pagefile)) || !File.Exists(pagefile)) return null;

            // 读取aspx的第一行，里面有Title=""
            String line = null;
            using (var reader = new StreamReader(pagefile))
            {
                while (!reader.EndOfStream && line.IsNullOrWhiteSpace()) line = reader.ReadLine();
                // 有时候Title跑到第二第三行去了
                if (!reader.EndOfStream) line += Environment.NewLine + reader.ReadLine();
                if (!reader.EndOfStream) line += Environment.NewLine + reader.ReadLine();
            }
            if (!String.IsNullOrEmpty(line))
            {
                // 正则
                Match m = reg_PageTitle.Match(line);
                if (m != null && m.Success) return m.Groups[1].Value;
            }

            // 第二正则
            String content = File.ReadAllText(pagefile);
            Match m2 = reg_PageTitle2.Match(content);
            if (m2 != null && m2.Success) return m2.Groups[1].Value;

            return null;
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            //var admin = ManageProvider.Provider.Current as IAdministrator;
            //if (admin != null) admin.WriteLog(typeof(TEntity), action, remark);
            ManageProvider.Provider.WriteLog(typeof(TEntity), action, remark);
        }
        #endregion

        #region 导入导出
        /// <summary>导出</summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static String Export(IList<IMenu> list)
        {
            return Export(new EntityList<TEntity>(list));
        }

        /// <summary>导出</summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static String Export(EntityList<TEntity> list)
        {
            return list.ToXml();
        }

        /// <summary>导入</summary>
        /// <param name="xml"></param>
        public static void Import(String xml)
        {
            var list = new EntityList<TEntity>();
            list.FromXml(xml);
            foreach (var item in list)
            {
                item.Import();
            }
        }
        #endregion

        #region IMenu 成员
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        string IMenu.GetFullPath(bool includeSelf, string separator, Func<IMenu, string> func)
        {
            Func<TEntity, String> d = null;
            if (func != null) d = item => func(item);

            return GetFullPath(includeSelf, separator, d);
        }

        /// <summary>检查菜单名称，修改为新名称。返回自身，支持链式写法</summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        IMenu IMenu.CheckMenuName(String oldName, String newName)
        {
            //IMenu menu = FindByPath(AllChilds, oldName, _.Name);
            IMenu menu = FindByPath(oldName, _.Name, _.Permission, _.Remark);
            if (menu != null && menu.Name != newName)
            {
                menu.Name = menu.Permission = newName;
                menu.Save();
            }

            return this;
        }

        /// <summary>当前菜单</summary>
        IMenu IMenu.Current { get { return Current; } }

        /// <summary>父菜单</summary>
        IMenu IMenu.Parent { get { return Parent; } }

        /// <summary>子菜单</summary>
        IList<IMenu> IMenu.Childs { get { return Childs.OfType<IMenu>().ToList(); } }

        /// <summary>子孙菜单</summary>
        IList<IMenu> IMenu.AllChilds { get { return AllChilds.OfType<IMenu>().ToList(); } }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu IMenu.FindByPath(String path) { return FindByPath(path, _.Name, _.Permission, _.Remark); }

        /// <summary>根据权限查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IMenu IMenu.FindForPerssion(String name) { return FindForPerssion(name); }
        #endregion
    }

    public partial interface IMenu
    {
        /// <summary>取得全路径的实体，由上向下排序</summary>
        /// <param name="includeSelf">是否包含自己</param>
        /// <param name="separator">分隔符</param>
        /// <param name="func">回调</param>
        /// <returns></returns>
        String GetFullPath(Boolean includeSelf, String separator, Func<IMenu, String> func);

        /// <summary>检查菜单名称，修改为新名称。返回自身，支持链式写法</summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        IMenu CheckMenuName(String oldName, String newName);

        /// <summary>当前菜单</summary>
        IMenu Current { get; }

        /// <summary>父菜单</summary>
        new IMenu Parent { get; }

        /// <summary>子菜单</summary>
        new IList<IMenu> Childs { get; }

        /// <summary>子孙菜单</summary>
        new IList<IMenu> AllChilds { get; }

        ///// <summary>深度</summary>
        //Int32 Deepth { get; }

        /// <summary>根据层次路径查找</summary>
        /// <param name="path">层次路径</param>
        /// <returns></returns>
        IMenu FindByPath(String path);

        /// <summary>根据权限查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IMenu FindForPerssion(String name);

        /// <summary>排序上升</summary>
        void Up();

        /// <summary>排序下降</summary>
        void Down();

        ///// <summary>保存</summary>
        ///// <returns></returns>
        //Int32 Save();

        /// <summary></summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        IList<IMenu> GetMySubMenus(Int32[] filters);
    }

    //public interface IMenuFactory : IEntityOperate
    //{
    //    IMenu Root { get; }

    //    ///// <summary>必要的菜单。必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
    //    //Int32[] Necessary { get; }
    //}

    ///// <summary>菜单实体工厂</summary>
    ///// <typeparam name="TEntity"></typeparam>
    //public class MenuFactory<TEntity> : Menu<TEntity>.EntityOperate where TEntity : Menu<TEntity>, new()
    //{
    //    public IMenu Root { get { return Menu<TEntity>.Root; } }
    //}
}