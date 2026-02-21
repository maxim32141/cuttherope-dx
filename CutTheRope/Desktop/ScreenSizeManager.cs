using System;

using CutTheRope.Framework.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CutTheRope.Desktop
{
    internal sealed class ScreenSizeManager(int gameWidth, int gameHeight)
    {
        // (get) Token: 0x060000AF RID: 175 RVA: 0x00004B78 File Offset: 0x00002D78
        public static int MAX_WINDOW_WIDTH => Global.GraphicsDeviceManager.GraphicsProfile == GraphicsProfile.HiDef ? 4096 : 2048;

        // (get) Token: 0x060000B0 RID: 176 RVA: 0x00004B92 File Offset: 0x00002D92
        public int WindowWidth => _windowRect.Width;

        // (get) Token: 0x060000B1 RID: 177 RVA: 0x00004B9F File Offset: 0x00002D9F
        public int WindowHeight => _windowRect.Height;

        // (get) Token: 0x060000B2 RID: 178 RVA: 0x00004BAC File Offset: 0x00002DAC
        public int ScreenWidth => _fullScreenRect.Width;

        // (get) Token: 0x060000B3 RID: 179 RVA: 0x00004BB9 File Offset: 0x00002DB9
        public int ScreenHeight => _fullScreenRect.Height;

        // (get) Token: 0x060000B4 RID: 180 RVA: 0x00004BC6 File Offset: 0x00002DC6
        public bool IsFullScreen { get; private set; }

        // (get) Token: 0x060000B5 RID: 181 RVA: 0x00004BCE File Offset: 0x00002DCE
        public Rectangle CurrentSize => IsFullScreen ? _fullScreenRect : _windowRect;

        // (get) Token: 0x060000B6 RID: 182 RVA: 0x00004BE5 File Offset: 0x00002DE5
        public int GameWidth { get; } = gameWidth;

        // (get) Token: 0x060000B7 RID: 183 RVA: 0x00004BED File Offset: 0x00002DED
        public int GameHeight { get; } = gameHeight;

        // (get) Token: 0x060000B8 RID: 184 RVA: 0x00004BF5 File Offset: 0x00002DF5
        public Rectangle ScaledViewRect => _scaledViewRect;

        public Rectangle ScaledViewRectPixels => _scaledViewRectPixels;

        public double BackingScale => _backingScaleState.CurrentScale;

        public double EffectiveDisplayScale
        {
            get
            {
                double logicalScale = _scaledViewRect.Width > 0 ? _scaledViewRect.Width / (double)GameWidth : 1d;
                return logicalScale * BackingScale;
            }
        }

        public int SurfaceWidthPixels => Math.Max(1, (int)Math.Round(CurrentSize.Width * BackingScale, MidpointRounding.AwayFromZero));

        public int SurfaceHeightPixels => Math.Max(1, (int)Math.Round(CurrentSize.Height * BackingScale, MidpointRounding.AwayFromZero));

        // (get) Token: 0x060000B9 RID: 185 RVA: 0x00004BFD File Offset: 0x00002DFD
        public bool SkipSizeChanges { get; private set; }

        // (set) Token: 0x060000BA RID: 186 RVA: 0x00004C05 File Offset: 0x00002E05
        public bool FullScreenCropWidth
        {
            set
            {
                if (_fullScreenCropWidth != value)
                {
                    _fullScreenCropWidth = value;
                    UpdateScaledView();
                }
            }
        }

        // (get) Token: 0x060000BB RID: 187 RVA: 0x00004C1D File Offset: 0x00002E1D
        public double WidthAspectRatio => _scaledViewRect.Width / (double)GameWidth;

        public int TransformWindowToViewX(int x)
        {
            return x - _scaledViewRect.X;
        }

        public int TransformWindowToViewY(int y)
        {
            return y - _scaledViewRect.Y;
        }

        public float TransformViewToGameX(float x)
        {
            return x * GameWidth / _scaledViewRect.Width;
        }

        public float TransformViewToGameY(float y)
        {
            return y * GameHeight / _scaledViewRect.Height;
        }

        public void Init(DisplayMode displayMode, int windowWidth, bool isFullScreen)
        {
            _ = TryUpdateBackingScale();
            FullScreenRectChanged(displayMode);
            int targetWindowWidth = windowWidth > 0 ? windowWidth : displayMode.Width - 100;
            if (targetWindowWidth < 800)
            {
                targetWindowWidth = 800;
            }
            if (targetWindowWidth > MAX_WINDOW_WIDTH)
            {
                targetWindowWidth = MAX_WINDOW_WIDTH;
            }
            if (targetWindowWidth > displayMode.Width)
            {
                targetWindowWidth = displayMode.Width;
            }
            WindowRectChanged(new Rectangle(0, 0, targetWindowWidth, ScaledGameHeight(targetWindowWidth)));
            if (isFullScreen)
            {
                ToggleFullScreen();
                return;
            }
            ApplyWindowSize(WindowWidth);
        }

        public bool RefreshBackingScaleIfChanged()
        {
            if (!TryUpdateBackingScale())
            {
                return false;
            }

            ApplyViewportToDevice();
            return true;
        }

        public int ScaledGameWidth(int scaledHeight)
        {
            return (int)((scaledHeight / _gameAspectRatio) + 0.5);
        }

        public int ScaledGameHeight(int scaledWidth)
        {
            return (int)((scaledWidth * _gameAspectRatio) + 0.5);
        }

        private void UpdateScaledView()
        {
            if (SkipSizeChanges)
            {
                return;
            }
            // Always use fullscreen-style letterboxing/pillarboxing for both modes
            Rectangle sourceRect = IsFullScreen ? _fullScreenRect : _windowRect;
            if (sourceRect.Width >= sourceRect.Height)
            {
                int scaledHeight = _fullScreenCropWidth ? sourceRect.Height : ScaledGameHeight(sourceRect.Width);
                int scaledWidth = _fullScreenCropWidth ? ScaledGameWidth(scaledHeight) : sourceRect.Width;
                _scaledViewRect = new Rectangle((sourceRect.Width - scaledWidth) / 2, (sourceRect.Height - scaledHeight) / 2, scaledWidth, scaledHeight);
                _scaledViewRectPixels = BackingScaleMath.LogicalToPixelRect(_scaledViewRect, BackingScale);
                return;
            }
            int portraitScaledHeight = _fullScreenCropWidth ? (int)(sourceRect.Width / 5f * 4f) : ScaledGameHeight(sourceRect.Width);
            int portraitScaledWidth = _fullScreenCropWidth ? ScaledGameWidth(portraitScaledHeight) : sourceRect.Width;
            _scaledViewRect = new Rectangle((sourceRect.Width - portraitScaledWidth) / 2, (sourceRect.Height - portraitScaledHeight) / 2, portraitScaledWidth, portraitScaledHeight);
            _scaledViewRectPixels = BackingScaleMath.LogicalToPixelRect(_scaledViewRect, BackingScale);
        }

        public void ApplyWindowSize(int width)
        {
            GraphicsDeviceManager graphicsDeviceManager = Global.GraphicsDeviceManager;
            graphicsDeviceManager.PreferredBackBufferWidth = width;
            graphicsDeviceManager.PreferredBackBufferHeight = ScaledGameHeight(width);
            graphicsDeviceManager.ApplyChanges();
            WindowRectChanged(new Rectangle(0, 0, graphicsDeviceManager.PreferredBackBufferWidth, graphicsDeviceManager.PreferredBackBufferHeight));
        }

        public void ToggleFullScreen()
        {
            SkipSizeChanges = true;
            GraphicsDeviceManager graphicsDeviceManager = Global.GraphicsDeviceManager;
            bool isFullScreen = graphicsDeviceManager.IsFullScreen;
            bool fullScreenCropWidth = _fullScreenCropWidth;
            FullScreenCropWidth = true;
            if (isFullScreen)
            {
                graphicsDeviceManager.PreferredBackBufferWidth = _windowRect.Width;
                graphicsDeviceManager.PreferredBackBufferHeight = _windowRect.Height;
            }
            else
            {
                graphicsDeviceManager.PreferredBackBufferWidth = _fullScreenRect.Width;
                graphicsDeviceManager.PreferredBackBufferHeight = _fullScreenRect.Height;
            }
            graphicsDeviceManager.IsFullScreen = !isFullScreen;
            graphicsDeviceManager.ApplyChanges();
            ApplyViewportToDevice();
            FullScreenCropWidth = fullScreenCropWidth;
            SkipSizeChanges = false;
            EnableFullScreen(!isFullScreen);
            Save();
            Application.SharedCanvas().Reshape();
            Application.SharedRootController().FullscreenToggled(!isFullScreen);
        }

        public void FixWindowSize(Rectangle newWindowRect)
        {
            if (SkipSizeChanges)
            {
                return;
            }
            GraphicsDeviceManager graphicsDeviceManager = Global.GraphicsDeviceManager;
            FullScreenRectChanged(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode);
            if (!IsFullScreen)
            {
                try
                {
                    int targetWidth = graphicsDeviceManager.PreferredBackBufferWidth;
                    if (newWindowRect.Width != WindowWidth)
                    {
                        targetWidth = newWindowRect.Width;
                    }
                    else if (newWindowRect.Height != WindowHeight)
                    {
                        targetWidth = ScaledGameWidth(newWindowRect.Height);
                    }
                    if (targetWidth < 800 || ScaledGameHeight(targetWidth) < ScaledGameHeight(800))
                    {
                        targetWidth = 800;
                    }
                    if (targetWidth > MAX_WINDOW_WIDTH)
                    {
                        targetWidth = MAX_WINDOW_WIDTH;
                    }
                    if (targetWidth > ScreenWidth)
                    {
                        targetWidth = ScreenWidth;
                    }
                    ApplyWindowSize(targetWidth);
                }
                catch (Exception)
                {
                }
            }
            Save();
            Application.SharedCanvas().Reshape();
        }

        public void ApplyViewportToDevice()
        {
            Rectangle boundsLogical = !IsFullScreen ? Rectangle.Intersect(_scaledViewRect, _windowRect) : Rectangle.Intersect(_scaledViewRect, _fullScreenRect);
            Rectangle boundsPixels = BackingScaleMath.LogicalToPixelRect(boundsLogical, BackingScale);
            try
            {
                if (Global.GraphicsDevice == null)
                {
                    return;
                }

                PresentationParameters presentationParameters = Global.GraphicsDevice.PresentationParameters;
                Rectangle bounds = BackingScaleMath.ResolvePresentDestinationRect(
                    boundsLogical,
                    boundsPixels,
                    presentationParameters.BackBufferWidth,
                    presentationParameters.BackBufferHeight,
                    BackingScale);

                Global.GraphicsDevice.Viewport = new Viewport(bounds);
            }
            catch (Exception)
            {
            }
        }

        public void Save()
        {
            Preferences.SetIntForKey(_windowRect.Width, "PREFS_WINDOW_WIDTH", false);
            Preferences.SetIntForKey(_windowRect.Height, "PREFS_WINDOW_HEIGHT", false);
            Preferences.SetBooleanForKey(IsFullScreen, "PREFS_WINDOW_FULLSCREEN", true);
        }

        private void WindowRectChanged(Rectangle newWindowRect)
        {
            if (!SkipSizeChanges)
            {
                _windowRect = newWindowRect;
                _windowRect.X = 0;
                _windowRect.Y = 0;
                UpdateScaledView();
            }
        }

        private void FullScreenRectChanged(DisplayMode d)
        {
            FullScreenRectChanged(new Rectangle(0, 0, d.Width, d.Height));
        }

        private void FullScreenRectChanged(Rectangle r)
        {
            if (!SkipSizeChanges)
            {
                _fullScreenRect = r;
                UpdateScaledView();
            }
        }

        private void EnableFullScreen(bool bFull)
        {
            if (!SkipSizeChanges)
            {
                IsFullScreen = bFull;
                UpdateScaledView();
            }
        }

        private bool TryUpdateBackingScale()
        {
            double candidateScale;
            try
            {
                if (!_backingScaleProvider.TryGetCurrentScale(out double reportedScale))
                {
                    return false;
                }

                candidateScale = reportedScale;
            }
            catch (Exception)
            {
                return false;
            }

            if (ShouldIgnoreFullscreenDownscaleToOne(
                    isFullScreen: IsFullScreen,
                    isDesktopGLProvider: _backingScaleProvider is DesktopGLBackingScaleProvider,
                    currentScale: _backingScaleState.CurrentScale,
                    candidateScale: candidateScale))
            {
                return false;
            }

            if (!_backingScaleState.TryUpdate(candidateScale))
            {
                return false;
            }

            UpdateScaledView();
            return true;
        }

        internal static bool ShouldIgnoreFullscreenDownscaleToOne(
            bool isFullScreen,
            bool isDesktopGLProvider,
            double currentScale,
            double candidateScale)
        {
            const double epsilon = 0.01d;
            if (!isFullScreen || !isDesktopGLProvider)
            {
                return false;
            }

            if (currentScale <= (1d + epsilon))
            {
                return false;
            }

            double normalizedCandidate = BackingScaleMath.NormalizeScale(candidateScale);
            return Math.Abs(normalizedCandidate - 1d) <= epsilon;
        }

        private static IBackingScaleProvider CreateBackingScaleProvider()
        {
            if (!OperatingSystem.IsMacOS())
            {
                return new FallbackBackingScaleProvider();
            }

#if MACOS_AVFOUNDATION
            return new MacBackingScaleProvider();
#else
            return new DesktopGLBackingScaleProvider();
#endif
        }

        public const int MIN_WINDOW_WIDTH = 800;
        private Rectangle _windowRect;

        private Rectangle _fullScreenRect;
        private readonly double _gameAspectRatio = gameHeight / (double)gameWidth;

        private readonly IBackingScaleProvider _backingScaleProvider = CreateBackingScaleProvider();
        private readonly BackingScaleState _backingScaleState = new(1d, epsilon: 0.01d, downscaleToOneConfirmationReadings: 3);
        private Rectangle _scaledViewRect;
        private Rectangle _scaledViewRectPixels;
        private bool _fullScreenCropWidth = true;
    }
}
