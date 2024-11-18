using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System;

namespace Anaglyph.DisplayCapture
{
    [DefaultExecutionOrder(-1000)]
    public class VRDisplayCaptureManager : MonoBehaviour
    {
        public static VRDisplayCaptureManager Instance { get; private set; }

        [SerializeField] private Vector2Int textureSize = new(1024, 1024);
        [SerializeField] private Vector2Int cropSize = new(512, 512); // Size of the cropped region

        [Header("Trigger Settings")]
        [SerializeField] private bool useExternalTrigger = false;
        [SerializeField] private UnityEvent onCaptureTriggered;
        [SerializeField] private UnityEvent<Texture2D> onSnapshotTaken;

        [Header("Controller Settings")]
        [SerializeField] private XRNode controllerNode = XRNode.RightHand;
        [SerializeField] private InputFeatureUsage<bool> selectedButton = CommonUsages.primaryButton;
        [Tooltip("Choose which controller to use")]
        public ControllerHand controllerHand = ControllerHand.Right;
        [Tooltip("Choose which button to use")]
        public ControllerButton buttonChoice = ControllerButton.TriggerButton;

        [Header("Mesh Settings")]
        [SerializeField] private MeshRenderer targetMeshRenderer;
        [SerializeField] private int materialIndex = 0;

        [Header("Post Request Settings")]
        [SerializeField] private PostRequest postRequest;

        public Texture2D SnapshotTexture => croppedTexture;

        public enum ControllerHand
        {
            Left,
            Right
        }

        public enum ControllerButton
        {
            TriggerButton,
            GripButton,
            PrimaryButton,
            SecondaryButton,
            MenuButton,
            Primary2DAxisClick,
            PrimaryTouch,
            SecondaryTouch
        }

        private Texture2D screenTexture;
        private Texture2D snapshotTexture;
        private Texture2D croppedTexture;
        private bool wasButtonPressed = false;
        private AndroidInterface androidInterface;
        private unsafe sbyte* imageData;
        private int bufferSize;

        // Nested AndroidInterface class - this is important!
        private class AndroidInterface
        {
            private AndroidJavaClass androidClass;
            private AndroidJavaObject androidInstance;

            public AndroidInterface(GameObject messageReceiver, int textureWidth, int textureHeight)
            {
                androidClass = new AndroidJavaClass("com.trev3d.DisplayCapture.DisplayCaptureManager");
                androidInstance = androidClass.CallStatic<AndroidJavaObject>("getInstance");
                androidInstance.Call("setup", messageReceiver.name, textureWidth, textureHeight);
            }

            public void RequestCapture() => androidInstance.Call("requestCapture");
            public void StopCapture() => androidInstance.Call("stopCapture");

            public unsafe sbyte* GetByteBuffer()
            {
                AndroidJavaObject byteBuffer = androidInstance.Call<AndroidJavaObject>("getByteBuffer");
                return AndroidJNI.GetDirectBufferAddress(byteBuffer.GetRawObject());
            }
        }

        private InputFeatureUsage<bool> GetSelectedInputFeature()
        {
            switch (buttonChoice)
            {
                case ControllerButton.TriggerButton:
                    return CommonUsages.triggerButton;
                case ControllerButton.GripButton:
                    return CommonUsages.gripButton;
                case ControllerButton.PrimaryButton:
                    return CommonUsages.primaryButton;
                case ControllerButton.SecondaryButton:
                    return CommonUsages.secondaryButton;
                case ControllerButton.MenuButton:
                    return CommonUsages.menuButton;
                case ControllerButton.Primary2DAxisClick:
                    return CommonUsages.primary2DAxisClick;
                case ControllerButton.PrimaryTouch:
                    return CommonUsages.primaryTouch;
                case ControllerButton.SecondaryTouch:
                    return CommonUsages.secondaryTouch;
                default:
                    return CommonUsages.triggerButton;
            }
        }

        private void Awake()
        {
            Instance = this;
            androidInterface = new AndroidInterface(gameObject, textureSize.x, textureSize.y);
            screenTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, 1, false);
            snapshotTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, 1, false);
            croppedTexture = new Texture2D(cropSize.x * 2, cropSize.y * 2, TextureFormat.RGBA32, 1, false);
            bufferSize = textureSize.x * textureSize.y * 4;

            controllerNode = controllerHand == ControllerHand.Right ? XRNode.RightHand : XRNode.LeftHand;

            if (onCaptureTriggered == null)
                onCaptureTriggered = new UnityEvent();
            if (onSnapshotTaken == null)
                onSnapshotTaken = new UnityEvent<Texture2D>();
        }

        private void Start()
        {
            if (!targetMeshRenderer)
            {
                Debug.LogError("Target MeshRenderer not assigned!");
                return;
            }

            if (postRequest == null)
            {
                postRequest = FindObjectOfType<PostRequest>();
                if (postRequest == null)
                {
                    Debug.LogWarning("PostRequest component not found in the scene!");
                }
            }

            StartScreenCapture();
        }

        private void Update()
        {
            if (!useExternalTrigger)
            {
                CheckControllerInput();
            }
        }

        private void CheckControllerInput()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            bool isPressed = false;

            if (device.isValid)
            {
                if (device.TryGetFeatureValue(GetSelectedInputFeature(), out isPressed))
                {
                    if (isPressed && !wasButtonPressed)
                    {
                        TakeSnapshot();
                    }
                    wasButtonPressed = isPressed;
                }
            }
        }

        public void TriggerSnapshot()
        {
            if (useExternalTrigger)
            {
                TakeSnapshot();
                onCaptureTriggered.Invoke();
            }
        }

        private void TakeSnapshot()
        {
            Graphics.CopyTexture(screenTexture, snapshotTexture);

            // Calculate the center region to crop
            int startX = (textureSize.x - cropSize.x) / 2;
            int startY = (textureSize.y - cropSize.y) / 2;

            // Get the pixels from the center region
            Color[] cropPixels = snapshotTexture.GetPixels(startX, startY, cropSize.x, cropSize.y);

            // Scale up the cropped pixels
            Color[] scaledPixels = new Color[cropSize.x * 2 * cropSize.y * 2];

            for (int y = 0; y < cropSize.y * 2; y++)
            {
                for (int x = 0; x < cropSize.x * 2; x++)
                {
                    // Sample from the original cropped pixels
                    int sourceX = x / 2;
                    int sourceY = y / 2;
                    int sourceIndex = sourceY * cropSize.x + sourceX;
                    int targetIndex = y * (cropSize.x * 2) + x;

                    scaledPixels[targetIndex] = cropPixels[sourceIndex];
                }
            }

            // Apply the scaled pixels to the cropped texture
            croppedTexture.SetPixels(0, 0, cropSize.x * 2, cropSize.y * 2, scaledPixels);
            croppedTexture.Apply();

            ApplySnapshotToMesh();

            if (postRequest != null)
            {
                postRequest.image = croppedTexture;
            }

            onSnapshotTaken.Invoke(croppedTexture);

            Debug.Log($"Snapshot taken and cropped {(useExternalTrigger ? "via external trigger" : $"using {controllerHand} controller {buttonChoice}")}!");
        }

        private void ApplySnapshotToMesh()
        {
            if (targetMeshRenderer && targetMeshRenderer.materials.Length > materialIndex)
            {
                Material material = targetMeshRenderer.materials[materialIndex];
                material.mainTexture = croppedTexture;
                targetMeshRenderer.materials[materialIndex] = material;
            }
        }

        public void StartScreenCapture()
        {
            androidInterface.RequestCapture();
        }

        public void StopScreenCapture()
        {
            androidInterface.StopCapture();
        }

        private unsafe void OnCaptureStarted()
        {
            imageData = androidInterface.GetByteBuffer();
        }

        private unsafe void OnNewFrameAvailable()
        {
            if (imageData == default) return;
            screenTexture.LoadRawTextureData((IntPtr)imageData, bufferSize);
            screenTexture.Apply();
        }
    }
}