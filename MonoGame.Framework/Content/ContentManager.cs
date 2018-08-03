// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;

#if !WINDOWS_UAP && !WebGL
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
#endif

namespace Microsoft.Xna.Framework.Content
{
	public partial class ContentManager : IDisposable
	{
        const byte ContentCompressedLzx = 0x80;
        const byte ContentCompressedLz4 = 0x40;

		private string _rootDirectory = string.Empty;
		private IServiceProvider serviceProvider;
		private IGraphicsDeviceService graphicsDeviceService;
#if WebGL
        private Dictionary<string, object> loadedAssets = new Dictionary<string, object>();
#else
        private Dictionary<string, object> loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
#endif
        private List<IDisposable> disposableAssets = new List<IDisposable>();
        private bool disposed;
        private byte[] scratchBuffer;

		private static object ContentManagerLock = new object();
        private static List<WeakReference> ContentManagers = new List<WeakReference>();

        private static readonly List<char> targetPlatformIdentifiers = new List<char>()
        {
            'w', // Windows (XNA & DirectX)
            'x', // Xbox360 (XNA)
            'm', // WindowsPhone7.0 (XNA)
            'i', // iOS
            'a', // Android
            'd', // DesktopGL
            'X', // MacOSX
            'W', // WindowsStoreApp
            'n', // NativeClient
            'M', // WindowsPhone8
            'r', // RaspberryPi
            'P', // PlayStation4
            'v', // PSVita
            'O', // XboxOne
            'S', // Nintendo Switch

            // NOTE: There are additional idenfiers for consoles that 
            // are not defined in this repository.  Be sure to ask the
            // console port maintainers to ensure no collisions occur.

            
            // Legacy identifiers... these could be reused in the
            // future if we feel enough time has passed.

            'p', // PlayStationMobile
            'g', // Windows (OpenGL)
            'l', // Linux
        };


        static partial void PlatformStaticInit();

        static ContentManager()
        {
            // Allow any per-platform static initialization to occur.
            PlatformStaticInit();
        }

        private static void AddContentManager(ContentManager contentManager)
        {
            lock (ContentManagerLock)
            {
                // Check if the list contains this content manager already. Also take
                // the opportunity to prune the list of any finalized content managers.
                bool contains = false;
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (ReferenceEquals(contentRef.Target, contentManager))
                        contains = true;
                    if (!contentRef.IsAlive)
                        ContentManagers.RemoveAt(i);
                }
                if (!contains)
                    ContentManagers.Add(new WeakReference(contentManager));
            }
        }

        private static void RemoveContentManager(ContentManager contentManager)
        {
            lock (ContentManagerLock)
            {
                // Check if the list contains this content manager and remove it. Also
                // take the opportunity to prune the list of any finalized content managers.
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (!contentRef.IsAlive || ReferenceEquals(contentRef.Target, contentManager))
                        ContentManagers.RemoveAt(i);
                }
            }
        }

        internal static void ReloadGraphicsContent()
        {
            lock (ContentManagerLock)
            {
                // Reload the graphic assets of each content manager. Also take the
                // opportunity to prune the list of any finalized content managers.
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (contentRef.IsAlive)
                    {
                        var contentManager = (ContentManager)contentRef.Target;
                        if (contentManager != null)
                            contentManager.ReloadGraphicsAssets();
                    }
                    else
                    {
                        ContentManagers.RemoveAt(i);
                    }
                }
            }
        }

		// Use C# destructor syntax for finalization code.
		// This destructor will run only if the Dispose method
		// does not get called.
		// It gives your base class the opportunity to finalize.
		// Do not provide destructors in types derived from this class.
		~ContentManager()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}

		public ContentManager(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			this.serviceProvider = serviceProvider;
            AddContentManager(this);
		}

		public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			if (rootDirectory == null)
			{
				throw new ArgumentNullException("rootDirectory");
			}
			this.RootDirectory = rootDirectory;
			this.serviceProvider = serviceProvider;
            AddContentManager(this);
		}

		public void Dispose()
		{
			Dispose(true);
			// Tell the garbage collector not to call the finalizer
			// since all the cleanup will already be done.
			GC.SuppressFinalize(this);
            // Once disposed, content manager wont be used again
            RemoveContentManager(this);
		}

		// If disposing is true, it was called explicitly and we should dispose managed objects.
		// If disposing is false, it was called by the finalizer and managed objects should not be disposed.
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
                if (disposing)
                {
                    Unload();
                }

                scratchBuffer = null;
				disposed = true;
			}
		}

		public virtual T Load<T>(string assetName)
		{
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            T result = default(T);
            
            // On some platforms, name and slash direction matter.
            // We store the asset by a /-seperating key rather than how the
            // path to the file was passed to us to avoid
            // loading "content/asset1.xnb" and "content\\ASSET1.xnb" as if they were two 
            // different files. This matches stock XNA behavior.
            // The dictionary will ignore case differences
            var key = assetName.Replace('\\', '/');

            // Check for a previously loaded asset first
            object asset = null;
            if (loadedAssets.TryGetValue(key, out asset))
            {
                if (asset is T)
                {
                    return (T)asset;
                }
            }

            // Load the asset.
            result = ReadAsset<T>(assetName, null);

            loadedAssets[key] = result;
            return result;
		}
		

		protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
		{
            throw new NotImplementedException();
		}

        internal void RecordDisposable(IDisposable disposable)
        {
            Debug.Assert(disposable != null, "The disposable is null!");

            // Avoid recording disposable objects twice. ReloadAsset will try to record the disposables again.
            // We don't know which asset recorded which disposable so just guard against storing multiple of the same instance.
            if (!disposableAssets.Contains(disposable))
                disposableAssets.Add(disposable);
        }

        /// <summary>
        /// Virtual property to allow a derived ContentManager to have it's assets reloaded
        /// </summary>
        protected virtual Dictionary<string, object> LoadedAssets
        {
            get { return loadedAssets; }
        }

		protected virtual void ReloadGraphicsAssets()
        {
#if !WebGL
            foreach (var asset in LoadedAssets)
            {
                // This never executes as asset.Key is never null.  This just forces the 
                // linker to include the ReloadAsset function when AOT compiled.
                if (asset.Key == null)
                    ReloadAsset(asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()));

                var methodInfo = ReflectionHelpers.GetMethodInfo(typeof(ContentManager), "ReloadAsset");
                var genericMethod = methodInfo.MakeGenericMethod(asset.Value.GetType());
                genericMethod.Invoke(this, new object[] { asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()) }); 
            }
#endif
        }


		public virtual void Unload()
		{
		    // Look for disposable assets.
		    foreach (var disposable in disposableAssets)
		    {
		        if (disposable != null)
		            disposable.Dispose();
		    }
			disposableAssets.Clear();
		    loadedAssets.Clear();
		}

		public string RootDirectory
		{
			get
			{
				return _rootDirectory;
			}
			set
			{
				_rootDirectory = value;
			}
		}

        internal string RootDirectoryFullPath
        {
            get
            {
                return Path.Combine(TitleContainer.Location, RootDirectory);
            }
        }
		
		public IServiceProvider ServiceProvider
		{
			get
			{
				return this.serviceProvider;
			}
		}

        internal byte[] GetScratchBuffer(int size)
        {            
            size = Math.Max(size, 1024 * 1024);
            if (scratchBuffer == null || scratchBuffer.Length < size)
                scratchBuffer = new byte[size];
            return scratchBuffer;
        }
	}
}
