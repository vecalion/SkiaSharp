using System;
using System.ComponentModel;
using CoreGraphics;
using Foundation;
using GLKit;
using OpenGLES;

namespace SkiaSharp.Views
{
	[Register(nameof(SKGLView))]
	[DesignTimeVisible(true)]
	public class SKGLView : GLKView, IGLKViewDelegate, IComponent
	{
		// for IComponent
		public ISite Site { get; set; }
		public event EventHandler Disposed;
		private bool designMode;

		private GRContext context;
		private GRBackendRenderTargetDesc renderTarget;

		// created in code
		public SKGLView()
		{
			Initialize();
		}

		// created in code
		public SKGLView(CGRect frame)
			: base(frame)
		{
			Initialize();
		}

		// created via designer
		public SKGLView(IntPtr p)
			: base(p)
		{
		}

		// created via designer
		public override void AwakeFromNib()
		{
			designMode = Site?.DesignMode == true;

			Initialize();
		}

		private void Initialize()
		{
			// create the GL context
			Context = new EAGLContext(EAGLRenderingAPI.OpenGLES2);
			DrawableColorFormat = GLKViewDrawableColorFormat.RGBA8888;
			DrawableDepthFormat = GLKViewDrawableDepthFormat.Format24;
			DrawableStencilFormat = GLKViewDrawableStencilFormat.Format8;
			DrawableMultisample = GLKViewDrawableMultisample.Sample4x;

			// hook up the drawing 
			Delegate = this;
		}

		public new void DrawInRect(GLKView view, CGRect rect)
		{
			if (designMode)
				return;

			if (context == null)
			{
				var glInterface = GRGlInterface.CreateNativeGlInterface();
				context = GRContext.Create(GRBackend.OpenGL, glInterface);

				// get the initial details
				renderTarget = SKGLDrawable.CreateRenderTarget();
			}

			// set the dimensions as they might have changed
			renderTarget.Width = (int)DrawableWidth;
			renderTarget.Height = (int)DrawableHeight;

			// create the surface
			using (var surface = SKSurface.Create(context, renderTarget))
			{
				// draw on the surface
				DrawInSurface(surface, renderTarget);

				surface.Canvas.Flush();
			}

			// flush the SkiaSharp contents to GL
			context.Flush();
		}

		public event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurface;

		public virtual void DrawInSurface(SKSurface surface, GRBackendRenderTargetDesc renderTarget)
		{
			PaintSurface?.Invoke(this, new SKPaintGLSurfaceEventArgs(surface, renderTarget));
		}

		public override CGRect Frame
		{
			get { return base.Frame; }
			set
			{
				base.Frame = value;
				SetNeedsDisplay();
			}
		}
	}
}
