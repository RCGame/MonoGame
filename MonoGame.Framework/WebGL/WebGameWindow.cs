// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Microsoft.Xna.Framework
{
    class WebGameWindow : GameWindow
    {
        private WebGamePlatform _platform;

        public dynamic document, glcanvas, gl, window;

        private Action<dynamic> _onmousemove, _onmousedown, _onmouseup, _onkeydown, _onkeyup, _onwheel;

        public WebGameWindow(WebGamePlatform platform)
        {
            _platform = platform;

            //Mouse.PrimaryWindow = this;
        }

        private void OnCursorLockChange()
        {
            if (document.pointerLockElement == glcanvas || document.mozPointerLockElement == glcanvas || document.webkitPointerLockElement == glcanvas)
            {
                document.addEventListener("mousemove", _onmousemove, false);
                document.addEventListener("mousedown", _onmousedown, false);
                document.addEventListener("mouseup", _onmouseup, false);
                glcanvas.addEventListener("wheel", _onwheel, false);
                document.addEventListener("keydown", _onkeydown, false);
                document.addEventListener("keyup", _onkeyup, false);

            }
            else
            {
                document.removeEventListener("mousemove", _onmousemove, false);
                document.removeEventListener("mousedown", _onmousedown, false);
                document.removeEventListener("mouseup", _onmouseup, false);
                glcanvas.removeEventListener("wheel", _onwheel, false);
                document.removeEventListener("keydown", _onkeydown, false);
                document.removeEventListener("keyup", _onkeyup, false);

            }
        }

        private void OnMouseClick(dynamic e)
        {
            glcanvas.requestPointerLock();
        }

        private void OnMouseMove(dynamic e)
        {
        }

        private void OnMouseDown(dynamic e)
        {
        }

        private void OnMouseUp(dynamic e)
        {
        }

        private void OnMouseWheel(dynamic e)
        {
        }

        private void OnKeyDown(dynamic e)
        {

        }

        private void OnKeyUp(dynamic e)
        {

        }

        internal void ProcessEvents()
        {

        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
        }

        protected override void SetTitle(string title)
        {
        }

        public override bool AllowUserResizing
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                int width = glcanvas.clientWidth;
                int height = glcanvas.clientHeight;

                return new Rectangle(0, 0, width, height);
            }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get
            {
                return DisplayOrientation.Default;
            }
        }

        public override string ScreenDeviceName
        {
            get
            {
                return string.Empty;
            }
        }
    }
}

