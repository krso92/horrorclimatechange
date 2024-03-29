﻿//#define PI_CREATE_ASSET_MENUS

using UnityEngine;

namespace Sisus
{
	#if PI_CREATE_ASSET_MENUS
	[CreateAssetMenu]
	#endif
	public class InspectorGraphics : ScriptableObject
	{
		public Texture WhitePixel;

		public Texture InspectorIconActive;
		public Texture InspectorIconInactive;

		public Texture objectHeaderBg;
		public Texture missingAssetIcon;
		public Texture rightClickAreaIndicator;
		public Texture tooltipIcon;
		
		public SkinnedTexture splitterBg = new SkinnedTexture();
		public SkinnedTexture prefixColumnResizeHandle = new SkinnedTexture();
		public SkinnedTexture prefixColumnResizeTrackLeft = new SkinnedTexture();
		public SkinnedTexture prefixColumnResizeTrackRight = new SkinnedTexture();
		
		public Texture horizontalSplitterBg;
		public Texture ReorderDropTargetBg;
		public Texture IconFolded;
		public Texture IconUnfolded;
		public Texture IconFrame;
		public SkinnedTexture ExecuteIcon;
		public Texture DebugModeOnIcon;
		public Texture DebugModeOffIcon;

		public Texture NavigationArrowLeft;
		public Texture NavigationArrowRight;

		public Texture DirectoryIcon;
		public Texture PrefabIcon;
		public Texture DirectoryIconEditor;
		public Texture DirectoryIconUnity;
		public Texture DirectoryIconUnityEditor;

		public Texture CSharpScriptIcon;
		public Texture DotNetFileIcon;
		public Texture DLLFileIcon;
		public Texture UnityFileIcon;
		public Texture UnityEditorFileIcon;
		
		[Header("Transform")]
		public Texture LocalSpaceIcon;
		public Texture WorldSpaceIcon;
		public Texture Position;
		public Texture Rotation;
		public Texture Scale;
		public Texture X;
		public Texture Y;
		public Texture Z;
		
		public Texture PowerInspectorLogo;

		public SkinnedTexture SnappingOnIcon = new SkinnedTexture();
		public SkinnedTexture SnappingOffIcon = new SkinnedTexture();

		public Texture HierarchyFolderIcon;

		public Texture errorMessage;
		public Texture warningMessage;
		public Texture infoMessage;

		public Texture clipboardCopy;
		public Texture clipboardPaste;
		public Texture clipboardInvalidOperation;

		public Texture enteringPlayMode;
	}
}