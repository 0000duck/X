using System;
using System.Collections.Generic;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace NewLife.Cube.Precompiled
{
    /// <summary>����Ԥ����Mvc����</summary>
    public class CompositePrecompiledMvcEngine : BuildManagerViewEngine, IVirtualPathFactory
    {
        private struct ViewMapping
        {
            public Type Type { get; set; }

            public PrecompiledViewAssembly ViewAssembly { get; set; }
        }
        private readonly IDictionary<String, ViewMapping> _mappings = new Dictionary<String, ViewMapping>(StringComparer.OrdinalIgnoreCase);
        //private readonly IViewPageActivator _viewPageActivator;

        /// <summary>����Ԥ����Mvc����</summary>
        /// <param name="viewAssemblies"></param>
        public CompositePrecompiledMvcEngine(params PrecompiledViewAssembly[] viewAssemblies) : this(viewAssemblies, null) { }

        /// <summary>����Ԥ����Mvc����</summary>
        /// <param name="viewAssemblies"></param>
        /// <param name="viewPageActivator"></param>
        public CompositePrecompiledMvcEngine(IEnumerable<PrecompiledViewAssembly> viewAssemblies, IViewPageActivator viewPageActivator)
            : base(viewPageActivator)
        {
            AreaViewLocationFormats = new String[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            AreaMasterLocationFormats = new String[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            AreaPartialViewLocationFormats = new String[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            ViewLocationFormats = new String[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            MasterLocationFormats = new String[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            PartialViewLocationFormats = new String[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            FileExtensions = new String[]
			{
				"cshtml"
			};
            foreach (var asm in viewAssemblies)
            {
                foreach (var type in asm.GetTypeMappings())
                {
                    _mappings[type.Key] = new ViewMapping
                    {
                        Type = type.Value,
                        ViewAssembly = asm
                    };
                }
            }
            //_viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>�ļ��Ƿ���ڡ�������ڣ����ɵ�ǰ���洴����ͼ</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override Boolean FileExists(ControllerContext controllerContext, String virtualPath)
        {
            ViewMapping viewMapping;
            if (!_mappings.TryGetValue(virtualPath, out viewMapping)) return false;

            // ���ӳ������ڣ��Ͳ�Ҫ������
            if (!Exists(virtualPath)) return false;

            var asm = viewMapping.ViewAssembly;
            // ������������һ�����㼴��ʹ�������ļ�
            // �����Ҫ��ȡ�������ļ������������ļ����ڣ���ʹ�������ļ�����
            if (!asm.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath)) return false;

            // ���ʹ�ý��µ������ļ����������ļ���ȷ���£���ʹ�������ļ�����
            if (asm.UsePhysicalViewsIfNewer && asm.IsPhysicalFileNewer(virtualPath)) return false;

            return true;
        }

        /// <summary>�����ֲ���ͼ</summary>
        /// <param name="controllerContext"></param>
        /// <param name="partialPath"></param>
        /// <returns></returns>
        protected override IView CreatePartialView(ControllerContext controllerContext, String partialPath)
        {
            return CreateViewInternal(partialPath, null, false);
        }

        /// <summary>������ͼ</summary>
        /// <param name="controllerContext"></param>
        /// <param name="viewPath"></param>
        /// <param name="masterPath"></param>
        /// <returns></returns>
        protected override IView CreateView(ControllerContext controllerContext, String viewPath, String masterPath)
        {
            return CreateViewInternal(viewPath, masterPath, true);
        }

        private IView CreateViewInternal(String viewPath, String masterPath, Boolean runViewStartPages)
        {
            ViewMapping viewMapping;
            if (!_mappings.TryGetValue(viewPath, out viewMapping)) return null;

            return new PrecompiledMvcView(viewPath, masterPath, viewMapping.Type, runViewStartPages, FileExtensions, ViewPageActivator);
        }

        /// <summary>����ʵ����Start��Layout���������</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Object CreateInstance(String virtualPath)
        {
            ViewMapping viewMapping;

            // ���û�и�ӳ�䣬��ֱ�ӷ��ؿ�
            if (!_mappings.TryGetValue(virtualPath, out viewMapping)) return null;

            var asm = viewMapping.ViewAssembly;
            // ������������һ�����㼴��ʹ�������ļ�
            // �����Ҫ��ȡ�������ļ������������ļ����ڣ���ʹ�������ļ�����
            if (!asm.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));

            // ���ʹ�ý��µ������ļ����������ļ���ȷ���£���ʹ�������ļ�����
            if (asm.UsePhysicalViewsIfNewer && asm.IsPhysicalFileNewer(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));

            // ���ʹ����Ƕ�ഴ��
            return ViewPageActivator.Create(null, viewMapping.Type);
        }

        /// <summary>�Ƿ����</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Boolean Exists(String virtualPath)
        {
            return _mappings.ContainsKey(virtualPath);
        }
    }
}