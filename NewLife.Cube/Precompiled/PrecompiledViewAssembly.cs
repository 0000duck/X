using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewLife.Cube.Precompiled
{
    /// <summary>Ԥ������ͼ����</summary>
    public class PrecompiledViewAssembly
    {
        private readonly String _baseVirtualPath;
        private readonly Assembly _assembly;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;

        /// <summary>����ʹ�������ļ�</summary>
        public Boolean PreemptPhysicalFiles { get; set; }

        /// <summary>ʹ�ø��µ������ļ�</summary>
        public Boolean UsePhysicalViewsIfNewer { get; set; }

        /// <summary>ʵ����Ԥ������ͼ����</summary>
        /// <param name="assembly"></param>
        public PrecompiledViewAssembly(Assembly assembly) : this(assembly, null) { }

        /// <summary>ʵ����Ԥ������ͼ����</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        public PrecompiledViewAssembly(Assembly assembly, String baseVirtualPath)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            PreemptPhysicalFiles = true;
            UsePhysicalViewsIfNewer = true;

            _baseVirtualPath = PrecompiledMvcEngine.NormalizeBaseVirtualPath(baseVirtualPath);
            _assembly = assembly;
            _assemblyLastWriteTime = new Lazy<DateTime>(() => _assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
        }

        /// <summary>Ϊָ���������ڳ��򼯴���ʵ��</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseVirtualPath"></param>
        /// <param name="usePhysicalViewsIfNewer"></param>
        /// <param name="preemptPhysicalFiles"></param>
        /// <returns></returns>
        public static PrecompiledViewAssembly OfType<T>(String baseVirtualPath, Boolean usePhysicalViewsIfNewer = false, Boolean preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly, baseVirtualPath)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        /// <summary>Ϊָ���������ڳ��򼯴���ʵ��</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usePhysicalViewsIfNewer"></param>
        /// <param name="preemptPhysicalFiles"></param>
        /// <returns></returns>
        public static PrecompiledViewAssembly OfType<T>(Boolean usePhysicalViewsIfNewer = false, Boolean preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        /// <summary>������ȡ��������ӳ��</summary>
        /// <returns></returns>
        public IDictionary<String, Type> GetTypeMappings()
        {
            return PrecompiledMvcEngine.GetTypeMappings(_assembly, _baseVirtualPath);
        }

        /// <summary>�����ļ��Ƿ����</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Boolean IsPhysicalFileNewer(String virtualPath)
        {
            return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
        }

    }
}