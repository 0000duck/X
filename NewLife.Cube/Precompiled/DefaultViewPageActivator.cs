using System;
using System.Web.Mvc;

namespace NewLife.Cube.Precompiled
{
    /// <summary>��ͼҳע����</summary>
    internal class DefaultViewPageActivator : IViewPageActivator
    {
        private static class Nested
        {
            internal static readonly DefaultViewPageActivator Instance;
            static Nested()
            {
                Instance = new DefaultViewPageActivator();
            }
        }

        private readonly Func<IDependencyResolver> _resolverThunk;

        /// <summary>��ǰע����</summary>
        public static DefaultViewPageActivator Current { get { return Nested.Instance; } }

        public DefaultViewPageActivator() : this(null) { }

        public DefaultViewPageActivator(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                _resolverThunk = (() => DependencyResolver.Current);
            }
            else
            {
                _resolverThunk = (() => resolver);
            }
        }

        /// <summary>������ͼʵ��</summary>
        /// <param name="controllerContext"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Create(ControllerContext controllerContext, Type type)
        {
            return _resolverThunk().GetService(type) ?? Activator.CreateInstance(type);
        }
    }
}